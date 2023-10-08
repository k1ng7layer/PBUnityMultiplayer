using System;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Authentication.Impl
{
    public class ServerAuthentication : AuthenticationServiceBase
    {
        [SerializeField] private string serverPassword = "12";
        
        public override AuthenticateResult Authenticate(int clientId, ArraySegment<byte> connectionMessage)
        {
            EConnectionResult connectionResult;
            var message = string.Empty;
            
            var byteRear = new ByteReader(connectionMessage);
            var password = byteRear.ReadString().Trim();
            Debug.Log($"password = {password}");
            
            if (serverPassword == password)
            {
                connectionResult = EConnectionResult.Success;
            }
            else
            {
                connectionResult = EConnectionResult.Reject;
                message = "Wrong credentials";
            }

            OnAuthenticated?.Invoke(new AuthenticateResult(connectionResult, message), clientId);
            
            var authResult = new AuthenticateResult(connectionResult, message);
            
            return authResult;
        }
    }
}