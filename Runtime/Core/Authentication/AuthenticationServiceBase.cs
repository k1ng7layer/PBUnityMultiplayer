using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Authentication
{
    public abstract class AuthenticationServiceBase
    {
        public event Action<AuthenticateResult, NetworkClient> OnAuthenticated;
        public abstract void Authenticate(NetworkClient client, byte[] authPayload);
    }
}