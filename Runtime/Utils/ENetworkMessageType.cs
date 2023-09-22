namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
        ConnectionRequest,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        ClientReady,
        Disconnect,
        ClientLostConnection,
        Custom,
        AuthenticationResult,
        NetworkMessage,
        Spawn,
        SpawnHandler,
        ClientAliveCheck,
        ServerAliveCheck,
        Sync,
        None
    }
}