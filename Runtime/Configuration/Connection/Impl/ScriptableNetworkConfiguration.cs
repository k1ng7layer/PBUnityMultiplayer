using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Configuration.Connection.Impl
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(ScriptableNetworkConfiguration), fileName = nameof(ScriptableNetworkConfiguration))]
    public class ScriptableNetworkConfiguration : ScriptableObject, 
        INetworkConfiguration
    {
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 7779;
        [SerializeField] private string localIp = "127.0.0.1";
        [SerializeField] private int localPort = 7777;
        [SerializeField] private int connectionTimeOut;
        [SerializeField] private int clientCheckAliveTimeOut = 4000;
        [SerializeField] private int serverClientDisconnectTime = 7000;
        [SerializeField] private int clientCheckAliveTime = 3000;
        [SerializeField] private int serverCheckAliveTime = 4000;
        [SerializeField] private int serverCheckAliveTimeSent = 2000;
        [SerializeField] private int maxClients;
        [SerializeField] private int serverTickRate = 1;
        [SerializeField] private int clientTickRateDivergence = 5;

        public string ServerIp => serverIp;

        public int ServerSyncTickRate => serverTickRate;

        public int ServerPort => serverPort;

        public string LocalIp => localIp;

        public int LocalPort => localPort;

        public int ConnectionTimeOut => connectionTimeOut;

        public int ClientCheckAliveTimeOut => clientCheckAliveTimeOut;

        public int ServerClientDisconnectTime => serverClientDisconnectTime;

        public int ClientCheckAliveTime => clientCheckAliveTime;

        public int ServerCheckAliveTimeOut => serverCheckAliveTime;

        public int ServerCheckAliveTimeSent => serverCheckAliveTimeSent = 2000;

        public int MaxClients => maxClients;

        public int ClientTickRateDivergence => clientTickRateDivergence;
    }
}