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
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Server
{
    internal class GameServer
    {
        private readonly INetworkConfiguration _networkConfiguration;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly ConcurrentQueue<OutcomePendingMessage> _sendMessagesQueue = new();
        private readonly ConcurrentQueue<IncomePendingMessage> _receiveMessagesQueue = new();
        private UdpTransport _udpTransport;
        private bool _isRunning;
        private int _nextId;

        public GameServer(INetworkConfiguration networkConfiguration)
        {
            _networkConfiguration = networkConfiguration;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;

        public Action<NetworkClient, byte[]> ClientConnected;
        public event Action<int> ClientDisconnected;
        public event Action<int> ClientReconnected;

        public void Start()
        {
            var ipResult = IPAddress.TryParse(_networkConfiguration.LocalIp, out var ip);

            if (!ipResult)
                throw new Exception($"[{nameof(NetworkManager)}] invalid local ip address, check config");
            
            if(_isRunning)
                throw new Exception($"[{nameof(NetworkManager)}] can't start server when network manager already running ");
            
            var localEndPoint = new IPEndPoint(ip, _networkConfiguration.ServerPort);

            _udpTransport = new UdpTransport(localEndPoint);
            
            _udpTransport.Start();

            _isRunning = true;
            
            // UniTask.Create(async () => { await Receive(); });
            // UniTask.Create(async () => { await ProcessSendQueue(); });
            // Task.Run(async () => await Receive());
            // Task.Run(async () => await ProcessSendQueue());
        }

        public void Update()
        {
            ProcessReceiveQueue();
            ProcessSendQueue();
            Receive();
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

        public void DisconnectClient(int clientId, string reason)
        {
            if (_networkClientsTable.TryGetValue(clientId, out var client))
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddInt((ushort)ENetworkMessageType.Disconnect);
                byteWriter.AddString(reason);

                Send(byteWriter.Data, client.RemoteEndpoint, ESendMode.Reliable);

                _networkClientsTable.Remove(clientId);
            }
        }
        
        public void Stop()
        {
            _isRunning = false;
            _udpTransport.Stop();
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
            while (_isRunning)
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
                            await _udpTransport.SendAsync(message.Payload, message.RemoteEndPoint, message.SendMode);
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
                case ENetworkMessageType.ConnectionRequest:
                    HandleNewConnection(messagePayload);
                    break;
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(messagePayload);
                    break;
                case ENetworkMessageType.ClientReconnected:
                    HandleClientReconnected(messagePayload);
                    break;
                case ENetworkMessageType.Custom:
                    break;
            }
        }

        private void HandleNewConnection(byte[] messagePayload)
        {
            var messageType = MessageHelper.GetMessageType(messagePayload);

            if (messageType == ENetworkMessageType.ConnectionRequest)
            {
                var byteReader = new ByteReader(messagePayload);
                var playerIpString = byteReader.ReadString(out var strSize);
                var playerPort = byteReader.ReadInt32();
                    
                var parseResult = IPAddress.TryParse(playerIpString, out var ipResult);
                    
                if(!parseResult)
                    return;

                var remoteEndpoint = new IPEndPoint(ipResult, playerPort);

                var clientId = _nextId++;
                
                var networkClient = new NetworkClient(_nextId++, remoteEndpoint);

                var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

                if (!hasClient)
                {
                    _networkClientsTable.Add(clientId, networkClient);
                    
                    var byteWriter = new ByteWriter(6);
                    
                    byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
                    byteWriter.AddInt(clientId);
                    byteWriter.AddString(playerIpString);
                    byteWriter.AddInt(playerPort);

                    Send(byteWriter.Data, networkClient, ESendMode.Reliable);
                    
                    //TODO: segregate client's credentials message 

                    var msgLength = messagePayload.Length - (6 + strSize);
                    
                    var connectInfoPayload = new byte[msgLength];
                    
                    Buffer.BlockCopy(messagePayload, 6 + strSize, connectInfoPayload, 0, msgLength);
                    
                    ClientConnected?.Invoke(networkClient, connectInfoPayload);
                }
            }
            
        }
        
        private void HandleClientReconnected(byte[] connectionPayload)
        {
            var byteReader = new ByteReader(connectionPayload);
            var clientId = byteReader.ReadInt32();

            var hasClient = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasClient)
            {
                client.IsOnline = true;
                ClientReconnected?.Invoke(clientId);
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

                ClientDisconnected?.Invoke(clientId);
            }
        }
        
    }
}