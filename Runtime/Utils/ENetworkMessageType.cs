namespace PBUnityMultiplayer.Runtime.Utils
{
    internal enum ENetworkMessageType
    {
        ConnectionRequest,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        Disconnect,
        Custom,
        AuthenticationResult,
        NetworkMessageHandler,
        None
    }
}