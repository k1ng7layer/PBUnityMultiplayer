using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService
{
    internal interface INetworkSpawnService
    {
        NetworkObject Spawn(int prefabId, Vector3 position, Quaternion rotation);
    }
}