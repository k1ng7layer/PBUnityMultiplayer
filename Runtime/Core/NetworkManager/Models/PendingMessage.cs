using System.Net;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct PendingMessage
    {
        public readonly byte[] Payload;
        public readonly IPEndPoint RemoteEndPoint;
        public readonly ESendMode SendMode;

        public PendingMessage(
            byte[] payload, 
            IPEndPoint remoteEndPoint, 
            ESendMode sendMode)
        {
            Payload = payload;
            RemoteEndPoint = remoteEndPoint;
            SendMode = sendMode;
        }
    }
}