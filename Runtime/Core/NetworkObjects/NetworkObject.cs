﻿using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.NetworkObjects
{
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private bool syncPosition;
        [SerializeField] private bool syncRotation;

        public bool IsLocalObject { get; private set; }

        private Vector3 _lastRealPosition;
        private Quaternion _lastRealRotation;
        
        public ushort Id { get; private set; }
        public bool Spawned { get; private set; }
        public int OwnerId { get; private set; }
        
        public void Spawn(ushort id, int ownerId, bool isLocal)
        {
            Id = id;
            IsLocalObject = isLocal;
            Spawned = true;
            OwnerId = ownerId;
        }

        internal void DeSpawn()
        {
            Spawned = false;
        }
    }
}