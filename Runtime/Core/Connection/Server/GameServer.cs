using System;
using System.Collections.Generic;
using System.Net;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Server;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Runtime.Utils.IdGenerator;

namespace PBUnityMultiplayer.Runtime.Core.Connection.Server
{
    public class GameServer : IDisposable
    {
        private readonly IServerConfiguration _serverConfiguration;
        private readonly IIdGenerator<ushort> _idGenerator;
        private readonly Dictionary<int, NetworkClient> _clientsTable = new();
        private readonly Dictionary<int, NetworkClient> _pendingClients = new();
        private readonly HashSet<NetworkClient> _clients = new();
        private readonly INetworkTransport _networkTransport;
        private readonly NetworkMessageHandlersService _messageHandlersService = new();
        private bool _running;

        public GameServer(
            INetworkTransport networkTransport, 
            IServerConfiguration serverConfiguration,
            IIdGenerator<ushort> idGenerator
        )
        {
            _networkTransport = networkTransport;
            _serverConfiguration = serverConfiguration;
            _idGenerator = idGenerator;
        }

        public IReadOnlyDictionary<int, NetworkClient> ClientsTable => _clientsTable;
        public IEnumerable<NetworkClient> Clients => _clients;
        public int CurrentTick { get; private set; }
        public event Action<int> ClientDisconnected;
        public event Action<int> ClientReconnected;
        
        public Func<int, ArraySegment<byte>, AuthenticateResult> ConnectionApproveCallback;
        private DateTime _lastPingMessageSendTime;
        internal event Action<int> ClientConnected;

        public void StartServer()
        {
            _running = true;
            _networkTransport.DataReceived += OnDataFromTransportReceived;
            _networkTransport.StartTransport(new IPEndPoint(IPAddress.Any, _serverConfiguration.Port));
        }
        
        public void Stop()
        {
            _running = false;
            _networkTransport.Stop();
        }

        public void SendMessage(int clientId, byte[] data, ESendMode sendMode)
        {
            NetworkClient client = null;
            var hasClient = ClientsTable.TryGetValue(clientId, out var readyClient);
            var hasPendingClient = _pendingClients.TryGetValue(clientId, out var pendingClient);

            if (hasClient)
            {
                client = readyClient;
            }
            else if(hasPendingClient)
            {
                client = pendingClient;
            }
            
            _networkTransport.Send(data, client.EndPointHash, sendMode);
        }
        
        public void SendMessage(byte[] data, ESendMode sendMode)
        {
            foreach (var client in Clients)
            {
                _networkTransport.Send(data, client.EndPointHash, sendMode);
            }
        }
        
        public void SendMessage(HashSet<int> exceptClients, byte[] data, ESendMode sendMode)
        {
            foreach (var client in Clients)
            {
                if(exceptClients.Contains(client.Id))
                    continue;
                
                _networkTransport.Send(data, client.EndPointHash, sendMode);
            }
        }
        
        public void SendMessage<T>(int clientId, T message, ESendMode sendMode) where T : struct
        {
            var handlerId = typeof(T).FullName;
            var hasClient = ClientsTable.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;

            var payload = BinarySerializationHelper.Serialize(message);
            
            var byteWriter = new ByteWriter();
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt32(payload.Length);
            byteWriter.AddBytes(payload);
            
            _networkTransport.Send(byteWriter.Data, client.EndPointHash, sendMode);
        }

        public void Tick()
        {
            if(!_running)
                return;
            
            CurrentTick++;
            
            _networkTransport.Tick();
            
            ProcessTimeOuts();
            SendPing();
        }

        public void Disconnect(int clientId)
        {
            if (_clientsTable.ContainsKey(clientId))
                _clientsTable.Remove(clientId);

            if (_pendingClients.ContainsKey(clientId))
                _pendingClients.Remove(clientId);
        }
        
        public void Disconnect(int clientId, string reason)
        {
            var hasClient = _clientsTable.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;
            
            if (_clientsTable.ContainsKey(clientId))
                _clientsTable.Remove(clientId);

            if (_pendingClients.ContainsKey(clientId))
                _pendingClients.Remove(clientId);
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
            byteWriter.AddInt32(clientId);
            byteWriter.AddString(reason);
            
            SendMessage(byteWriter.Data, ESendMode.Reliable);
        }

        public void RegisterMessageHandler<T>(Action<T> handler) where T: struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }

        private void OnDataFromTransportReceived(EndPoint remoteEndPoint, ArraySegment<byte> data)
        {
            var messageType = MessageHelper.GetMessageType(data);
            
            switch (messageType)
            {
                case ENetworkMessageType.ConnectionRequest:
                    HandleNewConnection(data, remoteEndPoint);
                    break;
                case ENetworkMessageType.ClientDisconnected:
                    HandleClientDisconnected(data);
                    break;
                case ENetworkMessageType.ClientReconnected:
                    HandleClientReconnected(data);
                    break;
                case ENetworkMessageType.NetworkMessage:
                    HandleNetworkMessage(data);
                    break;
                case ENetworkMessageType.ClientReady:
                    HandleClientReady(data);
                    break;
                case ENetworkMessageType.ClientAliveCheck:
                    HandleAliveCheck(data);
                    break;
                case ENetworkMessageType.Ping:
                    HandlePing(data);
                    break;
            }
        }
        
