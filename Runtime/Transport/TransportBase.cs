using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public abstract class TransportBase : MonoBehaviour
    {
        internal abstract void StartTransport(IPEndPoint localEndPoint);
        internal abstract void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode);
        internal abstract UniTask SendAsync(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode);
        internal abstract UniTask<TransportMessage> ReceiveAsync();
        internal abstract void Stop();
    }
}