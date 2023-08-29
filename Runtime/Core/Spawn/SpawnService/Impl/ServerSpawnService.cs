using System;
using System.Collections.Generic;
using PBUnityMultiplayer.Runtime.Configuration.Prefabs;
using PBUnityMultiplayer.Runtime.Core.MessageHandling;
using PBUnityMultiplayer.Runtime.Core.NetworkManager.Models;
using PBUnityMultiplayer.Runtime.Core.NetworkObjects;
using PBUnityMultiplayer.Runtime.Core.Spawn.SpawnedRepository;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PBUnityMultiplayer.Runtime.Core.Spawn.SpawnService.Impl
{
    internal class ServerSpawnService : INetworkSpawnService
    {
        private readonly INetworkPrefabsBase _networkPrefabsBase;
        private readonly IMessageHandlersService _messageHandlersService;
        private readonly INetworkSpawnedObjectsRepository _networkSpawnedObjectsRepository;
        private readonly Dictionary<int, List<NetworkObject>> _spawnedObjects;
        private ushort _nextId;

        public ServerSpawnService(
            INetworkPrefabsBase networkPrefabsBase, 
            IMessageHandlersService messageHandlersService,
            INetworkSpawnedObjectsRepository networkSpawnedObjectsRepository
        )
        {
            _networkPrefabsBase = networkPrefabsBase;
            _messageHandlersService = messageHandlersService;
            _networkSpawnedObjectsRepository = networkSpawnedObjectsRepository;
        }

        public event Action<SpawnResult> Spawned;

        public void RegisterSpawnHandler<T>(Action<T> handler) where T : struct
        {
            _messageHandlersService.RegisterHandler(handler);
        }

        public SpawnResult Spawn<T>(
            int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation, 
            int parentObjectId) where T : struct
        {
            var prefab = _networkPrefabsBase.Get(prefabId);
            var hasHandler = _messageHandlersService.TryGetHandlerId<T>(out var handlerId);
            
            if (!hasHandler)
                throw new Exception(
                    $"[{nameof(ServerSpawnService)}] spawn handler of type {nameof(T)} doesn't registered");
            
            var hasParent = _networkSpawnedObjectsRepository.TryGetObject(parentObjectId, out var parentObject);
            
            if (!hasParent)
                throw new Exception(
                    $"[{nameof(ServerSpawnService)}] can't find parent object with id {parentObjectId}");
            
            var networkObj = Object.Instantiate(prefab, position, rotation, parentObject.transform);
            
            networkObj.Spawn(_nextId++, false);
            owner.AddOwnership(networkObj);

            _networkSpawnedObjectsRepository.TryAdd(networkObj.Id, networkObj);
            
            var spawnedResult = new SpawnResult(
                prefabId,
                position, 
                rotation, 
                parentObjectId, 
                handlerId);
            
            return spawnedResult;
        }

        public SpawnResult Spawn<T>(
            int prefabId, 
            NetworkClient owner, 
            Vector3 position, 
            Quaternion rotation
            ) where T : struct
        {
            var prefab = _networkPrefabsBase.Get(prefabId);
            var hasHandler = _messageHandlersService.TryGetHandlerId<T>(out var handlerId);

            if (!hasHandler)
                throw new Exception(
                    $"[{nameof(ServerSpawnService)}] spawn handler of type {nameof(T)} doesn't registered");
            
            var networkObj = Object.Instantiate(prefab, position, rotation);
            
            networkObj.Spawn(_nextId++, false);
            owner.AddOwnership(networkObj);

            _networkSpawnedObjectsRepository.TryAdd(networkObj.Id, networkObj);
            
            var spawnedResult = new SpawnResult(
                prefabId,
                position, 
                rotation, 
                null, 
                handlerId);
            
            return spawnedResult;
        }

        public SpawnResult Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation, int parentObjectId)
        {
            var prefab = _networkPrefabsBase.Get(prefabId);

            var hasParent = _networkSpawnedObjectsRepository.TryGetObject(parentObjectId, out var parentObject);
            
            if (!hasParent)
                throw new Exception(
                    $"[{nameof(ServerSpawnService)}] can't find parent object with id {parentObjectId}");
            
            var networkObj = Object.Instantiate(prefab, position, rotation, parentObject.transform);
            
            networkObj.Spawn(_nextId++, false);
            owner.AddOwnership(networkObj);

            _networkSpawnedObjectsRepository.TryAdd(networkObj.Id, networkObj);
            
            var spawnedResult = new SpawnResult(
                prefabId,
                position, 
                rotation, 
                parentObjectId, 
                "none");
            
            return spawnedResult;
        }

        public SpawnResult Spawn(int prefabId, NetworkClient owner, Vector3 position, Quaternion rotation)
        {
            var prefab = _networkPrefabsBase.Get(prefabId);

            var networkObj = Object.Instantiate(prefab, position, rotation);
            
            networkObj.Spawn(_nextId++, false);
            owner.AddOwnership(networkObj);

            _networkSpawnedObjectsRepository.TryAdd(networkObj.Id, networkObj);
            
            var spawnedResult = new SpawnResult(
                prefabId,
                position, 
                rotation, 
                null, 
                "handlerId");

            return spawnedResult;
        }
    }
}