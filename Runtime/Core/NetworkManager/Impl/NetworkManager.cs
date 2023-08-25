using System;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Helpers;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Impl
{
    public class NetworkManager : MonoBehaviour, 
        INetworkManager
    {
        [SerializeField] private ScriptableNetworkConfiguration _networkConfiguration;
        [SerializeField] private bool _useApproval;

        private UdpTransport _udpTransport;
        
        private NetworkServer.NetworkServer _networkServer;
        private EventHandler<ConnectResult> _connectionEventHandler;
        private readonly Func<byte[], EConnectionResult> _connectionApprovalCallback;

        public void StartClient()
        {
            CreateConnection();
        }
        
        public void StartServer()
        {
            CreateConnection();
        }

        public UniTask<ConnectResult> ConnectToServer(IPEndPoint serverEndPoint, string password)
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
            
            _networkServer.Send(writer.Data, serverEndPoint, ESendMode.Reliable);
            
            return tcs.Task;
        }

        private void CreateConnection()
        {
            _networkServer = new NetworkServer.NetworkServer(_networkConfiguration);
            _networkServer.Start();
            
            _networkServer.OnMessageReceived += HandleIncomeMessage;
        }

        private void OnDestroy()
        {
            _networkServer.OnMessageReceived -= HandleIncomeMessage;
        }

        private void HandleApproval(byte[] payload)
        {
            var headlessPayloadLength = payload.Length - sizeof(ushort);
            
            var headlessPayload = new byte[headlessPayloadLength];

            Buffer.BlockCopy(payload, 2, headlessPayload, 0, headlessPayloadLength);
            
            _connectionEventHandler.Invoke(this, new ConnectResult(EConnectionResult.Success, headlessPayload));

            _connectionEventHandler = null;
        }
        
        private void HandleIncomeMessage(ENetworkMessageType messageType, byte[] payload)
        {
            switch (messageType)
            {
                case ENetworkMessageType.Connect:
                    break;
                case ENetworkMessageType.Disconnect:
                    break;
                case ENetworkMessageType.ClientDisconnected:
                    break;
                case ENetworkMessageType.ClientConnected:
                    break;
                case ENetworkMessageType.Custom:
                    break;
                case ENetworkMessageType.None:
                    break;
                case ENetworkMessageType.Reject:
                    break;
                case ENetworkMessageType.Approve:
                    HandleApproval(payload);
                    break;
                case ENetworkMessageType.ConnectionRequest:
                    break;
            }
        }
    }
}