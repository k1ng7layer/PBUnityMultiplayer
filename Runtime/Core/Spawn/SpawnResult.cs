using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn
{
    internal readonly struct SpawnResult
    {
        public readonly int PrefabId;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly string SpawnHandlerId;

        public SpawnResult(
            int prefabId,
            Vector3 position, 
            Quaternion rotation,
            string spawnHandlerId)
        {
            PrefabId = prefabId;
            Position = position;
            Rotation = rotation;
            SpawnHandlerId = spawnHandlerId;
        }
    }
}