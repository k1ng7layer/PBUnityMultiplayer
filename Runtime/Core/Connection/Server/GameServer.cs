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
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Server
{
    public class GameServer
    {
        private readonly INetworkPrefabsBase _networkPrefabsBase;
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly INetworkSpawnService _networkSpawnService;
        private readonly INetworkSpawnHandlerService _networkSpawnHandlerService;
        private readonly IIdGenerator<ushort> _networkObjectIdGenerator;
        private readonly INetworkSpawnedObjectsRepository _networkSpawnedObjectsRepository;
        private readonly IMessageHandlersService _messageHandlersService;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private UdpTransport _udpTransport;
        private bool _isRunning;
        private int _nextId;
       

        internal GameServer(
            INetworkConfiguration networkConfiguration, 
            INetworkSpawnService networkSpawnService,
            INetworkPrefabsBase networkPrefabsBase,
            IMessageHandlersService messageHandlersService,
            INetworkSpawnHandlerService networkSpawnHandlerService,
            INetworkSpawnedObjectsRepository networkSpawnedObjectsRepository,
            IIdGenerator<ushort> idGenerator)
        {
            _networkConfiguration = networkConfiguration;
            _networkSpawnService = networkSpawnService;
            _networkPrefabsBase = networkPrefabsBase;
            _messageHandlersService = messageHandlersService;
            _networkSpawnHandlerService = networkSpawnHandlerService;
            _networkSpawnedObjectsRepository = networkSpawnedObjectsRepository;
            _networkObjectIdGenerator = idGenerator;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;

        internal Action<NetworkClient, byte[]> ClientConnected;
        internal event Action<int> ClientDisconnected;
        internal event Action<int> ClientReconnected;

        public void SendMessageToAll<T>(T message, ESendMode sendMode)
        {
            var hasId = _messageHandlersService.TryGetHandlerId<T>(out var id);
            if(!hasId)
                return;

            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(id);
            byteWriter.AddInt(payload.Length);
            byteWriter.AddBytes(payload);

            foreach (var client in _networkClientsTable.Values)
            {
                Send(byteWriter.Data, client.RemoteEndpoint, sendMode);
            }
        }

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
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }

        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _networkSpawnHandlerService.RegisterHandler(handler);
        }
        
        public void Spawn<T>(
            int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation,
            T message) where T: struct
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var objectId = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(objectId, false);
            owner.AddOwnership(spawnedObject);

            var hasHandler = _networkSpawnHandlerService.TryGetHandlerId<T>(out var handlerId);

            if (!hasHandler)
                throw new Exception($"[{nameof(GameServer)}] can't process unregister handler");
            
            _networkSpawnedObjectsRepository.TryAdd(objectId, spawnedObject);
            
            var messageBytes = BinarySerializationHelper.Serialize(message);
            var byteWriter = new ByteWriter();
            
            _networkSpawnHandlerService.CallHandler(handlerId, messageBytes, spawnedObject);
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.SpawnHandler);
            byteWriter.AddInt(owner.Id);
            byteWriter.AddInt(prefabId);
            byteWriter.AddUshort(objectId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt(messageBytes.Length);
            byteWriter.AddBytes(messageBytes);

            SendToAll(byteWriter.Data, ESendMode.Reliable);
        }
        
        public void Spawn(
            int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation)
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var id = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(id, false);
            owner.AddOwnership(spawnedObject);

            _networkSpawnedObjectsRepository.TryAdd(id, spawnedObject);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.Spawn);
            byteWriter.AddInt(owner.Id);
            byteWriter.AddInt(prefabId);
            byteWriter.AddUshort(id);

            SendToAll(byteWriter.Data, ESendMode.Reliable);
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
            }
        }

        private void HandleSpawn(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
            var position = byteReader.ReadVector3();
            var rotation = byteReader.ReadQuaternion();
            var networkObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;
            
            if(!client.IsApproved)
                return;
            
            var objectId = _networkObjectIdGenerator.Next();
            
            networkObject.Spawn(objectId, false);
            client.AddOwnership(networkObject);

            var byteWriter = new ByteWriter();
            byteWriter.AddBytes(payload);
            byteWriter.AddUshort(objectId);
            
            SendToAll(byteWriter.Data, ESendMode.Reliable);
        }
        
        private void HandleSpawnHandler(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
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

            var objectId = _networkObjectIdGenerator.Next();
            
            client.AddOwnership(networkObject);
            networkObject.Spawn(objectId, false);
            
            _networkSpawnHandlerService.CallHandler(handlerId, messagePayload, networkObject);

            var byteWriter = new ByteWriter();
            
            byteWriter.AddBytes(payload);
            byteWriter.AddUshort(objectId);

            SendToAll(byteWriter.Data, ESendMode.Reliable);
        }
        
        private void HandleNetworkMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var networkMessageId = byteReader.ReadString();
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
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