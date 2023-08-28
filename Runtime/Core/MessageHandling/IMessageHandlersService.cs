using System;

namespace PBUnityMultiplayer.Runtime.Core.MessageHandling
{
    public interface IMessageHandlersService
    {
        bool TryGetHandlerId<T>(out int id);
        void RegisterHandler<T>(Action<T> handler) where T : struct;
        void CallHandler(int id, byte[] payload);
    }
}