namespace PBUnityMultiplayer.Runtime.Utils
{
    public enum ENetworkMessageType
    {
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