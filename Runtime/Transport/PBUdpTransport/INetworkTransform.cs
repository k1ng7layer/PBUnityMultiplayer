using System;
using System.Net;
using PBUdpTransport.Utils;

namespace PBUdpTransport
{
    public interface INetworkTransport
    {
        event Action<EndPoint, ArraySegment<byte>> DataReceived; 
        void Start(IPEndPoint localEndPoint);
        void Send(byte[] data, int connectionHash, ESendMode sendMode);
        void Tick();
        void Stop();
    }
}