using System;
using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Server
{
    public interface INetworkServerManager
    {
        int Tick { get; }
        IReadOnlyDictionary<int, NetworkClient> ConnectedClients { get; }
        event Action ClientConnectedToServer;
        event Action<NetworkClient> SeverAuthenticated;
        event Action<int> ClientReady;
        event Action<int> ClientDisconnected;
        event Action<int> ClientConnected;
        
        void StartServer();
        void StopServer();
        void SendMessage<T>(T message, int networkClientId) where T : struct;
        void SendMessage<T>(T message) where T : struct;
        void RegisterMessageHandler<T>(Action<T> handler) where T: struct;
    }
}