using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;

namespace PBUnityMultiplayer.Runtime.Core.PlayerService
{
    internal interface IPlayerService
    {
        public IReadOnlyDictionary<int, NetworkClient> ConnectedPlayers { get; }
        
        NetworkClient CreatePlayer();
    }
}