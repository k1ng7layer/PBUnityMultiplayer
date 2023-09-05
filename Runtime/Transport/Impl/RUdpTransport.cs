using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class RUdpTransport : TransportBase
    {
        private UdpTransport _udpTransport;

        internal override void StartTransport(IPEndPoint localEndPoint)
        {
            _udpTransport = new UdpTransport(localEndPoint);
            _udpTransport.Start();
        }

        internal override void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            _udpTransport.Send(data, remoteEndpoint, sendMode);
        }

        internal override async UniTask SendAsync(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            await _udpTransport.SendAsync(data, remoteEndpoint, sendMode);
        }

        internal override async UniTask<TransportMessage> ReceiveAsync()
        {
            var data = await _udpTransport.ReceiveAsync();

            return data;
        }

        internal override async void Stop()
        {
            _udpTransport.Stop();
        }
    }
}