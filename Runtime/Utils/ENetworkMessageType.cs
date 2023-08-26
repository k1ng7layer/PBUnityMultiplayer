namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
        ConnectionRequest,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        Disconnect,
        Custom,
        Approve,
        None
    }
}