using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Configuration.Client.Impl
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(DefaultClientConfiguration), fileName = nameof(DefaultClientConfiguration))]
    public class DefaultClientConfiguration : ScriptableObject, 
        IClientConfiguration
    {
        [SerializeField] private string serverIp;
        [SerializeField] private ushort serverPort;
        [SerializeField] private int clientTickRateDivergence;
        [SerializeField] private double clientPingSendRateMilliseconds;

        public string ServerIp => serverIp;
        public ushort ServerPort => serverPort;
        public int ClientTickRateDivergence => clientTickRateDivergence;
        public double ClientPingSendRateMilliseconds => clientPingSendRateMilliseconds;
    }
}