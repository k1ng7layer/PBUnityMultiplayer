using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Server
{
    public class GameServer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly TransportBase _transportBase;
        private readonly IIdGenerator<ushort> _networkObjectIdGenerator;
        private readonly INetworkSpawnedObjectsRepository _networkSpawnedObjectsRepository;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly Dictionary<int, NetworkClient> _pendingClientsTable = new();
        private readonly Dictionary<int, NetworkClient> _clientsToDisconnect = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private bool _isRunning;
        private int _nextId;
        private DateTime _lastAliveMessageSent;
       

        internal GameServer(
            INetworkConfiguration networkConfiguration,
            TransportBase transportBase)
        {
            _networkConfiguration = networkConfiguration;
            _transportBase = transportBase;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;

        internal event Action<NetworkClient, byte[]> ClientConnected;
        internal event Action<int, string> ClientDisconnected;
        internal event Action<int> ClientReconnected;
        internal event Action<byte[]> SpawnHandlerReceived; 
        internal event Action<byte[]> SpawnReceived; 
        internal event Action<byte[]> NetworkMessageReceived; 
        internal event Action<int> ClientLostConnection;
        
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
            byteWriter.AddInt32(payload.Length);
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

            //_udpTransport = new UdpTransport(localEndPoint);
            
            _transportBase.StartTransport(localEndPoint);

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
            CheckTimeout();
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
                byteWriter.AddInt32((ushort)ENetworkMessageType.Disconnect);
                byteWriter.AddString(reason);

                Send(byteWriter.Data, client.RemoteEndpoint, ESendMode.Reliable);

                _networkClientsTable.Remove(clientId);
            }
        }
        
        internal void DisconnectPendingClient(int clientId, string reason)
        {
            if (_pendingClientsTable.TryGetValue(clientId, out var client))
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddInt32((ushort)ENetworkMessageType.Disconnect);
                byteWriter.AddString(reason);

                Send(byteWriter.Data, client.RemoteEndpoint, ESendMode.Reliable);

                _pendingClientsTable.Remove(clientId);
            }
        }
        
        internal void Stop()
        {
            _isRunning = false;
            _transportBase.Stop();
        }
        
        private async Task Receive()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _transportBase.ReceiveAsync();
                    var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                   // Debug.Log("Received");

                   _receiveMessagesQueue.Enqueue(incomeMessage);
                }
                catch (Exception e)
                {
                    //Debug.Log(e);
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
                        await _transportBase.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
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
                case ENetworkMessageType.ClientAliveCheck:
                    HandleAliveCheck(messagePayload);
                    break;
            }
        }

        private void HandleAliveCheck(byte[] messagePayload)
        {
            var byteReader = new ByteReader(messagePayload, 2);
            
            var clientId = byteReader.ReadInt32();

            var hasClient = ConnectedPlayers.TryGetValue(clientId, out var player);
            var hasPendingClient = _pendingClientsTable.TryGetValue(clientId, out var pendingClient);

            if (hasClient)
            {
                player.LastMessageReceived = DateTime.Now;
            }

            if (hasPendingClient)
            {
                pendingClient.LastMessageReceived = DateTime.Now;
            }

            Debug.Log($"server HandleAliveCheck for client = {player.Id}");
        }
        
        private void HandleClientReady(byte[] messagePayload)
        {
            var byteReader = new ByteReader(messagePayload, 2);
            var clientId = byteReader.ReadInt32();
            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;

            client.IsReady = true;
            
            _pendingClientsTable.Remove(clientId);
            
            _networkClientsTable.Add(clientId, client);
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

            if (ConnectedPlayers.Count == _networkConfiguration.MaxClients)
            {
                var byteWriter = new ByteWriter();
                
                byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
                byteWriter.AddUshort((ushort)EConnectionResult.Reject);
                byteWriter.AddString("Server is full");
                
                Send(byteWriter.Data, remoteEndPoint, ESendMode.Reliable);
                
                return;
            }
            
            if (messageType == ENetworkMessageType.ConnectionRequest)
            {
                var clientId = _nextId++;
                
                var networkClient = new NetworkClient(clientId, remoteEndPoint);

                var hasClient = _pendingClientsTable.TryGetValue(clientId, out var client);

                if (!hasClient)
                {
                    _pendingClientsTable.Add(clientId, networkClient);
                    
                    var byteWriter = new ByteWriter();
                    
                    byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
                    byteWriter.AddInt32(clientId);
                    byteWriter.AddString(remoteEndPoint.Address.ToString());
                    byteWriter.AddInt32(remoteEndPoint.Port);

                    Send(byteWriter.Data, networkClient, ESendMode.Reliable);
                    
                    //TODO: segregate client's credentials message 

                    var msgLength = messagePayload.Length - 2;
                    
                    var connectInfoPayload = new byte[msgLength];
                    
                    Buffer.BlockCopy(messagePayload, 2, connectInfoPayload, 0, msgLength);
                    networkClient.IsOnline = true;
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

                ClientDisconnected?.Invoke(clientId, "client leave server");
            }
        }

        private void CheckTimeout()
        {
            foreach (var networkClient in ConnectedPlayers.Values)
            {
                var diff = DateTime.Now - networkClient.LastMessageReceived;
                
                if (diff.TotalMilliseconds >= _networkConfiguration.ClientCheckAliveTimeOut && networkClient.IsOnline)
                {
                    networkClient.IsOnline = false;
                    
                    var byteWriter = new ByteWriter();
                    byteWriter.AddUshort((ushort)ENetworkMessageType.ClientLostConnection);
                    byteWriter.AddInt32(networkClient.Id);
                    Debug.Log($"client with id networkClient.Id lost connection");
                    ClientLostConnection?.Invoke(networkClient.Id);
                    
                    SendToAll(byteWriter.Data, ESendMode.Reliable);
                }
                
                if (diff.TotalMilliseconds >= _networkConfiguration.ServerClientDisconnectTime && !networkClient.IsOnline)
                {
                    _clientsToDisconnect.Add(networkClient.Id, networkClient);
                }
            }
            
            foreach (var networkClient in _pendingClientsTable.Values)
            {
                var diff = DateTime.Now - networkClient.LastMessageReceived;
                
                if (diff.TotalMilliseconds >= _networkConfiguration.ClientCheckAliveTimeOut && networkClient.IsOnline)
                {
                    networkClient.IsOnline = false;
                    
                    var byteWriter = new ByteWriter();
                    byteWriter.AddUshort((ushort)ENetworkMessageType.ClientLostConnection);
                    byteWriter.AddInt32(networkClient.Id);
                    Debug.Log($"client with id networkClient.Id lost connection");
                    ClientLostConnection?.Invoke(networkClient.Id);
                    
                    SendToAll(byteWriter.Data, ESendMode.Reliable);
                }
                
                if (diff.TotalMilliseconds >= _networkConfiguration.ServerClientDisconnectTime && !networkClient.IsOnline)
                {
                    _clientsToDisconnect.Add(networkClient.Id, networkClient);
                }
            }

            foreach (var client in _clientsToDisconnect.Values)
            {
                if(_networkClientsTable.ContainsKey(client.Id))
                    _networkClientsTable.Remove(client.Id);
                
                if (_pendingClientsTable.ContainsKey(client.Id))
                    _pendingClientsTable.Remove(client.Id);
            }

            foreach (var client in _clientsToDisconnect.Values)
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
                byteWriter.AddInt32(client.Id);
                byteWriter.AddString("disconnected due timeout");
                
                ClientDisconnected?.Invoke(client.Id, "disconnected due timeout");
                
                SendToAll(byteWriter.Data, ESendMode.Reliable);
            }
            
            if(_clientsToDisconnect.Count > 0)
                _clientsToDisconnect.Clear();

            if ((DateTime.Now - _lastAliveMessageSent).TotalMilliseconds >=
                _networkConfiguration.ServerCheckAliveTimeSent)
            {
                _lastAliveMessageSent = DateTime.Now;
                
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)ENetworkMessageType.ServerAliveCheck);
                
                SendToAll(byteWriter.Data, ESendMode.Reliable);
            }
        }
    }
}