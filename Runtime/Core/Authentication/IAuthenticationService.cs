using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.Authentication
{
    public interface IAuthenticationService
    {
        AuthenticateResult Authenticate(NetworkClient client, byte[] message);
    }
}