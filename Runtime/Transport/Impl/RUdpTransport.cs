using System;
using System.Net;
using PBUdpTransport;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class RUdpTransport : MonoBehaviour, INetworkTransport
    {
        private UdpTransport _udpTransport;


        public event Action<EndPoint, ArraySegment<byte>> DataReceived;
        public void Start(IPEndPoint localEndPoint)
        {
            throw new NotImplementedException();
        }

        public void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}