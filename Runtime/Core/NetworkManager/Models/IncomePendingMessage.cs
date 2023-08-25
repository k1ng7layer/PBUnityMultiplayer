using System.Net;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public struct IncomePendingMessage
    {
        public readonly byte[] Payload;
        public readonly IPEndPoint RemoteEndPoint;

        public IncomePendingMessage(
            byte[] payload, 
            IPEndPoint remoteEndPoint)
        {
            Payload = payload;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}