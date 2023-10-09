using System;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Authentication
{
    public abstract class AuthenticationServiceBase : MonoBehaviour
    {
        public Action<AuthenticateResult, int> OnAuthenticated;
        public abstract AuthenticateResult Authenticate(int clientId, ArraySegment<byte> connectionMessage);
    }
}