        private void SendPing()
        {
            var diff = (DateTime.Now - _lastPingMessageSendTime).TotalMilliseconds;

            if (diff >= _serverConfiguration.ServerPingTimeMilliseconds)
            {
                var byteWriter = new ByteWriter();
                byteWriter.AddUshort((ushort)ENetworkMessageType.Ping);
            
                SendMessage(byteWriter.Data, ESendMode.Reliable);

                _lastPingMessageSendTime = DateTime.Now;
            }
        }

        private void HandlePing(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();

            var hasClient = _clientsTable.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;

            client.LastMessageReceived = DateTime.Now;
        }

        private void HandleAliveCheck(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            
            var clientId = byteReader.ReadInt32();

            var hasClient = _clientsTable.TryGetValue(clientId, out var player);
            var hasPendingClient = _pendingClients.TryGetValue(clientId, out var pendingClient);

            if (hasClient)
            {
                player.LastMessageReceived = DateTime.Now;
            }

            if (hasPendingClient)
            {
                pendingClient.LastMessageReceived = DateTime.Now;
            }
        }

        private void HandleClientReady(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();
            var hasClient = _pendingClients.TryGetValue(clientId, out var client);
            
            if(!hasClient)
                return;

            client.IsReady = true;

            _pendingClients.Remove(clientId);
            _clientsTable.Add(client.Id, client);
            _clients.Add(client);
            
            var clientEndPoint = (IPEndPoint)client.RemoteEndpoint;
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
            byteWriter.AddInt32(client.Id);
            byteWriter.AddString(clientEndPoint.Address.ToString());
            byteWriter.AddInt32(clientEndPoint.Port);
            
            //Notify all clients except connected, cos it has been already notified 
            var filter = new HashSet<int>();
            filter.Add(clientId);
            
            SendMessage(filter, byteWriter.Data, ESendMode.Reliable);
            
            ClientConnected?.Invoke(clientId);
        }

        private void HandleNetworkMessage(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var networkMessageId = byteReader.ReadString(out _);
            var payloadLength = byteReader.ReadInt32();
            var networkMessagePayload = byteReader.ReadBytes(payloadLength);

            _messageHandlersService.CallHandler(networkMessageId, networkMessagePayload);
        }

        private void HandleClientReconnected(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();

            var hasClient = _clientsTable.TryGetValue(clientId, out var client);

            if (hasClient)
            {
                client.IsOnline = true;
                ClientReconnected?.Invoke(clientId);
            }
        }

        private void HandleClientDisconnected(ArraySegment<byte> data)
        {
            var byteReader = new SegmentByteReader(data, 2);
            var clientId = byteReader.ReadInt32();
            var reason = byteReader.ReadString(out _);
            var hasPlayer = _clientsTable.TryGetValue(clientId, out var client);

            if (hasPlayer)
            {
                _clientsTable.Remove(clientId);
                _clients.Remove(client);
                ClientDisconnected?.Invoke(clientId);
            }

            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
            byteWriter.AddInt32(clientId);
            byteWriter.AddString(reason);
            
            SendMessage(byteWriter.Data, ESendMode.Reliable);
        }

        private void HandleNewConnection(ArraySegment<byte> data, EndPoint remoteEndPoint)
        {
            var clientConnectionId = remoteEndPoint.GetHashCode();
            var byteWriter = new ByteWriter();
            
            if (_clients.Count == _serverConfiguration.MaxClients)
            {
                byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
                byteWriter.AddUshort((ushort)EConnectionResult.Reject);
                byteWriter.AddString("Server is full");
                
                SendMessage(clientConnectionId, byteWriter.Data, ESendMode.Reliable);
                
                return;
            }

            var hasClient = _pendingClients.TryGetValue(clientConnectionId, out var client);
            
            if(hasClient)
                return;

            var id = _idGenerator.Next();
            
            client = new NetworkClient(id, remoteEndPoint);
            client.IsOnline = true;
            _pendingClients.Add(id, client);

            var connectResult = ConnectionApproveCallback(id, data.Slice(2, data.Count - 2));

            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)connectResult.ConnectionResult);
            byteWriter.AddInt32(client.Id);
            byteWriter.AddString(connectResult.Message);
            
            switch (connectResult.ConnectionResult)
            {
                case EConnectionResult.Success:
                    client.LastMessageReceived = DateTime.Now;
                    client.IsApproved = true;
                    client.LastMessageReceived = DateTime.Now;
                    var ipEndpoint = (IPEndPoint)remoteEndPoint;
                    byteWriter.AddString(ipEndpoint.Address.ToString());
                    byteWriter.AddInt32(ipEndpoint.Port);
                    break;
                case EConnectionResult.Reject:
                    _pendingClients.Remove(id);
                    break;
            }
            
            SendMessage(client.EndPointHash, byteWriter.Data, ESendMode.Reliable);
        }
        
        private void ProcessTimeOuts()
        {
            foreach (var client in Clients)
            {
                var diff = (DateTime.Now - client.LastMessageReceived).TotalMilliseconds;
                //Debug.Log($"diff = {diff}");
                if (diff >= _serverConfiguration.ClientTimeOutMilliseconds)
                {
                    Disconnect(client.Id, "TimeOut");
                }
            }
        }
        
        public void Dispose()
        {
            _networkTransport.DataReceived -= OnDataFromTransportReceived;
            Stop();
        }
    }
}