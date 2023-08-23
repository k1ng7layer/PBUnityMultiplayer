using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Helpers;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Impl
{
    public class NetworkManager : MonoBehaviour, 
        INetworkManager
    {
        [SerializeField] private ScriptableNetworkConfiguration _scriptableNetworkConfiguration;
        
        private UdpTransport _udpTransport;
        private bool _isRunning;
        private readonly ConcurrentQueue<PendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<PendingMessage> _receiveMessagesQueue = new();
        private EventHandler<ConnectResult> _connectionEventHandler;
        
        public void StartClient()
        {
            var ipResult = IPAddress.TryParse(_scriptableNetworkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");

            var localEndPoint = new IPEndPoint(ip, _scriptableNetworkConfiguration.LocalPort);

            _udpTransport = new UdpTransport(localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;

            var task = Task.Run(async () => await Receive());
        }
        
        private async Task Receive()
        {
            while (_isRunning)
            {
                var result = await _udpTransport.ReceiveAsync();

                var message = Encoding.UTF8.GetString(result.Payload);
                
                Debug.Log($"received message = {message}");
            }
        }

        public void StartServer()
        {
            
        }

        public UniTask<ConnectResult> ConnectToServer(IPEndPoint serverEndPoint, string password)
        {
            var ipResult = IPAddress.TryParse(_scriptableNetworkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");

            var localEndPoint = new IPEndPoint(ip, _scriptableNetworkConfiguration.LocalPort);

            _udpTransport = new UdpTransport(localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;

            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var writer = new ByteWriter(sizeof(ushort) + pwdBytes.Length);
            
            writer.AddUshort((ushort)ENetworkMessageType.Connect);
            writer.AddBytes(pwdBytes);

            var tcs = new UniTaskCompletionSource<ConnectResult>();
            var cts = new CancellationTokenSource(_scriptableNetworkConfiguration.ConnectionTimeOut);
            cts.Token.Register(() => { tcs.TrySetResult(new ConnectResult(EConnectionResult.TimeOut, null)); });
            
            _connectionEventHandler = (_, result) =>
            {
                tcs.TrySetResult(result);
            };
            
            var message = new PendingMessage(writer.Data, serverEndPoint, ESendMode.Reliable);

            _sendMessagesQueue.Enqueue(message);
            
            return tcs.Task;
        }

        private void ProcessReceiveQueue()
        {
            while (_receiveMessagesQueue.Count > 0)
            {
                var canDequeue = _receiveMessagesQueue.TryDequeue(out var message);

                if (canDequeue)
                {
                    HandleIncomeMessage(message);
                }
            }
        }

        private async UniTask ProcessSendQueue()
        {
            while (_sendMessagesQueue.Count > 0)
            {
                var canDequeue = _sendMessagesQueue.TryDequeue(out var message);
                
                if (canDequeue)
                {
                    await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
                }
            }
        }

        private void HandleIncomeMessage(PendingMessage pendingMessage)
        {
            var messageType = MessageHelper.GetMessageType(pendingMessage.Payload);

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
                    HandleApproval(pendingMessage.Payload);
                    break;
            }
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