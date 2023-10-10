using System;
using System.Collections.Generic;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Client.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Client;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Transport;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Client.Impl
{
    public class NetworkClientManager : MonoBehaviour, 
        INetworkClientManager
    {
        [SerializeField] private DefaultClientConfiguration clientConfiguration;
        [SerializeField] private NetworkPrefabsBase networkPrefabsBase;
        [SerializeField] private TransportBase transportBase;
        
        private GameClient _client;
        private bool _isRunning;

        public IReadOnlyDictionary<int, NetworkClient> ClientsTable => _client.ConnectedPlayers;
        public IEnumerable<NetworkClient> Clients => _client.Clients;
        public event Action<int> ClientConnected;
        public event Action<int> ClientDisconnected;
        public event Action ClientStarted;
        public event Action ClientReady;
        public int Tick => _client.CurrentTick;
        public NetworkClient LocalClient => _client.LocalClient;

        private void Start()
        {
            _client = new GameClient(clientConfiguration, transportBase);
        }

        public void StartClient()
        {
            if(_isRunning)
                StopClient();
            
            _isRunning = true;
         
            _client.ClientConnected += OnClientConnected;
            _client.ClientDisconnected += OnClientDisconnected;
            _client.LocalClientAuthenticated += OnClientAuthenticated;
            _client.Start();
            
            ClientStarted?.Invoke();
        }
        
        public void StopClient()
        {
            _isRunning = false;
            _client.Stop();
        }

        public void ConnectToServer(string password)
        {
            _client.ConnectToServer(password);
        }

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            _client.SendMessage(message, sendMode);
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _client.RegisterMessageHandler(handler);
        }

        private void OnClientConnected(int clientId)
        {
            ClientConnected?.Invoke(clientId);
        }

        private void OnClientDisconnected(int clientId, string reason)
        {
            ClientDisconnected?.Invoke(clientId);
        }
        
        private void OnClientAuthenticated(EConnectionResult authenticateResult, string serverMessage)
        {
            switch (authenticateResult)
            {
                case EConnectionResult.Reject:
                    StopClient();
                    break;
                case EConnectionResult.Success:
                    ClientReady?.Invoke();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if(!_isRunning)
                return;
            
            _client?.Tick();
        }

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.ClientConnected -= OnClientConnected;
                _client.ClientDisconnected -= OnClientDisconnected;
                _client.LocalClientAuthenticated -= OnClientAuthenticated;
            }
          
            _client?.Stop();
            _client?.Dispose();
        }
        
        private void OnDisable()
        {
            if (_client != null)
            {
                _client.ClientConnected -= OnClientConnected;
                _client.ClientDisconnected -= OnClientDisconnected;
                _client.LocalClientAuthenticated -= OnClientAuthenticated;
            }
          
            _client?.Stop();
            _client?.Dispose();
        }
    }
}