using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.Authentication.Impl
{
    public class ServerAuthentication : AuthenticationServiceBase
    {
        private const string PASSWORD = "12";

        public override void Authenticate(NetworkClient client, byte[] authPayload)
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
    }
}