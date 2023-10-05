using System;
using System.Collections.Generic;
using System.Net;
using kcp2k;
using PBUdpTransport;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{                   
    public class ServerKcpTransport : MonoBehaviour, 
        INetworkTransport
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        private readonly Dictionary<int, IPEndPoint> _connectionIdMap = new();
        
        private KcpServer _server;
        private bool _running;

        public event Action<EndPoint, ArraySegment<byte>> DataReceived;

        public void Start(IPEndPoint localEndPoint)
        {
            _server = new KcpServer(OnConnected, OnData, OnDisconnected, OnError, new KcpConfig());
            _server.Start((ushort)networkConfiguration.ServerPort);
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

        public void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            _server.Send(connectionHash, data, KcpChannel.Reliable);
        }

        public void Tick()
        {
            if(!_running)return;
            
            _server.Tick();
        }

        public void Stop()
        {
            _running = false;
            _server.Stop();
        }
    }
}