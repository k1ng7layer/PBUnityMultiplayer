using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkObjects
{
    public readonly struct NetworkObjectPositionState
    {
        public readonly Vector3 Position;

        public NetworkObjectPositionState(Vector3 position)
        {
            Position = position;
        }
    }
}