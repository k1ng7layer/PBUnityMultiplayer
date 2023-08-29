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

        public string ServerIp => serverIp;

        public int ServerPort => serverPort;

        public string LocalIp => localIp;

        public int LocalPort => localPort;

        public int ConnectionTimeOut => connectionTimeOut;
    }
}