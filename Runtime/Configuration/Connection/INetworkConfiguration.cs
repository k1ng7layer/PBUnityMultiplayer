namespace PBUnityMultiplayer.Runtime.Configuration.Connection
{
    public interface INetworkConfiguration
    {
        string ServerIp { get;}
        int ServerPort { get;}
        string LocalIp { get;}
        int LocalPort { get;}
        int ConnectionTimeOut { get;}
        int ClientCheckAliveTimeOut { get;}
        int ServerClientDisconnectTime { get; }
        int ClientCheckAliveTime { get; }
        int ServerCheckAliveTimeOut { get; }
        int ServerCheckAliveTimeSent { get; }
        int MaxClients { get; }
    }
}