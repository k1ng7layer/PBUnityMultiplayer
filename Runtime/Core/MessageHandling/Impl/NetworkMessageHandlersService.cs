using System;
using System.Collections.Generic;

namespace PBUnityMultiplayer.Runtime.Core.MessageHandling.Impl
{
    public class NetworkMessageHandlersService : IMessageHandlersService
    {
        private readonly Dictionary<string, List<NetworkMessageHandler>> _registeredHandlersTable = new();

        public bool TryGetHandlerId<T>(out string id)
        {
            id = typeof(T).FullName.ToString();

            var hasId = _registeredHandlersTable.TryGetValue(id, out var handlerId);

            return hasId;
        }

        public void RegisterHandler<T>(Action<T> handler) where T : struct
        {
            var id = typeof(T).FullName;

            if (!_registeredHandlersTable.ContainsKey(id))
            {
                _registeredHandlersTable.Add(id, new List<NetworkMessageHandler>());
            }
            
            var networkHandler = CreateHandler(handler);
            _registeredHandlersTable[id].Add(networkHandler);
        }

        public void CallHandler(string id, byte[] payload)
        {
            var hasHandler = _registeredHandlersTable.TryGetValue(id, out var handlers);
            
            if(!hasHandler)
                return;
            
            foreach (var handler in handlers)
            {
                handler?.Invoke(new NetworkMessageDeserializer(), payload);
            }
        }

        private NetworkMessageHandler CreateHandler<T>(Action<T> handler) where T : struct
            => (deserializer, payload) =>
            {
                var data = deserializer.Deserialize<T>(payload);

                handler?.Invoke(data);
            };
    }
}