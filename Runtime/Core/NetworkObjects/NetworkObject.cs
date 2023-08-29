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

        internal void Initialize(ushort id, bool isLocal)
        {
            Id = id;
            isLocalObject = isLocal;
        }

        private void FixedUpdate()
        {
          
        }
    }
}