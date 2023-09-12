using System;
using System.Net;
using Cysharp.Threading.Tasks;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Core.Authentication;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Client
{
    public interface INetworkClientManager
    {
        NetworkClient LocalClient { get; }
        void StartClient();
        UniTask<AuthenticateResult> ConnectToServerAsClientAsync(IPEndPoint serverEndPoint, string password);
        void StopClient();
        void SendMessage<T>(T message, ESendMode sendMode);
        void Spawn(int prefabId, Vector3 position, Quaternion rotation);
        void Spawn<T>(int prefabId, Vector3 position, Quaternion rotation, T message);
        void RegisterMessageHandler<T>(Action<T> handler) where T : struct;
        void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T: struct;
    }
}