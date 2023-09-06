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
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Client
{
    public class GameClient : Peer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private UdpTransport _udpTransport;
        private IPEndPoint _serverEndPoint;
        private IPEndPoint _localEndPoint;
        private bool _isRunning;
        
        internal GameClient(INetworkConfiguration networkConfiguration)
        {
            _networkConfiguration = networkConfiguration;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;
      
        public NetworkClient LocalClient { get; private set; }

        internal event Action LocalClientConnected;
        internal event Action<string> LocalClientDisconnected;
        internal event Action LocalClientReconnected;
        internal event Action<int> ClientConnected;
        internal event Action<int, string> ClientDisconnected;
        internal event Action<int> ClientReconnected;
        internal event Action<byte[]> SpawnReceived; 
        internal event Action<byte[]> NetworkMessageReceived; 
        internal event Action<byte[]> SpawnHandlerReceived; 
        internal event Action<EConnectionResult, string> LocalClientAuthenticated;
        internal event Action<int> ClientLostConnection;
        internal event Action ServerLostConnection;

        private DateTime _lastMessageReceivedFromServer;

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();

            var id = typeof(T).FullName;
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(id);
            byteWriter.AddInt32(payload.Length);
            byteWriter.AddBytes(payload);
            
            LocalClient.LastMessageSent = DateTime.Now;
            
            Send(byteWriter.Data, _serverEndPoint, sendMode);
        }
        
        internal void Start()
        {
            var localIpResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!localIpResult)
                throw new Exception($"[{nameof(GameClient)}] invalid local ip address, check config");
            
            if(_isRunning)
                throw new Exception($"[{nameof(GameClient)}] can't start server when network manager already running ");
            
            _localEndPoint = new IPEndPoint(ip, _networkConfiguration.LocalPort);

            var serverIpResult = IPAddress.TryParse(_networkConfiguration.ServerIp, out var serverIp);
            
            if (!serverIpResult)
                throw new Exception($"[{nameof(GameClient)}] invalid server ip address, check config");

            _serverEndPoint = new IPEndPoint(serverIp, _networkConfiguration.ServerPort);
            
            _udpTransport = new UdpTransport(_localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;
            
            Task.Run(async () => await Receive());
            Task.Run(async () => await ProcessSendQueue());
        }

        internal void Update()
        {
            ProcessReceiveQueue();
            SendAliveCheck();
        }

        internal void Send(
            byte[] data, 
            ESendMode sendMode
        )
        {
            LocalClient.LastMessageSent = DateTime.Now;
            
            var outcomeMessage = new OutcomePendingMessage(   data, _serverEndPoint, sendMode);
            
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

        internal void Stop()
        {
            _isRunning = false;
            _udpTransport.Stop();
            _networkClientsTable.Clear();
        }
        
        private async UniTask Receive()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpTransport.ReceiveAsync();

                    _lastMessageReceivedFromServer = DateTime.Now;
                    
                    var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                
                    _receiveMessagesQueue.Enqueue(incomeMessage);
                }
                catch (Exception e)
                {
                    //Debug.LogError(e);
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
            while (_isRunning)
            {
                while (_sendMessagesQueue.Count > 0)
                {
                    try
                    {
                        var canDequeue = _sendMessagesQueue.TryDequeue(out var message);
                
                        if (canDequeue)
                        {
                            //Debug.Log("BeginSent");
                            await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
                            //Debug.Log("Sent");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }
        
        private void HandleIncomeMessage(IncomePendingMessage incomePendingMessage)
        {
            var messageType = MessageHelper.GetMessageType(incomePendingMessage.Payload);
            var messagePayload = incomePendingMessage.Payload;
            switch (messageType)
            {
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(messagePayload);
                    break;
                case ENetworkMessageType.ClientConnected:
                    HandleNewConnection(messagePayload);
                    break;
                case ENetworkMessageType.ClientReconnected:
                    HandleClientReconnected(messagePayload);
                    break;
                case ENetworkMessageType.Custom:
                    break;
                case ENetworkMessageType.AuthenticationResult:
                    HandleConnectionAuthentication(messagePayload);
                    break;
                case ENetworkMessageType.NetworkMessage:
                    HandleNetworkMessage(messagePayload);
                    break;
                case ENetworkMessageType.SpawnHandler:
                    HandleSpawnHandlerMessage(messagePayload);
                    break;
                case ENetworkMessageType.Spawn:
                    HandleSpawnMessage(messagePayload);
                    break;
                case ENetworkMessageType.ClientLostConnection:
                    HandleClientLostConnection(messagePayload);
                    break;
                case ENetworkMessageType.ServerAliveCheck:
                    HandleServerAliveCheck(messagePayload);
                    break;
            }
        }

        private void HandleServerAliveCheck(byte[] payload)
        {
            _lastMessageReceivedFromServer = DateTime.Now;
        }

        private void HandleClientLostConnection(byte[] payload)
        {
            var byteReader = new ByteReader(payload);
            var clientId = byteReader.ReadInt32();
            var hasClient = ConnectedPlayers.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;

            client.IsOnline = false;
            
            ClientLostConnection?.Invoke(clientId);
        }

        private void HandleSpawnMessage(byte[] payload)
        {
            SpawnReceived?.Invoke(payload);
        }

        private void HandleSpawnHandlerMessage(byte[] payload)
        {
            SpawnHandlerReceived?.Invoke(payload);
        }

        private void HandleNetworkMessage(byte[] payload)
        {
            NetworkMessageReceived?.Invoke(payload);
        }

        private void HandleConnectionAuthentication(byte[] connPayload)
        {
            var byteReader = new ByteReader(connPayload, 2);
            var result = (EConnectionResult)byteReader.ReadUshort();
            var reason = byteReader.ReadString();
            
            Debug.Log($"client HandleConnectionAuthentication");

            if (result == EConnectionResult.Success)
            {
                var clientId = byteReader.ReadInt32();
                LocalClient = new NetworkClient(clientId, _localEndPoint);

                var byteWriter = new ByteWriter();
                
                byteWriter.AddUshort((ushort)ENetworkMessageType.ClientReady);
                byteWriter.AddInt32(clientId);
            }
            
            LocalClientAuthenticated?.Invoke(result, reason);
        }

        private void HandleNewConnection(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload, 2);
            var clientId = byteReader.ReadInt32();
            
            var playerIpString = byteReader.ReadString(out _);
            var playerPort = byteReader.ReadInt32();
                    
            var parseResult = IPAddress.TryParse(playerIpString, out var ipResult);
                    
            if(!parseResult)
                return;

            var remoteEndpoint = new IPEndPoint(ipResult, playerPort);
                    
            var networkClient = new NetworkClient(clientId, remoteEndpoint);
            
            _networkClientsTable.Add(clientId, networkClient);

            if (Equals(remoteEndpoint, _localEndPoint))
            {
                //LocalClient = networkClient;
                LocalClientConnected?.Invoke();
            }
            else
            {
                ClientConnected?.Invoke(clientId);
            }
        }

        private void HandleClientReconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasClient)
            {
                if (client.Id == LocalClient.Id)
                {
                    LocalClientReconnected?.Invoke();
                }
                else
                {
                    ClientReconnected?.Invoke(clientId);
                }
            }
        }

        private void HandleClientDisconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString();

            var hasPlayer = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                if (clientId == LocalClient.Id)
                {
                    client.IsOnline = false;
                    LocalClientDisconnected?.Invoke(reason);
                }
                else
                {
                    ClientDisconnected?.Invoke(clientId, reason);
                    _networkClientsTable.Remove(clientId);
                }
            }
        }

        private void SendAliveCheck()
        {
            if(!_isRunning || LocalClient == null)
                return;
            
            var clientCheckAliveTime = _networkConfiguration.ClientCheckAliveTime;
            var serverCheckAliveTime = _networkConfiguration.ServerCheckAliveTimeOut;

            if ((DateTime.Now - LocalClient.LastMessageSent).TotalMilliseconds >= clientCheckAliveTime)
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)ENetworkMessageType.ClientAliveCheck);
                byteWriter.AddInt32(LocalClient.Id);
                
                LocalClient.LastMessageSent = DateTime.Now;
                Debug.Log($"client SendAliveCheck");
                Send(byteWriter.Data, ESendMode.Reliable);
            }

            if ((DateTime.Now - _lastMessageReceivedFromServer).TotalMilliseconds >= serverCheckAliveTime)
            {
                ServerLostConnection?.Invoke();
                Stop();
            }
        }
    }
}