namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
        ConnectionRequest,
        Connect,
        Disconnect,
        ClientDisconnected,
        ClientConnected,
        ClientReconnected,
        Reject,
        Approve,
        Custom,
        None
    }
}