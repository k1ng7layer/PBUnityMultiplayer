using PBUnityMultiplayer.Runtime.Core.NetworkObjects;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers
{
    public interface INetworkSpawnHandlerService
    {
        bool TryGetHandlerId<T>(out string id);
        void RegisterHandler<T>(NetworkSpawnHandler<T> handler) where T : struct;
        void CallHandler(string id, byte[] payload, NetworkObject networkObject);
    }
}