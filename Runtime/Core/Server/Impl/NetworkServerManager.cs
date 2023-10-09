using System;
using System.Collections.Generic;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Server;
using PBUnityMultiplayer.Runtime.Configuration.Server.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Transport;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator.Impl;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Server.Impl
{
    public class NetworkServerManager : MonoBehaviour, 
        INetworkServerManager
    {
        [SerializeField] private DefaultServerConfiguration networkConfiguration;
        [SerializeField] private NetworkPrefabsBase networkPrefabsBase;
        [SerializeField] private bool useAuthentication;
        [SerializeField] private TransportBase transportBase;
        [SerializeField] private AuthenticationServiceBase serverAuthentication;

        private GameServer _server;
        private bool _running;
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedClients => _server.ClientsTable;
        public IEnumerable<NetworkClient> Clients => _server.Clients;
        public int Tick => _server.CurrentTick;
        public event Action ClientConnectedToServer;
        public event Action<NetworkClient> SeverAuthenticated;
        public event Action<int> ClientReady;
        public event Action<int> ClientDisconnected;
        public event Action<int> ClientConnected;
        public IServerConfiguration Configuration => networkConfiguration;

        private void Start()
        {
            _server = new GameServer(transportBase, networkConfiguration, new NetworkObjectIdGenerator());
        }

        public void StartServer()
        {
            _server.ClientDisconnected += OnClientDisconnected;
            _server.ClientConnected += OnClientReady;
            _server.ConnectionApproveCallback += OnClientConnected;
            
            _server.StartServer();
            
            _running = true;
        }

        public void StopServer() 
        {
            _running = false;
            
            _server.Stop();
        }

        public void SendMessage<T>(int networkClientId, T message, ESendMode sendMode) where T : struct
        {
            _server.SendMessage(networkClientId, message, sendMode);
        }

        public void SendMessage<T>(T message, ESendMode sendMode) where T : struct
        {
            foreach (var connectedClient in _server.Clients)
            {
                _server.SendMessage(connectedClient.Id, message, sendMode);
            }
        }

        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _server.RegisterMessageHandler(handler);
        }

        private AuthenticateResult OnClientConnected(int clientId, ArraySegment<byte> connectionMessage)
        {
            if (useAuthentication)
                return serverAuthentication.Authenticate(clientId, connectionMessage);
            
            return new AuthenticateResult(EConnectionResult.Success, "");
        }

        private void OnClientReady(int  id)
        {
            ClientReady?.Invoke(id);
        }

        private void OnClientDisconnected(int id)
        {
            ClientDisconnected?.Invoke(id);
        }
        
        private void FixedUpdate()
        {
            if(!_running)
                return;
            
            _server?.Tick();
        }

        private void OnDestroy()
        {
            if (_server != null)
            {
                _server.ClientDisconnected -= OnClientDisconnected;
                _server.ClientConnected -= OnClientReady;
                _server.ConnectionApproveCallback -= OnClientConnected;
            }
            
            _server?.Stop();
            _server?.Dispose();
        }
        private void OnDisable()
        {
            if (_server != null)
            {
                _server.ClientDisconnected -= OnClientDisconnected;
                _server.ClientConnected -= OnClientReady;
                _server.ConnectionApproveCallback -= OnClientConnected;
            }
            
            _server?.Stop();
            _server?.Dispose();
        }
    }
}