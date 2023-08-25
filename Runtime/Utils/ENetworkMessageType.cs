namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
        ConnectionRequest,
        Connect,
        Disconnect,
        ClientDisconnected,
        ClientConnected,
        Reject,
        Approve,
        Custom,
        None
    }
}