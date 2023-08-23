using System.Net;
using Cysharp.Threading.Tasks;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.NetworkManager
{
    public interface INetworkManager
    {
        void StartServer();
        UniTask<ConnectResult> ConnectToServer(IPEndPoint serverEndPoint, string password);
    }
}