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
        ClientLostConnection,
        Custom,
        AuthenticationResult,
        NetworkMessage,
        Spawn,
        SpawnHandler,
        ClientAliveCheck,
        ServerAliveCheck,
        None
    }
}