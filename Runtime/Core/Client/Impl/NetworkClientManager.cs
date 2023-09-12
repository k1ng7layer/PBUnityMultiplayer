using System;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Connection.Client;
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
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Client.Impl
{
    public class NetworkClientManager : MonoBehaviour, 
        INetworkClientManager
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        [SerializeField] private NetworkPrefabsBase networkPrefabsBase;

        private INetworkSpawnHandlerService _networkSpawnHandlerService;
        private INetworkSpawnService _networkSpawnService;
        private IMessageHandlersService _messageHandlersService;
        
        private EventHandler<AuthenticateResult> _clientConnectionEventHandler;
        
        private GameClient _client;
        private bool _isRunning;

        public event Action ClientConnectedToServer;
        public event Action ServerLostConnection;

        private void Awake()
        {
            _messageHandlersService = new NetworkMessageHandlersService();
            _networkSpawnHandlerService = new NetworkSpawnHandlerService();
            _networkSpawnService = new NetworkSpawnService(networkPrefabsBase);
        }

        public NetworkClient LocalClient => _client.LocalClient;

        public void StartClient()
        {
            if(_isRunning)
                StopClient();
            
            _isRunning = true;
            _client = new GameClient(networkConfiguration);
            _client.Start();
        }

        public UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password)
        {
            if (!_isRunning)
                throw new Exception($"[{nameof(NetworkClientManager)}] must call StartClient first");
                    
            _client.LocalClientConnected += OnLocalClientConnected;
            _client.LocalClientAuthenticated += OnClientAuthenticated;
            _client.SpawnReceived += HandleSpawnMessage;
            _client.SpawnHandlerReceived += HandleSpawnHandlerMessage;
            _client.NetworkMessageReceived += HandleNetworkMessage;
            _client.ServerLostConnection += OnServerLostConnection;
            
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var writer = new ByteWriter(sizeof(ushort) + pwdBytes.Length);
            
            writer.AddUshort((ushort)ENetworkMessageType.ConnectionRequest);
            writer.AddString(password);

            var tcs = new UniTaskCompletionSource<AuthenticateResult>();
            var cts = new CancellationTokenSource(networkConfiguration.ConnectionTimeOut);
            
            cts.Token.Register(() => { tcs.TrySetResult(new AuthenticateResult(EConnectionResult.TimeOut, null)); });
            
            _clientConnectionEventHandler = (_, result) =>
            {
                tcs.TrySetResult(result);
            };
            
            _client.Send(writer.Data, serverEndPoint, ESendMode.Reliable);
            
            return tcs.Task;
        }
        
        public void StopClient()
        {
            _client.Stop();
            _client.LocalClientConnected -= OnLocalClientConnected;
            _client.LocalClientAuthenticated -= OnClientAuthenticated;
            _client.SpawnReceived -= HandleSpawnMessage;
            _client.SpawnHandlerReceived -= HandleSpawnHandlerMessage;
            _client.NetworkMessageReceived -= HandleNetworkMessage;
            _client.ServerLostConnection -= OnServerLostConnection;
        }

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            _client.SendMessage(message, sendMode);
        }

        public void Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            var prefab = networkPrefabsBase.Get(prefabId);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.Spawn);
            byteWriter.AddInt32(_client.LocalClient.Id);
            byteWriter.AddInt32(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
          
            _client.Send(byteWriter.Data, ESendMode.Reliable);
        }

        public void Spawn<T>(int prefabId, Vector3 position, Quaternion rotation, T message)
        {
            var prefab = networkPrefabsBase.Get(prefabId);
            var hasHandler = _networkSpawnHandlerService.TryGetHandlerId<T>(out var handlerId);

            if (!hasHandler)
                throw new Exception($"[{nameof(GameClient)}] can't process unregister handler");
            
            var messageBytes = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.SpawnHandler);
            byteWriter.AddInt32(_client.LocalClient.Id);
            byteWriter.AddInt32(prefabId);
            byteWriter.AddVector3(position);
            byteWriter.AddQuaternion(rotation);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt32(messageBytes.Length);
            byteWriter.AddBytes(messageBytes);
            
            Debug.Log($"Spawn count = {byteWriter.Data.Length}");
            
            _client.Send(byteWriter.Data, ESendMode.Reliable);
        }

        private void OnLocalClientConnected()
        {
            ClientConnectedToServer?.Invoke();
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }
        
        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _networkSpawnHandlerService.RegisterHandler(handler);
        }
        
        private void OnClientAuthenticated(EConnectionResult authenticateResult, string serverMessage)
        {
            if (authenticateResult == EConnectionResult.Reject)
            {
                StopClient();
            }
            
            _clientConnectionEventHandler?.Invoke(this, new AuthenticateResult(authenticateResult, serverMessage));
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
            var hasClient = _client.ConnectedPlayers.TryGetValue(clientId, out var client);

            if(!hasClient)
                return;
            
            var isLocalObject = _client.LocalClient.Id == objectId;
            
            networkObject.Spawn(objectId, isLocalObject);
            
            client.AddOwnership(networkObject);
        }
        
        private void HandleSpawnHandlerMessage(byte[] payload)
        {
            Debug.Log($"[{nameof(NetworkClientManager)}]received spawn handler size = {payload.Length}");
            var byteReader = new ByteReader(payload, 2);
            
            var clientId = byteReader.ReadInt32();
            var prefabId = byteReader.ReadInt32();
            var position = byteReader.ReadVector3();
            var rotation = byteReader.ReadQuaternion();
            var handlerId = byteReader.ReadString();
            var payloadSize = byteReader.ReadInt32();
            var messagePayload = byteReader.ReadBytes(payloadSize);
            var objectId = byteReader.ReadUshort();
            var networkObject = _networkSpawnService.Spawn(prefabId, position, rotation);
            
            var hasClient = _client.ConnectedPlayers.TryGetValue(clientId, out var client);
            
            //TODO:
            //TODO:
            if(!hasClient)
                return;

            Debug.Log($"client HandleSpawnHandlerMessage, id = {objectId}");
            networkObject.Spawn(objectId, clientId == _client.LocalClient.Id);
            client.AddOwnership(networkObject);
            
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

        private void OnServerLostConnection()
        {
            ServerLostConnection?.Invoke();
        }

        private void FixedUpdate()
        {
            _client?.Update();
        }
    }
}