namespace PBUnityMultiplayer.Runtime.Core.MessageHandling
{
    public delegate void NetworkMessageHandler(NetworkMessageDeserializer deserializer, byte[] payload);
}