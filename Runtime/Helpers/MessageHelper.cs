using System;
using PBUdpTransport.Utils;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public static class MessageHelper
    {
        public static ENetworkMessageType GetMessageType(byte[] data)
        {
            var byteSpan = new Span<byte>(data);
            var messageTypeSpan = byteSpan.Slice(2, 2);
            var flagsInt = BitConverter.ToUInt16(messageTypeSpan);

            var result = ENetworkMessageType.None;

            if (Enum.TryParse(flagsInt.ToString(), out ENetworkMessageType messageType))
                result = messageType;
            
            return result;
        }
    }
}