using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.Authentication.Impl
{
    public class DefaultAuthenticationService : IAuthenticationService
    {
        private const string PASSWORD = "1234";

        public void AuthenticateServer(NetworkClient client, byte[] authPayload)
        {
            var byteRear = new ByteReader(authPayload);
            var password = byteRear.ReadString();

            if (PASSWORD == password)
            {
                client.IsApproved = true;
                OnAuthenticated?.Invoke(new AuthenticateResult(EConnectionResult.Success, "Success"), client);
                return;
            }
            
            OnAuthenticated?.Invoke(new AuthenticateResult(EConnectionResult.Reject, "Wrong credentials"), client);
        }

        public void AuthenticateClient(NetworkClient client, byte[] authPayload)
        {
            
        }

        public event Action<AuthenticateResult, NetworkClient> OnAuthenticated;
        
        public void AuthenticateServer(NetworkClient client)
        {
        
        }

        public void AuthenticateClient(NetworkClient client)
        {
            
        }
    }
}