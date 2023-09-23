using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public abstract class TransportBase : MonoBehaviour, IDisposable
    {
        protected internal abstract void StartTransport(IPEndPoint localEndPoint);
        protected internal abstract void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode);
        protected internal abstract UniTask SendAsync(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode);
        protected internal abstract UniTask<TransportMessage> ReceiveAsync();
        protected internal abstract void Stop();

        public void Dispose()
        {
            OnDispose();
        }

        protected virtual void OnDispose()
        { }
    }
}