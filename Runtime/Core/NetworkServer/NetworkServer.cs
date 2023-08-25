using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Helpers;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkServer
{
    internal class NetworkServer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly Dictionary<int, NetworkClient> _networkClients = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        private UdpTransport _udpTransport;
        private bool _isRunning;

        public Action<ENetworkMessageType, byte[]> OnMessageReceived;
        public Func< byte[], EConnectionResult> OnConnectRequested;

        public bool useApproval;

        public NetworkServer(INetworkConfiguration networkConfiguration)
        {
            _networkConfiguration = networkConfiguration;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClients;

        public void Start()
        {
            var ipResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");
            
            if(_isRunning)
                throw new Exception($"[{nameof(NetworkManager)}] can't start server when network manager already running ");
            
            var localEndPoint = new IPEndPoint(ip, _networkConfiguration.LocalPort);

            _udpTransport = new UdpTransport(localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;
            
            UniTask.RunOnThreadPool(async () => { await Receive(); }, false);
            UniTask.RunOnThreadPool(async () => { await ProcessSendQueue(); }, false);
        }

        public void Update()
        {
            ProcessReceiveQueue();
        }

        public void Send(
            byte[] data, 
            NetworkClient networkClient, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, networkClient.RemoteEndpoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }
        
        public void Send(
            byte[] data, 
            IPEndPoint remoteEndPoint, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, remoteEndPoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }
        
        private async UniTask Receive()
        {
            while (_isRunning)
            {
                var result = await _udpTransport.ReceiveAsync();

                var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                
                _receiveMessagesQueue.Enqueue(incomeMessage);
            }
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
        
        private void HandleIncomeMessage(IncomePendingMessage incomePendingMessage)
        {
            var messageType = MessageHelper.GetMessageType(incomePendingMessage.Payload);

            if (messageType == ENetworkMessageType.ConnectionRequest && useApproval)
            {
                var result = OnConnectRequested(incomePendingMessage.Payload);
                
                if (result == EConnectionResult.Success)
                {
                    var byteReader = new ByteReader(incomePendingMessage.Payload);
                    var playerId = byteReader.ReadInt32();
                    var playerIpString = byteReader.ReadString();
                    var playerPort = byteReader.ReadInt32();
                    
                    var parseResult = IPAddress.TryParse(playerIpString, out var ipResult);
                    
                    if(!parseResult)
                        return;

                    var remoteEndpoint = new IPEndPoint(ipResult, playerPort);
                    
                    var networkClient = new NetworkClient(playerId, remoteEndpoint);
                    
                    _networkClients.Add(networkClient.Id, networkClient);
                }
                
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)result);
                byteWriter.AddUshort((ushort)result);
                
                Send(byteWriter.Data, incomePendingMessage.RemoteEndPoint, ESendMode.Reliable);
            }
            
            OnMessageReceived?.Invoke(messageType, incomePendingMessage.Payload);
        }
    }
}