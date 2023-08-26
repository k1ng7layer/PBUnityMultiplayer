using System.Collections.Generic;
using System.Net;
using PBUdpTransport.Utils;

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

        public void Send(byte[] data, ESendMode sendMode)
        {
            var message = new OutcomePendingMessage(data, RemoteEndpoint, sendMode);
            
            _outcomePendingMessagesQueue.Enqueue(message);
        }

        internal bool TryRetrieveNextPendingMessage(out OutcomePendingMessage outcomePendingMessage)
        {
            return _outcomePendingMessagesQueue.TryDequeue(out outcomePendingMessage);
        }
    }
}