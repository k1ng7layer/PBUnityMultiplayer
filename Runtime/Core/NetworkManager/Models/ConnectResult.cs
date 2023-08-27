using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct ConnectResult
    {
        public readonly EConnectionResult ConnectionResult;
        private readonly string _message;

        public ConnectResult(EConnectionResult connectionResult, string message)
        {
            ConnectionResult = connectionResult;
            _message = message;
        }
    }
}