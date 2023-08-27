﻿using System.Net;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct OutcomePendingMessage
    {
        public readonly byte[] Payload;
        public readonly IPEndPoint RemoteEndPoint;
        public readonly ESendMode SendMode;

        public OutcomePendingMessage(
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