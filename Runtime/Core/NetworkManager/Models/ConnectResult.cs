using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager.Models
{
    public readonly struct ConnectResult
    {
        public readonly EConnectionResult ConnectionResult;
        public readonly string Message;

        public ConnectResult(EConnectionResult connectionResult, string message)
        {
            ConnectionResult = connectionResult;
            Message = message;
        }
    }
}