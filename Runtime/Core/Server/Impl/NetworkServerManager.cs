using System;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Authentication.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
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

        private GameServer _server;
        private AuthenticationServiceBase _serverAuthentication;
        
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
        
        public void StartServer()
        {
            var spawnedObjectRepository = new NetworkSpawnedObjectsRepository();
            var serverSpawnService = new NetworkSpawnService(networkPrefabsBase);
            var networkMessageService = new NetworkMessageHandlersService();
            var spawnHandlerService = new NetworkSpawnHandlerService();
                    
            _server = new GameServer(
                networkConfiguration, 
                serverSpawnService, 
                networkPrefabsBase, 
                networkMessageService,
                spawnHandlerService,
                spawnedObjectRepository, new NetworkObjectIdGenerator()
                );
            
            _server.ClientConnected += ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated += OnServerAuthenticated;
            
            _server.Start();
        }

        public void StopServer()
        {
            _server.ClientConnected -= ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated -= OnServerAuthenticated;
            
            _server.Stop();
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _server.RegisterMessageHandler(handler);
        }

        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _server.RegisterSpawnHandler(handler);
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
        
        private void FixedUpdate()
        {
            _server?.Update();
        }
    }
}