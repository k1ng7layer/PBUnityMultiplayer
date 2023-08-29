namespace PBUnityMultiplayer.Runtime.Core.Spawn
{
    internal interface INetworkSpawnable
    {
        bool Spawned { get; }
        void Spawn(ushort id, bool isLocal);
        void DeSpawn();
    }
}