namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
        ConnectionRequest,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        ClientReady,
        AuthenticationResult,
        NetworkMessage,
        ClientAliveCheck,
        ServerAliveCheck,
        Ping,
        Sync,
        None
    }
}