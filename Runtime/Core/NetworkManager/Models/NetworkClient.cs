using System.Net;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    internal class NetworkClient
    {
        public NetworkClient(int id, IPEndPoint remoteEndpoint)
        {
            Id = id;
            RemoteEndpoint = remoteEndpoint;
        }

        public int Id { get; }
        public IPEndPoint RemoteEndpoint { get; }
    }
}