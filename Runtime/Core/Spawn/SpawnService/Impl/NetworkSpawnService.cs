using PBUnityMultiplayer.Runtime.Configuration.Prefabs;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl
{
    internal class NetworkSpawnService : INetworkSpawnService
    {
        private readonly INetworkPrefabsBase _networkPrefabsBase;

        public NetworkSpawnService(
            INetworkPrefabsBase networkPrefabsBase)
        {
            _networkPrefabsBase = networkPrefabsBase;
        }
        
        public NetworkObject Spawn(int prefabId, Vector3 position, Quaternion rotation)
        {
            var prefab = _networkPrefabsBase.Get(prefabId);

            var networkObj = Object.Instantiate(prefab, position, rotation);

            return networkObj;
        }
    }
}