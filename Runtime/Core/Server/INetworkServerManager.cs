using System;
using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnHandlers;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Core.Server
{
    public interface INetworkServerManager
    {
        public IReadOnlyDictionary<int, NetworkClient> ConnectedClients { get; }
        event Action ClientConnectedToServer;
        event Action<NetworkClient> SeverAuthenticated;
        event Action<NetworkClient> ClientReadyToWork;
        event Action<int, string> SeverClientDisconnected;
        event Action<int> SeverClientConnected;
        
        void StartServer();
        void StopServer();
        public void Spawn<T>(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation, T message) where T : struct;
        public void Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation);
        void SendMessage<T>(T message, int networkClientId) where T : struct;
        void SendMessage<T>(T message) where T : struct;
        void RegisterMessageHandler<T>(Action<T> handler) where T: struct;
        void RegisterSpawnHandler<T>(NetworkSpawnHandler<T> handler) where T : struct;
    }
}