using System;
using System.Collections.Generic;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Authentication.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator.Impl;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Server.Impl
{
    public class NetworkServerManager : MonoBehaviour, 
        INetworkServerManager
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        [SerializeField] private NetworkPrefabsBase networkPrefabsBase;
        [SerializeField] private bool useAuthentication;
        [SerializeField] private TransportBase transportBase;
        
        private INetworkSpawnService _networkSpawnService;
        private IIdGenerator<ushort> _networkObjectIdGenerator;
        private INetworkSpawnHandlerService _networkSpawnHandlerService;
        private IMessageHandlersService _messageHandlersService;
        
        private AuthenticationServiceBase _serverAuthentication;
        private GameServer _server;

        public AuthenticationServiceBase AuthenticationServiceBase
        {
            get
            {
                if (_serverAuthentication == null)
                    _serverAuthentication = new ServerAuthentication();

                return _serverAuthentication;
            }
            set => _serverAuthentication = value;
        }

        public IReadOnlyDictionary<int, NetworkClient> ConnectedClients => _server.ConnectedPlayers;
        public event Action ClientConnectedToServer;
        public event Action<NetworkClient> SeverAuthenticated;
        public event Action<NetworkClient> ClientReadyToWork;
        public event Action<int, string> SeverClientDisconnected;
        public event Action<int> SeverClientConnected;
        public event Action<int> ClientLostConnection;

        private void Awake()
        {
            _networkObjectIdGenerator = new NetworkObjectIdGenerator();
            _networkSpawnService = new NetworkSpawnService(networkPrefabsBase);
            _messageHandlersService = new NetworkMessageHandlersService();
            _networkSpawnHandlerService = new NetworkSpawnHandlerService();
        }

        public void StartServer()
        {
            _server = new GameServer(networkConfiguration, transportBase);
            
            _server.ClientConnected += ServerHandleNewConnection;
            _server.SpawnHandlerReceived += HandleSpawnHandler;
            _server.SpawnReceived += HandleSpawn;
            _server.NetworkMessageReceived += HandleNetworkMessage;
            _server.ClientLostConnection += OnClientLostConnection;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.ClientReady += OnClientReady;
            
            AuthenticationServiceBase.OnAuthenticated += OnServerAuthenticated;
            
            _server.Start();
        }

        public void StopServer()
        {
            _server.ClientConnected -= ServerHandleNewConnection;
            _server.SpawnHandlerReceived -= HandleSpawnHandler;
            _server.SpawnReceived -= HandleSpawn;
            _server.SpawnReceived -= HandleNetworkMessage;
            _server.ClientReady -= OnClientReady;
            
            AuthenticationServiceBase.OnAuthenticated -= OnServerAuthenticated;
            
            _server.Stop();
        }

        public NetworkObject Spawn<T>(int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation, 
            T message) where T : struct
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var objectId = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(objectId, owner.Id, false);
            owner.AddOwnership(spawnedObject);

            var handlerId = typeof(T).FullName;

            // if (!hasHandler)
            //     throw new Exception($"[{nameof(NetworkServerManager)}] can't process unregister handler");

            var messageBytes = BinarySerializationHelper.Serialize(message);
            var byteWriter = new ByteWriter();
            
           // _networkSpawnHandlerService.CallHandler(handlerId, messageBytes, spawnedObject);
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.SpawnHandler);
            byteWriter.AddInt32(owner.Id);
            byteWriter.AddInt32(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt32(messageBytes.Length);
            byteWriter.AddBytes(messageBytes);
            byteWriter.AddUshort(objectId);
            Debug.Log($"client Spawn, id = {objectId}");
            _server.SendToAllApprovedClients(byteWriter.Data, ESendMode.Reliable);

            return spawnedObject;
        }

        public NetworkObject Spawn(int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation)
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var id = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(id, owner.Id, false);
            owner.AddOwnership(spawnedObject);

            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.Spawn);
            byteWriter.AddInt32(owner.Id);
            byteWriter.AddInt32(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddUshort(id);

            _server.SendToAllApprovedClients(byteWriter.Data, ESendMode.Reliable);
            
            return spawnedObject;
        }

        public void SendMessage<T>(T message, int networkClientId) where T : struct
        {
            _server.SendMessage<T>(message, networkClientId, ESendMode.Reliable);
        }

        public void SendMessage<T>(T message) where T : struct
        {
            foreach (var connectedClient in ConnectedClients)
            {
                _server.SendMessage<T>(message, connectedClient.Value.Id, ESendMode.Reliable);
            }
        }

        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }

        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _networkSpawnHandlerService.RegisterHandler(handler);
        }

        private void OnClientReady(NetworkClient networkClient)
        {
            ClientReadyToWork?.Invoke(networkClient);
        }
        
        private void ServerHandleNewConnection(NetworkClient networkClient, byte[] payload)
        {
            SeverClientConnected?.Invoke(networkClient.Id);

            if (useAuthentication)
            {
                Debug.Log($"Authenticate start");
                AuthenticationServiceBase.Authenticate(networkClient, payload);
            }
            else
            {
                SeverAuthenticated?.Invoke(networkClient);
            }
        }
        
        private void OnServerAuthenticated(AuthenticateResult authenticateResult, NetworkClient client)
        {
            //TODO: send message to all clients
            var result = authenticateResult.ConnectionResult;
            var byteWriter = new ByteWriter();

            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)result);
            byteWriter.AddInt32(client.Id);
            byteWriter.AddString(authenticateResult.Message);
            client.LastMessageReceived = DateTime.Now;
            Debug.Log($"OnServerAuthenticated = {result}");
            switch (result)
            {
                case EConnectionResult.Success:
                    client.IsApproved = true;
                    break;
                case EConnectionResult.Reject:
                case EConnectionResult.TimeOut:
                    SeverClientDisconnected?.Invoke(client.Id, "Timeout");
                    _server.DisconnectPendingClient(client.Id, authenticateResult.Message);
                    break;
            }
            _server.Send(byteWriter.Data, client, ESendMode.Reliable);
            
            SeverAuthenticated?.Invoke(client);
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
            var hasClient = _server.ConnectedPlayers.TryGetValue(clientId, out var client);
            
            //TODO:
            if(!hasClient)
                return;

            client.LastMessageReceived = DateTime.Now;
            
            var objectId = _networkObjectIdGenerator.Next();
            
            networkObject.Spawn(objectId, clientId, false);
            client.AddOwnership(networkObject);
            
            _networkSpawnHandlerService.CallHandler(handlerId, messagePayload, networkObject);

            var byteWriter = new ByteWriter();
            
            // byteWriter.AddBytes(byteReader.Data);
            // byteWriter.AddUshort(objectId);
            
            byteWriter.AddUshort((ushort)(ENetworkMessageType.SpawnHandler));
            byteWriter.AddInt32(clientId);
            byteWriter.AddInt32(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt32(payloadSize);
            byteWriter.AddBytes(messagePayload);
            byteWriter.AddUshort(objectId);
            Debug.Log($"server HandleSpawnHandlerMessage, id = {objectId}, data count = {byteWriter.Data.Length}");
            _server.SendToAllApprovedClients(byteWriter.Data, ESendMode.Reliable);
        }
        
        private void HandleSpawn(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
            var position = byteReader.ReadVector3();
            var rotation = byteReader.ReadQuaternion();
            var networkObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var hasClient = _server.ConnectedPlayers.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;
            
            if(!client.IsApproved)
                return;
            
            client.LastMessageReceived = DateTime.Now;
            var objectId = _networkObjectIdGenerator.Next();
            
            networkObject.Spawn(objectId, clientId, false);
            client.AddOwnership(networkObject);

            var byteWriter = new ByteWriter();
            byteWriter.AddBytes(payload);
            byteWriter.AddUshort(objectId);
            
            _server.SendToAllApprovedClients(byteWriter.Data, ESendMode.Reliable);
        }
        
        private void HandleNetworkMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 6);
            var networkMessageId = byteReader.ReadString();
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }

        private void OnClientLostConnection(int id)
        {
            ClientLostConnection?.Invoke(id);
        }

        private void OnClientDisconnected(int id, string reason)
        {
            SeverClientDisconnected?.Invoke(id, reason);
        }
        
        private void FixedUpdate()
        {
            _server?.Update();
        }
    }
}