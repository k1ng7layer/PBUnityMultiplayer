using System;
using PBUnityMultiplayer.Runtime.Utils;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public static class MessageHelper
    {
        internal static ENetworkMessageType GetMessageType(ArraySegment<byte> data)
        {
            var messageTypeSpan = data.Slice(0, 2);
            var flagsInt = BitConverter.ToUInt16(messageTypeSpan);

            var result = ENetworkMessageType.None;

            if (Enum.TryParse(flagsInt.ToString(), out ENetworkMessageType messageType))
                result = messageType;
            
            return result;
        }

        public static int GetPlayerId(byte[] data)
        {
            var byteSpan = new Span<byte>(data);
            var idSlice = byteSpan.Slice(2, 4);
            var id = BitConverter.ToInt32(idSlice);

            return id;
        }

        // public static IPEndPoint GetPlayerEndpoint(byte[] data)
        // {
        //     // var ipStringSpan = new Span<byte>(data);
        //     // var ipStringSlice = ipStringSpan.Slice(6, 15);
        //     // var ipStringSpan = new Span<byte>(data);
        //     // var ipStringSlice = ipStringSpan.Slice(6, 15);
        // }
    }
}