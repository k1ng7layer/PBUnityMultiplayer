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
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl;
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
        [SerializeField] private bool useAuthentication;

        private EventHandler<AuthenticateResult> _clientConnectionEventHandler;
        
        private GameClient _client;

        public event Action ClientConnectedToServer;
        
        public UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password)
        {
            if (_client == null)
            {
                var spawnHandlerService = new NetworkSpawnHandlerService();
                var spawnService = new NetworkSpawnService(networkPrefabsBase);
                    
                _client = new GameClient(
                    networkConfiguration, 
                    networkPrefabsBase, 
                    spawnHandlerService,
                    spawnService);
            }
            
            _client.Start();

            _client.LocalClientConnected += OnLocalClientConnected;
            _client.LocalClientAuthenticated += OnClientAuthenticated;
            
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
            _client.LocalClientConnected -= OnLocalClientConnected;
            _client.LocalClientAuthenticated -= OnClientAuthenticated;
        }

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            _client.SendMessage(message, sendMode);
        }

        public void Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            _client.Spawn(prefabId, position, rotation);
        }

        public void Spawn<T>(int prefabId, Vector3 position, Quaternion rotation, T message)
        {
            _client.Spawn(prefabId, position, rotation, message);
        }

        private void OnLocalClientConnected()
        {
            ClientConnectedToServer?.Invoke();
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _client.RegisterMessageHandler(handler);
        }
        
        public void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct
        {
            _client.RegisterSpawnHandler(handler);
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
        }
    }
}