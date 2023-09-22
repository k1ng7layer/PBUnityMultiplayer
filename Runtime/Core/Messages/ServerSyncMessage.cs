using System;

namespace PBUnityMultiplayer.Runtime.Core.Messages
{
    [Serializable]
    public struct ServerSyncMessage
    {
        public int ServerTick;
    }
}