using PBUnityMultiplayer.Runtime.Core.Spawn;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using UnityEngine;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public static class NetworkMessageSerializationHelper
    {
        internal static SpawnResult DeserializeSpawnResult(ByteReader byteReader)
        {
            var prefabId = byteReader.ReadInt32();
            var positionX = byteReader.ReadFloat();
            var positionY = byteReader.ReadFloat();
            var positionZ = byteReader.ReadFloat();
            var quatX = byteReader.ReadFloat();
            var quatY = byteReader.ReadFloat();
            var quatZ = byteReader.ReadFloat();
            var quatW = byteReader.ReadFloat();
            var parentObjectId = byteReader.ReadInt32();
            var spawnHandlerId = byteReader.ReadString();

            var position = new Vector3(positionX, positionY, positionZ);
            var rotation = new Quaternion(quatX, quatY, quatZ, quatW);

            return new SpawnResult(prefabId, position, rotation, parentObjectId, spawnHandlerId);
        }
    
        internal static void SerializeSpawnResult(ByteWriter byteWriter, SpawnResult spawnResult)
        {
            byteWriter.AddInt(spawnResult.PrefabId);
            byteWriter.AddFloat(spawnResult.Position.x);
            byteWriter.AddFloat(spawnResult.Position.y);
            byteWriter.AddFloat(spawnResult.Position.z);
            byteWriter.AddFloat(spawnResult.Rotation.x);
            byteWriter.AddFloat(spawnResult.Rotation.y);
            byteWriter.AddFloat(spawnResult.Rotation.z);
            byteWriter.AddFloat(spawnResult.Rotation.w);
            byteWriter.AddInt(spawnResult.ParentObjectId ?? -1);
            byteWriter.AddString(spawnResult.SpawnHandlerId);
        }
    }
}