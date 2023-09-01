using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.Connection.Client;
using PBUnityMultiplayer.Runtime.Core.Connection.Server;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager
{
    public interface INetworkManager
    {
        // public GameServer Server { get; }
        // public GameClient Client { get; }
        // event Action ClientConnectedToServer;
        // event Action<NetworkClient> SeverAuthenticated;
        // event Action<AuthenticateResult> ClientAuthenticated;
        // void StartServer();
        // void StopServer();
        // UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password);
    }
}