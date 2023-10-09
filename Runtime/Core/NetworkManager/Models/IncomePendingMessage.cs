using System;
using System.Net;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public struct IncomePendingMessage
    {
        public readonly  ArraySegment<byte> Payload;
        public readonly EndPoint RemoteEndPoint;

        public IncomePendingMessage(
            ArraySegment<byte> payload, 
            EndPoint remoteEndPoint)
        {
            Payload = payload;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}