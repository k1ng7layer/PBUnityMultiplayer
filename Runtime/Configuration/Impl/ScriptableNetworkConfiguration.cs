using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Configuration.Impl
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(ScriptableNetworkConfiguration), fileName = nameof(ScriptableNetworkConfiguration))]
    public class ScriptableNetworkConfiguration : ScriptableObject, 
        INetworkConfiguration
    {
        [SerializeField] private string serverIp;
        [SerializeField] private int serverPort;
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