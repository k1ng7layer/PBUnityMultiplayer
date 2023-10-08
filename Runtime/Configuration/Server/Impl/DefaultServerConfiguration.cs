using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Configuration.Server.Impl
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(DefaultServerConfiguration), fileName = nameof(DefaultServerConfiguration))]
    public class DefaultServerConfiguration : ScriptableObject, 
        IServerConfiguration
    {
        [SerializeField] private ushort port;
        [SerializeField] private ushort maxClients;
        [SerializeField] private double clientAliveTimeOutMilliseconds;
        [SerializeField] private double serverPingTimeRateMilliseconds;

        public ushort Port => port;
        public ushort MaxClients => maxClients;
        public double ClientTimeOutMilliseconds => clientAliveTimeOutMilliseconds;
        public double ServerPingTimeMilliseconds => serverPingTimeRateMilliseconds;
    }
}