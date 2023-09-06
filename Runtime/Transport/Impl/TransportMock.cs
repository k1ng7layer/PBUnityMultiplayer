using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public class TransportMock : TransportBase
    {
        private TransportMessage _transportMessage;
        internal override void StartTransport(IPEndPoint localEndPoint)
        {
            
        }

        internal override void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            throw new System.NotImplementedException();
        }

        internal override UniTask SendAsync(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode)
        {
            return UniTask.CompletedTask;
        }

        internal override async UniTask<TransportMessage> ReceiveAsync()
        {
            await UniTask.Delay(1000);
            return await UniTask.FromResult(_transportMessage);
        }

        internal override void Stop()
        {
            throw new System.NotImplementedException();
        }

        public void AddIncomeMessageToReturn(TransportMessage transportMessage)
        {
            _transportMessage = transportMessage;
        }
    }
}