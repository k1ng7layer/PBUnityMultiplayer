namespace PBUnityMultiplayer.Runtime.Configuration
{
    public interface INetworkConfiguration
    {
        string ServerIp { get;}
        int ServerPort { get;}
        string LocalIp { get;}
        int LocalPort { get;}
        int ConnectionTimeOut { get;}
    }
}