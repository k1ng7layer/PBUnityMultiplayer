using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Configuration.Prefabs
{
    public interface INetworkPrefabsBase
    {
        NetworkObject Get(int id);
    }
}