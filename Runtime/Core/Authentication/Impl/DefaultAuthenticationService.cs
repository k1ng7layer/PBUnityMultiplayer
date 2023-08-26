using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.Authentication.Impl
{
    public class DefaultAuthenticationService : IAuthenticationService
    {
        private const string PASSWORD = "1234";

        public event Action<NetworkClient> OnAuthenticated;

        public AuthenticateResult Authenticate(NetworkClient client, byte[] message)
        {
            var byteRear = new ByteReader(message);
            var password = byteRear.ReadString();

            var connectionResult = EConnectionResult.Reject;
            var respMessage = string.Empty;
            
            if (PASSWORD == password)
            {
                connectionResult = EConnectionResult.Success;
                
                OnAuthenticated?.Invoke(client);
            }
            else
            {
                connectionResult = EConnectionResult.Reject;
                respMessage = "Wrong credentials";
            }
            
            return new AuthenticateResult(connectionResult, respMessage);
        }
    }
}