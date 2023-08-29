using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService
{
    internal interface INetworkSpawnService
    {
        event Action<SpawnResult> Spawned;
        
        void RegisterSpawnHandler<T>(Action<T> handler) where T : struct;
        SpawnResult Spawn<T>(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation, int parentObjectId) where T : struct;
        SpawnResult Spawn<T>(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation) where T : struct;
        SpawnResult Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation, int parentObjectId);
        SpawnResult Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation);
    }
}