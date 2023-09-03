using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Server
{
    public class GameServer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly IIdGenerator<ushort> _networkObjectIdGenerator;
        private readonly INetworkSpawnedObjectsRepository _networkSpawnedObjectsRepository;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private UdpTransport _udpTransport;
        private bool _isRunning;
        private int _nextId;
       

        internal GameServer(
            INetworkConfiguration networkConfiguration)
        {
            _networkConfiguration = networkConfiguration;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;

        internal event Action<NetworkClient, byte[]> ClientConnected;
        internal event Action<int> ClientDisconnected;
        internal event Action<int> ClientReconnected;
        internal event Action<byte[]> SpawnHandlerReceived; 
        internal event Action<byte[]> SpawnReceived; 
        internal event Action<byte[]> NetworkMessageReceived; 
        
        public void SendMessage<T>(T message, int networkClientId, ESendMode sendMode)
        {
            var handlerId = typeof(T).FullName;
            var hasClient = ConnectedPlayers.TryGetValue(networkClientId, out var client);
            
            if(!hasClient)
                return;

            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt(payload.Length);
            byteWriter.AddBytes(payload);
            
            Send(byteWriter.Data, client.RemoteEndpoint, sendMode);
        }
        
        internal void Start()
        {
            var ipResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");
            
            if(_isRunning)
                throw new Exception($"[{nameof(NetworkManager)}] can't start server when network manager already running ");
            
            var localEndPoint = new IPEndPoint(ip, _networkConfiguration.ServerPort);

            _udpTransport = new UdpTransport(localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;
            
            // UniTask.Create(async () => { await Receive(); });
            // UniTask.Create(async () => { await ProcessSendQueue(); });
            // Task.Run(async () => await Receive());
            // Task.Run(async () => await ProcessSendQueue());

            Task.Run(async () => await Receive());
        }

        internal void Update()
        {
            ProcessReceiveQueue();
            ProcessSendQueue();
            //Receive();
        }

        internal void Send(
            byte[] data, 
            NetworkClient networkClient, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, networkClient.RemoteEndpoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }
        
        internal void Send(
            byte[] data, 
            IPEndPoint remoteEndPoint, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, remoteEndPoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }
        
        internal void SendToAll(
            byte[] data,
            ESendMode sendMode
        )
        {
            foreach (var client in _networkClientsTable.Values)
            {
                var outcomeMessage = new OutcomePendingMessage(data, client.RemoteEndpoint, sendMode);
            
                _sendMessagesQueue.Enqueue(outcomeMessage);
            }
        }

        internal void DisconnectClient(int clientId, string reason)
        {
            if (_networkClientsTable.TryGetValue(clientId, out var client))
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddInt((ushort)ENetworkMessageType.Disconnect);
                byteWriter.AddString(reason);

                Send(byteWriter.Data, client.RemoteEndpoint, ESendMode.Reliable);

                _networkClientsTable.Remove(clientId);
            }
        }
        
        internal void Stop()
        {
            _isRunning = false;
            _udpTransport.Stop();
        }
        
        private async Task Receive()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpTransport.ReceiveAsync();
                    var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                    Debug.Log("Received");
                    _receiveMessagesQueue.Enqueue(incomeMessage);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
        }
        
        private void ProcessReceiveQueue()
        {
            while (_receiveMessagesQueue.Count > 0)
            {
                var canDequeue = _receiveMessagesQueue.TryDequeue(out var message);

                if (canDequeue)
                {
                    HandleIncomeMessage(message);
                }
            }
        }

        private async UniTask ProcessSendQueue()
        {
            while (_sendMessagesQueue.Count > 0)
            {
                try
                {
                    var canDequeue = _sendMessagesQueue.TryDequeue(out var message);
                
                    if (canDequeue)
                    {
                        await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
        
        private void HandleIncomeMessage(IncomePendingMessage incomePendingMessage)
        {
            var messageType = MessageHelper.GetMessageType(incomePendingMessage.Payload);
            var messagePayload = incomePendingMessage.Payload;
            Debug.Log($"HandleIncomeMessage = {messageType}");
            switch (messageType)
            {
                case ENetworkMessageType.ConnectionRequest:
                    HandleNewConnection(messagePayload, incomePendingMessage.RemoteEndPoint);
                    break;
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(messagePayload);
                    break;
                case ENetworkMessageType.ClientReconnected:
                    HandleClientReconnected(messagePayload);
                    break;
                case ENetworkMessageType.NetworkMessage:
                    HandleNetworkMessage(messagePayload);
                    break;
                case ENetworkMessageType.SpawnHandler:
                    HandleSpawnHandler(messagePayload);
                    break;
                case ENetworkMessageType.Spawn:
                    HandleSpawn(messagePayload);
                    break;
                case ENetworkMessageType.ClientReady:
                    HandleClientReady(messagePayload);
                    break;
            }
        }

        private void HandleClientReady(byte[] messagePayload)
        {
            
        }

        private void HandleSpawn(byte[] payload)
        {
            SpawnReceived?.Invoke(payload);
        }
        
        private void HandleSpawnHandler(byte[] payload)
        {
            SpawnHandlerReceived?.Invoke(payload);
        }
        
        private void HandleNetworkMessage(byte[] payload)
        {
            NetworkMessageReceived?.Invoke(payload);
        }

        private void HandleNewConnection(byte[] messagePayload, IPEndPoint remoteEndPoint)
        {
            var messageType = MessageHelper.GetMessageType(messagePayload);

            if (messageType == ENetworkMessageType.ConnectionRequest)
            {
                var clientId = _nextId++;
                
                var networkClient = new NetworkClient(clientId, remoteEndPoint);

                var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

                if (!hasClient)
                {
                    _networkClientsTable.Add(clientId, networkClient);
                    
                    var byteWriter = new ByteWriter();
                    
                    byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
                    byteWriter.AddInt(clientId);
                    byteWriter.AddString(remoteEndPoint.Address.ToString());
                    byteWriter.AddInt(remoteEndPoint.Port);

                    Send(byteWriter.Data, networkClient, ESendMode.Reliable);
                    
                    //TODO: segregate client's credentials message 

                    var msgLength = messagePayload.Length - 2;
                    
                    var connectInfoPayload = new byte[msgLength];
                    
                    Buffer.BlockCopy(messagePayload, 2, connectInfoPayload, 0, msgLength);
                    
                    ClientConnected?.Invoke(networkClient, connectInfoPayload);
                }
            }
            
        }
        
        private void HandleClientReconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasClient)
            {
                client.IsOnline = true;
                ClientReconnected?.Invoke(clientId);
            }
        }

        private void HandleClientDisconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasPlayer = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                client.IsOnline = false;

                ClientDisconnected?.Invoke(clientId);
            }
        }
        
    }
}