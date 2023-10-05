using System;
using System.Collections.Generic;
using System.Net;
using PBUdpTransport;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public abstract class TransportBase : MonoBehaviour, IDisposable, INetworkTransport
    {
        protected readonly Dictionary<int, IPEndPoint> _connectionIdMap = new();
        public abstract event Action<EndPoint, ArraySegment<byte>> DataReceived; 
        protected internal abstract void StartTransport(IPEndPoint localEndPoint);
        public void Start(IPEndPoint localEndPoint)
        {
            throw new NotImplementedException();
        }

        void INetworkTransport.Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            Send(data, connectionHash, sendMode);
        }

        void INetworkTransport.Stop()
        {
            Stop();
        }

        protected internal abstract void Send(byte[] data, int connectionHash, ESendMode sendMode);
        protected internal abstract void Stop();

        public void Dispose()
        {
            OnDispose();
        }

        protected virtual void OnDispose()
        { }
    }
}