using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Client
{
    public class GameClient : Peer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        
        private UdpTransport _udpTransport;
        private IPEndPoint _serverEndPoint;
        private IPEndPoint _localEndPoint;
        private bool _isRunning;
        internal IMessageHandlersService _messageHandlersService;
        
        internal GameClient(INetworkConfiguration networkConfiguration)
        {
            _networkConfiguration = networkConfiguration;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;
        internal IMessageHandlersService MessageHandlersService
        {
            get
            {
                if (_messageHandlersService == null)
                    _messageHandlersService = new NetworkMessageHandlersService();

                return _messageHandlersService;
            }
            set => _messageHandlersService = value;
        }
        public NetworkClient LocalClient { get; private set; }

        internal event Action LocalClientConnected;
        internal event Action LocalClientDisconnected;
        internal event Action LocalClientReconnected;
        internal event Action<int> ClientConnected;
        internal event Action<int> ClientDisconnected;
        internal event Action<int> ClientReconnected;
        internal event Action<EConnectionResult, string> LocalClientAuthenticated;

        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
            var id = typeof(T).FullName.ToString();
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(id);
            byteWriter.AddInt(payload.Length);
            byteWriter.AddBytes(payload);
            
            Send(byteWriter.Data, _serverEndPoint, sendMode);
        }
        
        internal void Start()
        {
            var localIpResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!localIpResult)
                throw new Exception($"[{nameof(GameClient)}] invalid local ip address, check config");
            
            if(_isRunning)
                throw new Exception($"[{nameof(GameClient)}] can't start server when network manager already running ");
            
            _localEndPoint = new IPEndPoint(ip, _networkConfiguration.LocalPort);

            var serverIpResult = IPAddress.TryParse(_networkConfiguration.ServerIp, out var serverIp);
            
            if (!serverIpResult)
                throw new Exception($"[{nameof(GameClient)}] invalid server ip address, check config");

            _serverEndPoint = new IPEndPoint(serverIp, _networkConfiguration.ServerPort);
            
            _udpTransport = new UdpTransport(_localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;
            
            //UniTask.RunOnThreadPool(async () => { await Receive(); }, false);
            //UniTask.RunOnThreadPool(async () => { await ProcessSendQueue(); }, false);

            Task.Run(async () => await Receive());
            Task.Run(async () => await ProcessSendQueue());
        }
        
        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            MessageHandlersService.RegisterHandler(handler);
        }

        internal void Update()
        {
            ProcessReceiveQueue();
            //ProcessSendQueue();
            //Receive();
        }

        internal void Send(
            byte[] data, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, _serverEndPoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }
        
        internal void Send(
            byte[] data, 
            IPEndPoint remoteEndPoint, 
            ESendMode sendMode
        )
        {
            var outcomeMessage = new OutcomePendingMessage(data, remoteEndPoint, sendMode);
            
            _sendMessagesQueue.Enqueue(outcomeMessage);
        }

        internal void Stop()
        {
            _isRunning = false;
            _udpTransport.Stop();
        }
        
        private async UniTask Receive()
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpTransport.ReceiveAsync();

                    var incomeMessage = new IncomePendingMessage(result.Payload, result.RemoteEndpoint);
                
                    _receiveMessagesQueue.Enqueue(incomeMessage);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
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
            while (_isRunning)
            {
                while (_sendMessagesQueue.Count > 0)
                {
                    try
                    {
                        var canDequeue = _sendMessagesQueue.TryDequeue(out var message);
                
                        if (canDequeue)
                        {
                            Debug.Log("BeginSent");
                            await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
                            Debug.Log("Sent");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }
        
        private void HandleIncomeMessage(IncomePendingMessage incomePendingMessage)
        {
            var messageType = MessageHelper.GetMessageType(incomePendingMessage.Payload);
            var messagePayload = incomePendingMessage.Payload;
            switch (messageType)
            {
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(messagePayload);
                    break;
                case ENetworkMessageType.ClientConnected:
                    HandleNewConnection(messagePayload);
                    break;
                case ENetworkMessageType.ClientReconnected:
                    HandleClientReconnected(messagePayload);
                    break;
                case ENetworkMessageType.Custom:
                    break;
                case ENetworkMessageType.AuthenticationResult:
                    HandleConnectionAuthentication(messagePayload);
                    break;
                case ENetworkMessageType.NetworkMessage:
                    HandleNetworkMessage(messagePayload);
                    break;
            }
        }

        private void HandleNetworkMessage(byte[] payload)
        {
            var byteReader = new ByteReader(payload, 2);
            var networkMessageId = byteReader.ReadString();
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            MessageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }

        private void HandleConnectionAuthentication(byte[] connPayload)
        {
            var byteReader = new ByteReader(connPayload, 2);
            var result = (EConnectionResult)byteReader.ReadUshort();
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString();
            LocalClient = new NetworkClient(clientId, _localEndPoint);
            LocalClientAuthenticated?.Invoke(result, reason);
        }

        private void HandleNewConnection(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload, 2);
            var clientId = byteReader.ReadInt32();
            
            var playerIpString = byteReader.ReadString(out _);
            var playerPort = byteReader.ReadInt32();
                    
            var parseResult = IPAddress.TryParse(playerIpString, out var ipResult);
                    
            if(!parseResult)
                return;

            var remoteEndpoint = new IPEndPoint(ipResult, playerPort);
                    
            var networkClient = new NetworkClient(clientId, remoteEndpoint);
            
            _networkClientsTable.Add(clientId, networkClient);

            if (Equals(remoteEndpoint, _localEndPoint))
            {
                //LocalClient = networkClient;
                LocalClientConnected?.Invoke();
            }
            else
            {
                ClientConnected?.Invoke(clientId);
            }
        }

        private void HandleClientReconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasClient)
            {
                if (client.Id == LocalClient.Id)
                {
                    LocalClientReconnected?.Invoke();
                }
                else
                {
                    ClientReconnected?.Invoke(clientId);
                }
            }
        }

        private void HandleClientDisconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasPlayer = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                client.IsOnline = false;

                if (clientId == LocalClient.Id)
                {
                    LocalClientDisconnected?.Invoke();
                }
                else
                {
                    ClientDisconnected?.Invoke(clientId);
                }
            }
        }
    }
}