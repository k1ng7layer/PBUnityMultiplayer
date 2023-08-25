using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Core.Authentication.Impl
{
    public class DefaultAuthenticationService : IAuthenticationService
    {
        private const string PASSWORD = "1234";
        
        public AuthenticateResult Authenticate(NetworkClient client, byte[] message)
        {
            var byteRear = new ByteReader(message);
            var password = byteRear.ReadString();

            if (PASSWORD == password)
                return new AuthenticateResult(EConnectionResult.Success, string.Empty);

            return new AuthenticateResult(EConnectionResult.Reject, "Wrong credentials");
        }
    }
}