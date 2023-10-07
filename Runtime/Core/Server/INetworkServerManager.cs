using System;
using System.Collections.Generic;
using PBUdpTransport.Utils;
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
        void SendMessage<T>(int networkClientId, T message, ESendMode sendMode) where T : struct;
        void SendMessage<T>(T message, ESendMode sendMode) where T : struct;
        void RegisterMessageHandler<T>(Action<T> handler) where T: struct;
    }
}