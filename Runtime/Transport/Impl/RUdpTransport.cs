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

        protected internal override void StartTransport(IPEndPoint localEndPoint)
        {
            _udpTransport = new UdpTransport(localEndPoint);
            _udpTransport.Start();
        }

        protected internal override void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            _udpTransport.Send(data, remoteEndpoint, sendMode);
        }

        protected internal override async UniTask SendAsync(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            await _udpTransport.SendAsync(data, remoteEndpoint, sendMode);
        }

        protected internal override async UniTask<TransportMessage> ReceiveAsync()
        {
            var data = await _udpTransport.ReceiveAsync();

            return data;
        }

        protected internal override void Stop()
        {
            _udpTransport.Stop();
        }
    }
}