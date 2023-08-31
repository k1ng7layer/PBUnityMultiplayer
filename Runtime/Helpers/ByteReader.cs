using System;
using System.Text;
using UnityEngine;

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
        
        public ByteReader(byte[] data, int startOffset)
        {
            _data = data;
            _readPosition = startOffset;
        }

        public int ReadInt32()
        {
            // var value = _data[_readPosition] 
            //             | (_data[_readPosition + 1] << 8) 
            //             | (_data[_readPosition + 2] << 16) 
            //             | (_data[_readPosition + 3] << 24);
            
            var intSpan = new Span<byte>(_data, _readPosition, sizeof(int));
            var value = BitConverter.ToInt32(intSpan);

            _readPosition += 4;
            
            return value;
        }

        public string ReadString()
        {
            var size = ReadInt32();
            var stringBytes = size == 0 ? 
                Array.Empty<byte>() : new Span<byte>(_data).Slice(_readPosition, size);

            var result = Encoding.UTF8.GetString(stringBytes);

            _readPosition += size;
            
            return result;
        }
        
        public Vector3 ReadVector3()
        {
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();
            
            return new Vector3(x, y, z);
        }
        
        public Quaternion ReadQuaternion()
        {
            var x = ReadFloat();
            var y = ReadFloat();
            var z = ReadFloat();
            var w = ReadFloat();
            
            return new Quaternion(x, y, z, w);
        }

        public float ReadFloat()
        {
            var intSpan = new Span<byte>(_data, _readPosition, sizeof(float));
            var value = BitConverter.ToSingle(intSpan);

            _readPosition += 4;
            
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            if (_readPosition + count > _data.Length)
                throw new IndexOutOfRangeException();
            
            var bytes = new byte[count];

            for (int i = 0; i < count; i++)
            {
                bytes[i] = _data[_readPosition + i];
            }

            return bytes;
        }
        
        public string ReadString(out int stringLength)
        {
            stringLength = ReadInt32();
            var stringBytes =  stringLength == 0 ? 
                Array.Empty<byte>() : new Span<byte>(_data).Slice(_readPosition, stringLength);

            var result = Encoding.UTF8.GetString(stringBytes);

            _readPosition += stringLength;
            
            return result;
        }
        
        public ushort ReadUshort()
        {
            // var value = _data[_readPosition] 
            //             | (_data[_readPosition + 1] << 8) 
            //             | (_data[_readPosition + 2] << 16) 
            //             | (_data[_readPosition + 3] << 24);
            
            var intSpan = new Span<byte>(_data, _readPosition, sizeof(int));
            var value = BitConverter.ToUInt16(intSpan);

            _readPosition += 2;
            
            return value;
        }
    }
}