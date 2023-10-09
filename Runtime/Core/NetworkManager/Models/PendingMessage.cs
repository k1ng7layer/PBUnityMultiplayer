using System.Net;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct OutcomePendingMessage
    {
        public readonly byte[] Payload;
        public readonly int connectionHash;
        public readonly ESendMode SendMode;

        public OutcomePendingMessage(
            byte[] payload, 
            int connectionHash,
            ESendMode sendMode)
        {
            Payload = payload;
            SendMode = sendMode;
            this.connectionHash = connectionHash;
        }
    }
}