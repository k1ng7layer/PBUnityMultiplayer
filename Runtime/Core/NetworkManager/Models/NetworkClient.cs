using System.Collections.Generic;
using System.Net;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public class NetworkClient
    {
        private readonly Queue<OutcomePendingMessage> _outcomePendingMessagesQueue = new();
        
        public NetworkClient(int id, IPEndPoint remoteEndpoint)
        {
            Id = id;
            RemoteEndpoint = remoteEndpoint;
        }

        public int Id { get; }
        public IPEndPoint RemoteEndpoint { get; }
        public bool IsApproved { get; set; }
        public bool IsOnline { get; set; }
    }
}