using System;
using System.Collections.Generic;
using System.Net;

namespace PBUnityMultiplayer.Runtime.Core.ConnectionHandler
{
    internal interface IConnectionHandler
    {
        Action<byte[], IPEndPoint> MessageReceived {get;}
        
        public IReadOnlyDictionary<int, Player> ConnectedPlayers { get; }
        
        void StartConnection(ERunningMode runningMode);
        void CloseConnection();
    }
}