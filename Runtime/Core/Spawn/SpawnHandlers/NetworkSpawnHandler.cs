using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers
{
    public delegate void PackedNetworkSpawnHandler(NetworkMessageDeserializer deserializer, byte[] payload,
        NetworkObject networkObject);

    public delegate void NetworkSpawnHandler<T>(NetworkObject networkObject, T spawnData) where T : struct;
}