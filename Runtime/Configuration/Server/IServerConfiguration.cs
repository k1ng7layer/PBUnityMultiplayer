namespace PBUnityMultiplayer.Runtime.Configuration.Server
{
    public interface IServerConfiguration
    {
        ushort Port { get; }
        ushort MaxClients { get;}
        double ClientAliveTimeOutMilliseconds { get;}
        double ServerPingTimeRateMilliseconds { get;}
    }
}