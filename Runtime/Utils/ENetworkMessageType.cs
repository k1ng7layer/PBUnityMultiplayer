namespace PBUnityMultiplayer.Runtime.Utils
{
    internal enum ENetworkMessageType
    {
        ConnectionRequest,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        ClientReady,
        Disconnect,
        Custom,
        AuthenticationResult,
        NetworkMessage,
        Spawn,
        SpawnHandler,
        None
    }
}