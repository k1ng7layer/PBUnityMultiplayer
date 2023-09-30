using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using kcp2k;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Configuration.Connection.Impl;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class ServerKcpTransport : TransportBase
    {
        [SerializeField] private ScriptableNetworkConfiguration networkConfiguration;
        
        private KcpServer _server;

        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;

        protected internal override void StartTransport(IPEndPoint localEndPoint)
        {
            _server = new KcpServer(OnConnected, OnData, OnDisconnected, OnError, new KcpConfig());
            _server.Start((ushort)networkConfiguration.ServerPort);
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
        } 
        
        private void OnDisconnected(EndPoint connectionId)
        {
            var hash = connectionId.GetHashCode();
            
            _connectionIdMap.Remove(hash);
        } 
        
        private void OnError(EndPoint connectionId, ErrorCode errorCode, string message)
        {
            
        } 

        protected internal override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            _server.Send(connectionHash, data, KcpChannel.Reliable);
        }

        protected internal override void Stop()
        {
            _server.Stop();
        }

        private void FixedUpdate()
        {
            _server.Tick();
        }
    }
}