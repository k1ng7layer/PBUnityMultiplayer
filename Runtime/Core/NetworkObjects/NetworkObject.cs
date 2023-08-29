using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkObjects
{
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private bool syncPosition;
        [SerializeField] private bool syncRotation;
        
        private bool isLocalObject;

        private Vector3 _lastRealPosition;
        private Quaternion _lastRealRotation;
        
        public ushort Id { get; private set; }
        public bool Spawned { get; private set; }
        
        internal void Spawn(ushort id, bool isLocal)
        {
            Id = id;
            isLocalObject = isLocal;
            Spawned = true;
        }

        internal void DeSpawn()
        {
            Spawned = false;
        }
    }
}