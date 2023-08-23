using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct ConnectResult
    {
        public readonly EConnectionResult ConnectionResult;
        public readonly byte[] Payload;

        public ConnectResult(EConnectionResult connectionResult, byte[] payload)
        {
            ConnectionResult = connectionResult;
            Payload = payload;
        }
    }
}