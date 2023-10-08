using System;
using System.Net;
using PBUdpTransport;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public abstract class TransportBase : MonoBehaviour, IDisposable, INetworkTransport
    {
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public abstract event Action<EndPoint, ArraySegment<byte>> DataReceived;

        public abstract void StartTransport(IPEndPoint localEndPoint);

        public abstract void Send(byte[] data, int connectionHash, ESendMode sendMode);

        public abstract void Tick();

        public abstract void Stop();
    }
}