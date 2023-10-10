using System;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.Client
{
    public interface INetworkClientManager
    {
        event Action<int> ClientConnected; 
        event Action<int> ClientDisconnected;
        event Action ClientStarted;
        int Tick { get; }
        NetworkClient LocalClient { get; }
        void StartClient();
        void ConnectToServer(string password);
        void StopClient();
        void SendMessage<T>(T message, ESendMode sendMode);
        void RegisterMessageHandler<T>(Action<T> handler) where T : struct;
    }
}