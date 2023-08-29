using System;

namespace PBUnityMultiplayer.Runtime.Core.MessageHandling
{
    public interface IMessageHandlersService
    {
        bool TryGetHandlerId<T>(out string id);
        void RegisterHandler<T>(Action<T> handler) where T : struct;
        void CallHandler(string id, byte[] payload);
    }
}