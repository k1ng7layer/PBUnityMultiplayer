using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class RUdpTransport : TransportBase
    {
        private UdpTransport _udpTransport;

        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;

        protected internal override void StartTransport(IPEndPoint localEndPoint)
        {
            _udpTransport = new UdpTransport(localEndPoint);
            _udpTransport.Start();
        }

        protected internal override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            
        }

        protected internal override void Stop()
        {
            _udpTransport.Stop();
        }

        protected override void OnDispose()
        {
            _udpTransport?.Dispose();
        }
    }
}