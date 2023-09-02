using System;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Authentication.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl;
using PBUnityMultiplayer.Runtime.Helpers;
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
        
        public event Action ClientConnectedToServer;
        public event Action<NetworkClient> SeverAuthenticated;
        public event Action<int> SeverClientDisconnected;
        public event Action<int> SeverClientConnected;

        private void Awake()
        {
            _networkObjectIdGenerator = new NetworkObjectIdGenerator();
            _networkSpawnService = new NetworkSpawnService(networkPrefabsBase);
            _messageHandlersService = new NetworkMessageHandlersService();
            _networkSpawnHandlerService = new NetworkSpawnHandlerService();
        }

        public void StartServer()
        {
            _server = new GameServer(
                networkConfiguration);
            
            _server.ClientConnected += ServerHandleNewConnection;
            _server.SpawnHandlerReceived += HandleSpawnHandler;
            _server.SpawnReceived += HandleSpawn;
            _server.SpawnReceived += HandleNetworkMessage;
            
            AuthenticationServiceBase.OnAuthenticated += OnServerAuthenticated;
            
            _server.Start();
        }

        public void StopServer()
        {
            _server.ClientConnected -= ServerHandleNewConnection;
            _server.SpawnHandlerReceived -= HandleSpawnHandler;
            _server.SpawnReceived -= HandleSpawn;
            _server.SpawnReceived -= HandleNetworkMessage;
            
            AuthenticationServiceBase.OnAuthenticated -= OnServerAuthenticated;
            
            _server.Stop();
        }

        public void Spawn<T>(int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation, 
            T message) where T : struct
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var objectId = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(objectId, false);
            owner.AddOwnership(spawnedObject);

            var hasHandler = _networkSpawnHandlerService.TryGetHandlerId<T>(out var handlerId);

            if (!hasHandler)
                throw new Exception($"[{nameof(NetworkServerManager)}] can't process unregister handler");

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

            _server.SendToAll(byteWriter.Data, ESendMode.Reliable);
        }

        public void Spawn(int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation)
        {
            var spawnedObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            var id = _networkObjectIdGenerator.Next();
            spawnedObject.Spawn(id, false);
            owner.AddOwnership(spawnedObject);

            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.Spawn);
            byteWriter.AddInt(owner.Id);
            byteWriter.AddInt(prefabId);
            byteWriter.AddUshort(id);

            _server.SendToAll(byteWriter.Data, ESendMode.Reliable);
        }

        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }

        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _networkSpawnHandlerService.RegisterHandler(handler);
        }
        
        private void ServerHandleNewConnection(NetworkClient networkClient, byte[] payload)
        {
            SeverClientConnected?.Invoke(networkClient.Id);

            if (useAuthentication)
            {
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
            byteWriter.AddInt(client.Id);
            byteWriter.AddString(authenticateResult.Message);
            SeverAuthenticated?.Invoke(client);
            
            switch (result)
            {
                case EConnectionResult.Success:
                    client.IsApproved = true;
                    break;
                case EConnectionResult.Reject:
                case EConnectionResult.TimeOut:
                    SeverClientDisconnected?.Invoke(client.Id);
                    _server.DisconnectClient(client.Id, authenticateResult.Message);
                    break;
            }
            _server.Send(byteWriter.Data, client, ESendMode.Reliable);
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

            var objectId = _networkObjectIdGenerator.Next();
            
            client.AddOwnership(networkObject);
            networkObject.Spawn(objectId, false);
            
            _networkSpawnHandlerService.CallHandler(handlerId, messagePayload, networkObject);

            var byteWriter = new ByteWriter();
            
            byteWriter.AddBytes(payload);
            byteWriter.AddUshort(objectId);

            _server.SendToAll(byteWriter.Data, ESendMode.Reliable);
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
            
            var objectId = _networkObjectIdGenerator.Next();
            
            networkObject.Spawn(objectId, false);
            client.AddOwnership(networkObject);

            var byteWriter = new ByteWriter();
            byteWriter.AddBytes(payload);
            byteWriter.AddUshort(objectId);
            
            _server.SendToAll(byteWriter.Data, ESendMode.Reliable);
        }
        
        private void HandleNetworkMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var networkMessageId = byteReader.ReadString();
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }
        
        private void FixedUpdate()
        {
            _server?.Update();
        }
    }
}