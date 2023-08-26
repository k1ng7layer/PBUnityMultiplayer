using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Authentication
{
    public interface IAuthenticationService
    {
        event Action<NetworkClient> OnAuthenticated;
        AuthenticateResult Authenticate(NetworkClient client, byte[] message);
    }
}