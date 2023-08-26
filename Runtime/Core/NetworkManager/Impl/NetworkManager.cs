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
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Server;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Impl
{
    public class NetworkManager : MonoBehaviour, 
        INetworkManager
    {
        [SerializeField] private ScriptableNetworkConfiguration _networkConfiguration;

        private UdpTransport _udpTransport;
        private NetworkServer _server;
        private EventHandler<ConnectResult> _connectionEventHandler;
        private readonly Func<byte[], EConnectionResult> _connectionApprovalCallback;

        public IAuthenticationService AuthenticationService { get; set; }
        
        public void StartClient()
        {
            _server = new NetworkServer(_networkConfiguration);
            _server.Start();
        }

        public event Action OnClientConnectedToServer;
        public event Action OnClientAuthenticated;
        public event Action<NetworkClient> OnSeverAuthenticated;

        public void StartServer()
        {
            _server = new NetworkServer(_networkConfiguration);
            _server.Start();
            
            _server.OnNewClientConnected += HandleNewConnection;
            AuthenticationService.OnAuthenticated += OnServerAuthenticated;
        }
        
        public void StopServer()
        {
            _server.OnNewClientConnected -= HandleNewConnection;
            AuthenticationService.OnAuthenticated -= OnServerAuthenticated;
        }

        public UniTask<ConnectResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password)
        {
            var ipResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");

            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var writer = new ByteWriter(sizeof(ushort) + pwdBytes.Length);
            
            writer.AddUshort((ushort)ENetworkMessageType.Connect);
            writer.AddBytes(pwdBytes);

            var tcs = new UniTaskCompletionSource<ConnectResult>();
            var cts = new CancellationTokenSource(_networkConfiguration.ConnectionTimeOut);
            
            cts.Token.Register(() => { tcs.TrySetResult(new ConnectResult(EConnectionResult.TimeOut, null)); });
            
            _connectionEventHandler = (_, result) =>
            {
                tcs.TrySetResult(result);
            };
            
            _server.Send(writer.Data, serverEndPoint, ESendMode.Reliable);
            
            return tcs.Task;
        }

        private AuthenticateResult HandleNewConnection(byte[] connPayload, NetworkClient networkClient)
        {
            return AuthenticationService.Authenticate(networkClient, connPayload);
        }

        private void OnServerAuthenticated(NetworkClient client)
        {
            OnSeverAuthenticated?.Invoke(client);
        }

        private void HandleApproval(byte[] payload)
        {
            var headlessPayloadLength = payload.Length - sizeof(ushort);
            
            var headlessPayload = new byte[headlessPayloadLength];

            Buffer.BlockCopy(payload, 2, headlessPayload, 0, headlessPayloadLength);
            
            _connectionEventHandler.Invoke(this, new ConnectResult(EConnectionResult.Success, headlessPayload));

            _connectionEventHandler = null;
        }
    }
}