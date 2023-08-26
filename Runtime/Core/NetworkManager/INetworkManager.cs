using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager
{
    public interface INetworkManager
    {
        event Action OnClientConnectedToServer;
        event Action OnClientAuthenticated;
        event Action<NetworkClient> OnSeverAuthenticated;
        
        void StartServer();
        void StopServer();
        UniTask<ConnectResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password);
    }
}