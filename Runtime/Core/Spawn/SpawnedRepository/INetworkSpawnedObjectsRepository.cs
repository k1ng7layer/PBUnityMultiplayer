using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository
{
    internal interface INetworkSpawnedObjectsRepository
    {
        bool TryAdd(int id, NetworkObject networkObject);
        bool TryRemove(int id);
        
        bool TryGetObject(int id, out NetworkObject networkObject);
    }
}