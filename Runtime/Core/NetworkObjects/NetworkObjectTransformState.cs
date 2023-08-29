using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkObjects
{
    public readonly struct NetworkObjectState
    {
        public readonly Vector3 Position;
        public readonly Vector3 Rotation;

        public NetworkObjectState(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}