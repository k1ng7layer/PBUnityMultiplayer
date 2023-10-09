using System;
using System.Collections.Generic;
using System.Net;
using kcp2k;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    readonly struct DelayedSend
    {
        public readonly byte[] Data;
        public readonly KcpChannel SendMode;

        public DelayedSend(byte[] data, KcpChannel sendMode)
        {
            Data = data;
            SendMode = sendMode;
        }
    }
    
    public class ClientKcpTransport : TransportBase
    {
        private IPEndPoint _serverEndPoint;
        private KcpClient _kcpClient;
        private bool _running;
        private readonly Queue<DelayedSend> _delayedSends = new();
        
        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;
        
        public override void StartTransport(IPEndPoint localEndPoint)
        {
            _serverEndPoint = localEndPoint;
            
            _kcpClient = new KcpClient(OnConnected, OnData, OnDisconnected, OnError, new KcpConfig());
            _running = true;
            
            _kcpClient.Connect(localEndPoint.Address.ToString(), (ushort)localEndPoint.Port);
        }
        

        public override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            var channel = GetChannel(sendMode);

            if (!_kcpClient.connected)
            {
                var delayed = new DelayedSend(data, channel);
                _delayedSends.Enqueue(delayed);
            }
            
            _kcpClient.Send(data, channel);
        }

        public override void Stop()
        {
            _kcpClient.Disconnect();
            _running = false;
        }

        private KcpChannel GetChannel(ESendMode sendMode)
        {
            return sendMode switch
            {
                ESendMode.Reliable => KcpChannel.Reliable,
                ESendMode.Unreliable => KcpChannel.Unreliable,
            };
        }
        
        private void OnData(ArraySegment<byte> data, KcpChannel channel)
        {
            Debug.Log($"OnData");
            DataReceived?.Invoke(_serverEndPoint, data);
        }

        private void OnConnected()
        {
            Debug.Log($"client OnConnected");
            SendDelayedMessages();
        } 
        
        private void OnDisconnected()
        {
            Debug.Log($"client OnDisconnected");
        } 
        
        private void OnError(ErrorCode errorCode, string message)
        {
            Debug.Log($"client OnError {errorCode.ToString()}");
        }

        protected void SendDelayedMessages()
        {
            if (_delayedSends.Count > 0)
            {
                var send = _delayedSends.Dequeue();
                _kcpClient.Send(send.Data, send.SendMode);
            }
        }

        public override void Tick()
        {
            if(!_running)
                return;
            
            _kcpClient.Tick();
        }
    }
}