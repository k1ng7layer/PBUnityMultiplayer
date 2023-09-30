using System;
using System.Net;
using kcp2k;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class ClientKcpTransport : TransportBase
    {
        private IPEndPoint _serverEndPoint;
        private KcpClient _kcpClient;
        
        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;
        
        protected internal override void StartTransport(IPEndPoint localEndPoint)
        {
            _serverEndPoint = localEndPoint;
            
            _kcpClient = new KcpClient(OnConnected, OnData, OnDisconnected, OnError, new KcpConfig());
            
            _kcpClient.Connect(localEndPoint.Address.ToString(), (ushort)localEndPoint.Port);
        }
        

        protected internal override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            var channel = GetChannel(sendMode);
            
            _kcpClient.Send(data, channel);
        }

        protected internal override void Stop()
        {
            _kcpClient.Disconnect();
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
            DataReceived?.Invoke(_serverEndPoint, data);
        }

        private void OnConnected()
        {
        
        } 
        
        private void OnDisconnected()
        {
            
        } 
        
        private void OnError(ErrorCode errorCode, string message)
        {
            
        } 

        private void FixedUpdate()
        {
            _kcpClient.Tick();
        }
    }
}