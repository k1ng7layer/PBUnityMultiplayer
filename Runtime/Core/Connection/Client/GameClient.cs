using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Client;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Client
{
    public class GameClient : Peer, IDisposable
    {
        private readonly IClientConfiguration _clientConfiguration;
        private readonly INetworkTransport _networkTransport;
        private readonly Dictionary<int, NetworkClient> _networkClientsTable = new();
        private readonly HashSet<NetworkClient> _clients = new();
        private readonly NetworkMessageHandlersService _messageHandlersService = new();
        private int _serverEndPointHash;
        private bool _isRunning;
        
        internal GameClient(
            IClientConfiguration clientConfiguration, 
            INetworkTransport networkTransport
        )
        {
            _clientConfiguration = clientConfiguration;
            _networkTransport = networkTransport;
        }
        
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers => _networkClientsTable;
        public IEnumerable<NetworkClient> Clients => _clients;
      
        public NetworkClient LocalClient { get; private set; }
        public int CurrentTick { get; private set; }
        internal event Action<int> ClientConnected;
        internal event Action<int, string> ClientDisconnected;
        internal event Action<EConnectionResult, string> LocalClientAuthenticated;

        private DateTime _lastMessageReceivedFromServer;
        
        internal void Start()
        {
            if(_isRunning)
              Stop();
            
            _isRunning = true;
            
            var serverIpResult = IPAddress.TryParse(_clientConfiguration.ServerIp, out var serverIp);
            
            if (!serverIpResult)
                throw new Exception($"[{nameof(GameClient)}] invalid server ip address, check config");

            var serverEndPoint = new IPEndPoint(serverIp, _clientConfiguration.ServerPort);
            _serverEndPointHash = serverEndPoint.GetHashCode();
            _networkTransport.StartTransport(serverEndPoint);
            
            _networkTransport.DataReceived += OnDataReceived;
        }
        
        public void ConnectToServer(string password)
        {
            if (!_isRunning)
                throw new Exception($"[{nameof(GameClient)}] must call StartClient first");
            
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var writer = new ByteWriter(sizeof(ushort) + pwdBytes.Length);
            
            writer.AddUshort((ushort)ENetworkMessageType.ConnectionRequest);
            writer.AddString(password);
            
            Send(writer.Data, ESendMode.Reliable);
        }
        
        public void SendMessage<T>(T message, ESendMode sendMode)
        {
            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();

            var id = typeof(T).FullName;
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddInt32(LocalClient.Id);
            byteWriter.AddString(id);
            byteWriter.AddInt32(payload.Length);
            byteWriter.AddBytes(payload);
            
            LocalClient.LastMessageSent = DateTime.Now;
            
            _networkTransport.Send(byteWriter.Data, _serverEndPointHash, sendMode);
        }
        
        public void Stop()
        {
            if (_isRunning && LocalClient != null && LocalClient.IsOnline)
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
                byteWriter.AddInt32(LocalClient.Id);
                byteWriter.AddString("Manual quit");
            
                Send(byteWriter.Data, ESendMode.Reliable);
            }
            
            _isRunning = false;
            _networkClientsTable.Clear();
            _clients.Clear();
            
            LocalClient = null;
        }

        public void Tick()
        {
            if(!_isRunning)
                return;
            
            CurrentTick++;
            _networkTransport.Tick();
        }

        public void Send(
            byte[] data, 
            ESendMode sendMode
        )
        {
            _networkTransport.Send(data, _serverEndPointHash, sendMode);
        }

        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }
        
        public void UnregisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }
        
        private void OnDataReceived(EndPoint endPoint, ArraySegment<byte> data)
        {
            HandleIncomeMessage(data);
        }
        
        private void HandleIncomeMessage(ArraySegment<byte> data)
        {
            var messageType = MessageHelper.GetMessageType(data);
            
            switch (messageType)
            {
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(data);
                    break;
                case ENetworkMessageType.ClientConnected:
                    HandleNewConnection(data);
                    break;
                case ENetworkMessageType.AuthenticationResult:
                    HandleConnectionAuthentication(data);
                    break;
                case ENetworkMessageType.NetworkMessage:
                    HandleNetworkMessage(data);
                    break;
                case ENetworkMessageType.ServerAliveCheck:
                    HandleServerAliveCheck(data);
                    break;
                case ENetworkMessageType.Sync:
                    HandleSync(data);
                    break;
                case ENetworkMessageType.Ping:
                    HandlePing(data);
                    break;
            }
        }

        private void HandlePing(ArraySegment<byte> data)
        {
            _lastMessageReceivedFromServer = DateTime.Now;
        }

        private void HandleSync(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var serverTick = byteReader.ReadInt32();
            if(Mathf.Abs(CurrentTick - serverTick) >= _clientConfiguration.ClientTickRateDivergence)
                CurrentTick = serverTick;
        }
        
        private void HandleServerAliveCheck(ArraySegment<byte> data)
        {
            _lastMessageReceivedFromServer = DateTime.Now;
        }

        private void HandleNetworkMessage(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var networkMessageId = byteReader.ReadString(out _);
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }

        private void HandleConnectionAuthentication(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var result = (EConnectionResult)byteReader.ReadUshort();
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString(out _);

            if (result == EConnectionResult.Success)
            {
                var ipString = byteReader.ReadString(out _);
                var port = byteReader.ReadInt32();

                var ip = IPAddress.Parse(ipString);
                var ipEndpoint = new IPEndPoint(ip, port);
                
                LocalClient = new NetworkClient(clientId, ipEndpoint);
                LocalClient.IsOnline = true;
                var byteWriter = new ByteWriter();
                
                byteWriter.AddUshort((ushort)ENetworkMessageType.ClientReady);
                byteWriter.AddInt32(clientId);
                
                Send(byteWriter.Data, ESendMode.Reliable);
                
                _networkClientsTable.Add(clientId, LocalClient);
                _clients.Add(LocalClient);
            }
           
            LocalClientAuthenticated?.Invoke(result, reason);
        }

        private void HandleNewConnection(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();
            
            var playerIpString = byteReader.ReadString(out var strLength);
            var playerPort = byteReader.ReadInt32();
                    
            var parseResult = IPAddress.TryParse(playerIpString, out var ipResult);
                    
            if(!parseResult)
                return;

            var remoteEndpoint = new IPEndPoint(ipResult, playerPort);
                    
            var networkClient = new NetworkClient(clientId, remoteEndpoint);
            
            _networkClientsTable.Add(clientId, networkClient);
            _clients.Add(networkClient);
            
            ClientConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString(out _);

            var hasPlayer = _networkClientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                ClientDisconnected?.Invoke(clientId, reason);
                _networkClientsTable.Remove(clientId);
                _clients.Remove(client);
            }
        }

        public void Dispose()
        {
            _networkTransport.DataReceived -= OnDataReceived;
            _networkTransport.Stop();
        }
    }
}