using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository.Impl
{
    public class NetworkSpawnedObjectsRepository : INetworkSpawnedObjectsRepository
    {
        private readonly Dictionary<int, NetworkObject> _spawnedObjectsTable = new ();
        
        public bool TryAdd(int id, NetworkObject networkObject)
        {
            return _spawnedObjectsTable.TryAdd(id, networkObject);
        }

        public bool TryRemove(int id)
        {
            if (_spawnedObjectsTable.ContainsKey(id))
            {
                _spawnedObjectsTable.Remove(id);
                
                return true;
            }

            return false;
        }

        public bool TryGetObject(int id, out NetworkObject networkObject)
        {
            return _spawnedObjectsTable.TryGetValue(id, out networkObject);
        }
    }
}