using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PBUnityMultiplayer.Runtime.Core.MessageHandling
{
    public class NetworkMessageDeserializer
    {
        public T Deserialize<T>(byte[] array)
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            
            return (T)formatter.Deserialize(stream);
        }
    }
}