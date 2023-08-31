using System;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Authentication.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Client;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Impl
{
    public class NetworkManager : MonoBehaviour, 
        INetworkManager
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        [SerializeField] private NetworkPrefabsBase networkPrefabsBase;
        [SerializeField] private bool useAuthentication;
        
        private UdpTransport _udpTransport;
        private GameServer _server;
        private GameClient _client;
        private EventHandler<AuthenticateResult> _clientConnectionEventHandler;
        private AuthenticationServiceBase _serverAuthentication;
        internal IMessageHandlersService _messageHandlersService;

        private readonly Func<byte[], EConnectionResult> _connectionApprovalCallback;

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

        public GameServer Server
        {
            get
            {
                if (_server == null)
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
                        spawnedObjectRepository);
                }
                   

                return _server;
            }
        }

        public GameClient Client
        {
            get
            {
                if (_client == null)
                {
                    var spawnHandlerService = new NetworkSpawnHandlerService();
                    _client = new GameClient(
                        networkConfiguration, 
                        networkPrefabsBase, 
                        spawnHandlerService);
                }
                
                return _client;
            }
        }
        
        public event Action ClientConnectedToServer;
        public event Action<NetworkClient> SeverAuthenticated;
        public event Action<int> SeverClientDisconnected;
        public event Action<int> SeverClientConnected;
        public event Action<AuthenticateResult> ClientAuthenticated;

        public void StartServer()
        {
            Server.Start();
            
            _server.ClientConnected += ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated += OnServerAuthenticated;
        }
        
        public void StopServer()
        {
            Server.Stop();
            Server.ClientConnected -= ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated -= OnServerAuthenticated;
        }
        
        private void StartClient()
        {
            Client.Start();

            Client.LocalClientConnected += OnLocalClientConnected;
            Client.LocalClientAuthenticated += OnClientAuthenticated;
        }

        public void StopClient()
        {
            _client.Stop();
            _client.LocalClientConnected -= OnLocalClientConnected;
            _client.LocalClientAuthenticated -= OnClientAuthenticated;
        }

        public UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password)
        {
            StartClient();
            
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

        // public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        // {
        //     MessageHandlersService.RegisterHandler(handler);
        // }
        
        private void OnLocalClientConnected()
        {
            ClientConnectedToServer?.Invoke();
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

        private void OnClientAuthenticated(EConnectionResult authenticateResult, string serverMessage)
        {
            if (authenticateResult == EConnectionResult.Reject)
            {
                StopClient();
            }
            
            _clientConnectionEventHandler?.Invoke(this, new AuthenticateResult(authenticateResult, serverMessage));
        }

        private void FixedUpdate()
        {
            _client?.Update();

            _server?.Update();
        }
    }
}