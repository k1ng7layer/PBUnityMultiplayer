using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Client
{
    public class GameClient : Peer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly INetworkPrefabsBase _networkPrefabsBase;
        private readonly INetworkSpawnHandlerService _networkSpawnHandlerService;
        private readonly INetworkSpawnService _networkSpawnService;
        private readonly IMessageHandlersService _messageHandlersService;
        
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private UdpTransport _udpTransport;
        private IPEndPoint _serverEndPoint;
        private IPEndPoint _localEndPoint;
        private bool _isRunning;
        
        
        internal GameClient(INetworkConfiguration networkConfiguration, 
            INetworkPrefabsBase networkPrefabsBase,
            INetworkSpawnHandlerService networkSpawnHandlerService,
            INetworkSpawnService networkSpawnService)
        {
            _networkConfiguration = networkConfiguration;
            _networkPrefabsBase = networkPrefabsBase;
            _networkSpawnHandlerService = networkSpawnHandlerService;
            _networkSpawnService = networkSpawnService;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;
      
        public NetworkClient LocalClient { get; private set; }

        internal event Action LocalClientConnected;
        internal event Action LocalClientDisconnected;
        internal event Action LocalClientReconnected;
        internal event Action<int> ClientConnected;
        internal event Action<int> ClientDisconnected;
        internal event Action<int> ClientReconnected;
        internal event Action<EConnectionResult, string> LocalClientAuthenticated;

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();

            var id = typeof(T).FullName;
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(id);
            byteWriter.AddInt(payload.Length);
            byteWriter.AddBytes(payload);
            
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
            
            //UniTask.RunOnThreadPool(async () => { await Receive(); }, false);
            //UniTask.RunOnThreadPool(async () => { await ProcessSendQueue(); }, false);

            Task.Run(async () => await Receive());
            Task.Run(async () => await ProcessSendQueue());
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }
        
        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _networkSpawnHandlerService.RegisterHandler(handler);
        }

        internal void Update()
        {
            ProcessReceiveQueue();
            //ProcessSendQueue();
            //Receive();
        }

        public void Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            var prefab = _networkPrefabsBase.Get(prefabId);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.Spawn);
            byteWriter.AddInt(LocalClient.Id);
            byteWriter.AddInt(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
          
            Send(byteWriter.Data, _serverEndPoint, ESendMode.Reliable);
        }
        
        public void Spawn<T>(int prefabId, Vector3 position, Quaternion rotation, T message)
        {
            var prefab = _networkPrefabsBase.Get(prefabId);
            var hasHandler = _networkSpawnHandlerService.TryGetHandlerId<T>(out var handlerId);

            if (!hasHandler)
                throw new Exception($"[{nameof(GameClient)}] can't process unregister handler");
            
            var messageBytes = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.SpawnHandler);
            byteWriter.AddInt(LocalClient.Id);
            byteWriter.AddInt(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt(messageBytes.Length);
            byteWriter.AddBytes(messageBytes);
            
            Debug.Log($"Spawn count = {byteWriter.Data.Length}");
            
            Send(byteWriter.Data, _serverEndPoint, ESendMode.Reliable);
        }

        internal void Send(
            byte[] data, 
            ESendMode sendMode
        )
        {
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
        }
        
        private async UniTask Receive()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpTransport.ReceiveAsync();

                    var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                
                    _receiveMessagesQueue.Enqueue(incomeMessage);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
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
                            Debug.Log("BeginSent");
                            await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
                            Debug.Log("Sent");
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
            }
        }

        private void HandleSpawnMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
            var position = byteReader.ReadVector3();
            var rotation = byteReader.ReadQuaternion();
            var objectId = byteReader.ReadUshort();
            
            var networkObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

            if(!hasClient)
                return;
            
            var isLocalObject = LocalClient.Id == objectId;
            
            networkObject.Spawn(objectId, isLocalObject);
            
            client.AddOwnership(networkObject);
        }

        private void HandleSpawnHandlerMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
            var objectId = byteReader.ReadUshort();
            var position = byteReader.ReadVector3();
            var rotation = byteReader.ReadQuaternion();
            var handlerId = byteReader.ReadString();
            var payloadSize = byteReader.ReadInt32();
            var messagePayload = byteReader.ReadBytes(payloadSize);
            var networkObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            
            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);
            
            //TODO:
            if(!hasClient)
                return;

            client.AddOwnership(networkObject);
            networkObject.Spawn(objectId, clientId == LocalClient.Id);
            
            _networkSpawnHandlerService.CallHandler(handlerId, messagePayload, networkObject);
        }

        private void HandleNetworkMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var networkMessageId = byteReader.ReadString();
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }

        private void HandleConnectionAuthentication(byte[] connPayload)
        {
            var byteReader = new ByteReader(connPayload, 2);
            var result = (EConnectionResult)byteReader.ReadUshort();
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString();
            LocalClient = new NetworkClient(clientId, _localEndPoint);
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

            var hasPlayer = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                client.IsOnline = false;

                if (clientId == LocalClient.Id)
                {
                    LocalClientDisconnected?.Invoke();
                }
                else
                {
                    ClientDisconnected?.Invoke(clientId);
                }
            }
        }
    }
}