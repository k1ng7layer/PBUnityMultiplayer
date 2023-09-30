using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public abstract class TransportBase : MonoBehaviour, IDisposable
    {
        protected readonly Dictionary<int, IPEndPoint> _connectionIdMap = new();
        public abstract event Action<EndPoint, ArraySegment<byte>> DataReceived; 
        protected internal abstract void StartTransport(IPEndPoint localEndPoint);
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