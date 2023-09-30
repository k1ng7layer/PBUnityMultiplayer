using System;
using System.Collections.Generic;
using System.Net;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public class NetworkClient
    {
        private readonly Dictionary<int, NetworkObject> _spawnedObjects = new();
        internal IReadOnlyDictionary<int, NetworkObject> SpawnedObjects => _spawnedObjects;

        public NetworkClient(int id, EndPoint remoteEndpoint)
        {
            Id = id;
            RemoteEndpoint = remoteEndpoint;
        }

        public int Id { get; }
        public EndPoint RemoteEndpoint { get; }
        public int EndPointHash { get; set; }
        public bool IsApproved { get; set; }
        public bool IsOnline { get; set; }
        public bool IsReady { get; set; }
        public DateTime LastMessageReceived { get; set; }
        public DateTime LastMessageSent { get; set; }

        internal void AddOwnership(NetworkObject networkObject)
        {
            var hasObject = _spawnedObjects.ContainsKey(networkObject.Id);

            if (hasObject)
                throw new InvalidOperationException(
                    $"[{nameof(NetworkClient)}] network object with id {networkObject.Id} already owned by client with id {Id}");
            
            _spawnedObjects.Add(networkObject.Id, networkObject);
        }
    }
}