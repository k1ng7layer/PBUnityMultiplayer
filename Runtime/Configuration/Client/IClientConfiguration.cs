namespace PBUnityMultiplayer.Runtime.Configuration.Client
{
    public interface IClientConfiguration
    {
        string ServerIp { get; }
        ushort ServerPort { get; }
        int ClientTickRateDivergence { get; }
        double ClientPingSendRateMilliseconds { get; }
    }
}