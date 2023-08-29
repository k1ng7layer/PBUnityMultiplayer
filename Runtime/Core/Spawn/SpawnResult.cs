using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Spawn
{
    internal readonly struct SpawnResult
    {
        public readonly int PrefabId;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly int? ParentObjectId;
        public readonly string SpawnHandlerId;

        public SpawnResult(
            int prefabId,
            Vector3 position, 
            Quaternion rotation, 
            int? parentObjectId, 
            string spawnHandlerId)
        {
            PrefabId = prefabId;
            Position = position;
            Rotation = rotation;
            ParentObjectId = parentObjectId;
            SpawnHandlerId = spawnHandlerId;
        }
    }
}