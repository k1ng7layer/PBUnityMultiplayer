using System;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using PBUnityMultiplayer.Runtime.Utils.Attributes;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Configuration.Prefabs.Impl
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(NetworkPrefabsBase), fileName = nameof(NetworkPrefabsBase))]
    public class NetworkPrefabsBase : ScriptableObject, 
        INetworkPrefabsBase
    {
        [KeyValue(nameof(Prefab.id))] [SerializeField]
        private Prefab[] prefabs;

        public NetworkObject Get(int id)
        {
            for (var i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];
                if (prefab.id == id)
                    return prefab.NetworkObject;
            }

            throw new Exception($"[PrefabsBase] Can't find prefab with name: {id}");
        }
    }
    
    [Serializable]
    public class Prefab
    {
        public int id;
        public NetworkObject NetworkObject;
    }
}