using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers.Impl
{
    public class NetworkSpawnHandlerService : INetworkSpawnHandlerService
    {
        private readonly Dictionary<string, List<PackedNetworkSpawnHandler>> _registeredHandlersTable = new();

        public bool TryGetHandlerId<T>(out string id)
        {
            id = typeof(T).FullName.ToString();

            var hasId = _registeredHandlersTable.TryGetValue(id, out var handlerId);

            return hasId;
        }

        public void RegisterHandler<T>(NetworkSpawnHandler<T> handler) where T : struct
        {
            var id = typeof(T).FullName.ToString();

            if (!_registeredHandlersTable.ContainsKey(id))
            {
                _registeredHandlersTable.Add(id, new List<PackedNetworkSpawnHandler>());
            }
            
            var networkHandler = CreateHandler(handler);
            _registeredHandlersTable[id].Add(networkHandler);
        }

        public void CallHandler(string id, byte[] payload, NetworkObject networkObject)
        {
            var hasHandler = _registeredHandlersTable.TryGetValue(id, out var handlers);
            
            if(!hasHandler)
                return;
            
            foreach (var handler in handlers)
            {
                handler?.Invoke(new NetworkMessageDeserializer(), payload, networkObject);
            }
        }

        private PackedNetworkSpawnHandler CreateHandler<T>(NetworkSpawnHandler<T> handler) where T : struct
            => (deserializer, payload, networkObject) =>
            {
                var data = deserializer.Deserialize<T>(payload);

                handler?.Invoke(networkObject, data);
            };
    }
}