using System;
using System.Collections.Generic;
using System.Net;
using kcp2k;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{                   
    public class ServerKcpTransport : TransportBase
    {
        private readonly Dictionary<int, IPEndPoint> _connectionIdMap = new();
        
        private KcpServer _server;
        private bool _running;

        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;

        public override void StartTransport(IPEndPoint localEndPoint)
        {
            _server = new KcpServer(OnConnected, OnData, OnDisconnected, OnError, new KcpConfig());
            _server.Start((ushort)localEndPoint.Port);
            _running = true;
        }
        
        private void OnData(int connectionHash, ArraySegment<byte> data, KcpChannel channel)
        {
            var hasEndpoint = _connectionIdMap.TryGetValue(connectionHash, out var endPoint);
            
            if(!hasEndpoint)
                return;
            
            DataReceived?.Invoke(endPoint, data);
        }

        private void OnConnected(EndPoint connectionId)
        {
            var hash = connectionId.GetHashCode();
            
            _connectionIdMap.Add(hash, (IPEndPoint)connectionId);
            
            Debug.Log($"OnConnected");
        } 
        
        private void OnDisconnected(EndPoint connectionId)
        {
            var hash = connectionId.GetHashCode();
            
            _connectionIdMap.Remove(hash);
            
            Debug.Log($"OnDisconnected");
        } 
        
        private void OnError(EndPoint connectionId, ErrorCode errorCode, string message)
        {
            
        }

        public override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            _server.Send(connectionHash, data, KcpChannel.Reliable);
        }

        public override void Tick()
        {
            if(!_running)return;
            
            _server.Tick();
        }

        public override void Stop()
        {
            _running = false;
            _server?.Stop();
        }
    }
}