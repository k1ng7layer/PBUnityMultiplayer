using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public static class BinarySerializationHelper
    {
        public static byte[] Serialize<T>(T data)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, data);
            
            return stream.ToArray();
        }

        public static T Deserialize<T>(byte[] array)
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            
            return (T)formatter.Deserialize(stream);
        }
    }
}