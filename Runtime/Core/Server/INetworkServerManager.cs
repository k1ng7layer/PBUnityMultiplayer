using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Server
{
    public interface INetworkServerManager
    {
        event Action ClientConnectedToServer;
        event Action<NetworkClient> SeverAuthenticated;
        event Action<int> SeverClientDisconnected;
        event Action<int> SeverClientConnected;
        
        void StartServer();
        void StopServer();
    }
}