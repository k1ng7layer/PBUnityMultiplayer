using System;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using UnityEngine;

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
        public void Spawn<T>(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation, T message) where T : struct;
        public void Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation);
    }
}