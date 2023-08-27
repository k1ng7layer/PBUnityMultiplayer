using System;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Helpers;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Impl;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Authentication.Impl;
using PBUnityMultiplayer.Runtime.Core.Connection.Client;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Impl
{
    public class NetworkManager : MonoBehaviour, 
        INetworkManager
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        [SerializeField] private bool useAuthentication;
        
        private UdpTransport _udpTransport;
        private GameServer _server;
        private GameClient _client;
        private EventHandler<AuthenticateResult> _clientConnectionEventHandler;
        private readonly Func<byte[], EConnectionResult> _connectionApprovalCallback;
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
        public event Action<AuthenticateResult> ClientAuthenticated;

        public void StartServer()
        {
            _server = new GameServer(networkConfiguration);
            _server.Start();
            
            _server.OnNewClientConnected += ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated += OnServerAuthenticated;
        }
        
        public void StopServer()
        {
            _server.OnNewClientConnected -= ServerHandleNewConnection;
            AuthenticationServiceBase.OnAuthenticated -= OnServerAuthenticated;
        }
        
        private void StartClient()
        {
            _client = new GameClient(networkConfiguration);
            _client.Start();

            _client.LocalClientConnected += OnLocalClientConnected;
            _client.LocalClientAuthenticated += OnClientAuthenticated;
        }

        public void StopClient()
        {
            _client.LocalClientConnected -= OnLocalClientConnected;
            _client.LocalClientAuthenticated -= OnClientAuthenticated;
        }

        public UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password)
        {
            StartClient();
            
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var writer = new ByteWriter(sizeof(ushort) + pwdBytes.Length);
            
            writer.AddUshort((ushort)ENetworkMessageType.ConnectionRequest);
            writer.AddBytes(pwdBytes);

            var tcs = new UniTaskCompletionSource<AuthenticateResult>();
            var cts = new CancellationTokenSource(networkConfiguration.ConnectionTimeOut);
            
            cts.Token.Register(() => { tcs.TrySetResult(new AuthenticateResult(EConnectionResult.TimeOut, null)); });
            
            _clientConnectionEventHandler = (_, result) =>
            {
                tcs.TrySetResult(result);
            };
            
            _server.Send(writer.Data, serverEndPoint, ESendMode.Reliable);
            
            return tcs.Task;
        }

        private void OnLocalClientConnected()
        {
            ClientConnectedToServer?.Invoke();
        }

        private void ServerHandleNewConnection(NetworkClient networkClient, byte[] payload)
        {
            if(useAuthentication)
                AuthenticationServiceBase.Authenticate(networkClient, payload);
        }

        private void OnServerAuthenticated(AuthenticateResult authenticateResult, NetworkClient client)
        {
            //TODO: send message to all clients
            var result = authenticateResult.ConnectionResult;
            var byteWriter = new ByteWriter(10 + authenticateResult.Message.Length);

            byteWriter.AddUshort((ushort)result);
            byteWriter.AddInt(client.Id);
            byteWriter.AddString(authenticateResult.Message);
            
            switch (result)
            {
                case EConnectionResult.Success:
                    client.IsApproved = true;
                    _server.Send(byteWriter.Data, client, ESendMode.Reliable);
                    break;
                case EConnectionResult.Reject:
                case EConnectionResult.TimeOut:
                    _server.DisconnectClient(client.Id, authenticateResult.Message);
                    break;
            }
            
            SeverAuthenticated?.Invoke(client);
        }

        private void OnClientAuthenticated(EConnectionResult authenticateResult, string serverMessage)
        {
            if (authenticateResult == EConnectionResult.Reject)
            {
                StopClient();
            }
            
            _clientConnectionEventHandler?.Invoke(this, new AuthenticateResult(authenticateResult, serverMessage));
        }
    }
}