using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Authentication
{
    public interface IAuthenticationService
    {
        event Action<AuthenticateResult, NetworkClient> OnAuthenticated;
        // void AuthenticateServer(NetworkClient client, byte[] authPayload);
        // void AuthenticateClient(NetworkClient client, byte[] authPayload);
        void AuthenticateServer(NetworkClient client);
        void AuthenticateClient(NetworkClient client);
    }
}