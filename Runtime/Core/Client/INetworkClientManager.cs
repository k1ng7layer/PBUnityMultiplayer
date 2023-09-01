using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Client
{
    public interface INetworkClientManager
    {
        UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password);
        void StopClient();
        void SendMessage<T>(T message, ESendMode sendMode);
        void Spawn(int prefabId, Vector3 position, Quaternion rotation);
        void Spawn<T>(int prefabId, Vector3 position, Quaternion rotation, T message);
    }
}