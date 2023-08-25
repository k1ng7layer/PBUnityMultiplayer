﻿using System;
using System.Text;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public class ByteReader
    {
        private readonly byte[] _data;

        private int _readPosition;

        public ByteReader(byte[] data)
        {
            _data = data;
        }

        public int ReadInt32()
        {
            var value = _data[_readPosition] 
                        | (_data[_readPosition + 1] << 8) 
                        | (_data[_readPosition + 2] << 16) 
                        | (_data[_readPosition + 3] << 24);

            _readPosition += 4;
            
            return value;
        }

        public string ReadString()
        {
            var size = ReadInt32();
            var stringBytes =  size == 0 ? 
                Array.Empty<byte>() : new Span<byte>(_data).Slice(_readPosition, size);

            var result = Encoding.UTF8.GetString(stringBytes);

            _readPosition += size;
            
            return result;
        }
    }
}