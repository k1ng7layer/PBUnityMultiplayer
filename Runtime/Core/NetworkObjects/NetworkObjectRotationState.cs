using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkObjects
{
    public readonly struct NetworkObjectRotationState
    {
        public readonly Vector3 Rotation;

        public NetworkObjectRotationState(Vector3 rotation)
        {
            Rotation = rotation;
        }
    }
}