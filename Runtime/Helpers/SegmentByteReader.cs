using System;
using System.Text;

namespace PBUnityMultiplayer.Runtime.Helpers
{
    public class SegmentByteReader
    {
        private readonly ArraySegment<byte> _arraySegment;
        private int _readPosition;

        public SegmentByteReader(ArraySegment<byte> arraySegment)
        {
            _arraySegment = arraySegment;
        }
        
        public SegmentByteReader(ArraySegment<byte> arraySegment, int offset)
        {
            _arraySegment = arraySegment;
            _readPosition = offset;
        }

        public int ReadInt32()
        {
            var value = BitConverter.ToInt32(_arraySegment.Slice(_readPosition, 4));
            _readPosition += 4;
            
            return value;
        }

        public float ReadFloat()
        {
            var value = BitConverter.ToSingle(_arraySegment.Slice(_readPosition, 4));
            _readPosition += 4;
            
            return value;
        }
        
        public float ReadUshort()
        {
            var value = BitConverter.ToUInt16(_arraySegment.Slice(_readPosition, 2));
            _readPosition += 2;

            return value;
        }
        
        public string ReadString(out int stringLength)
        {
            stringLength = ReadInt32();
            var stringBytes =  stringLength == 0 ? 
                Array.Empty<byte>() : _arraySegment.Slice(_readPosition, stringLength);
            var result = Encoding.UTF8.GetString(stringBytes);

            _readPosition += stringLength;

            return result;
        }

        public byte[] ReadBytes(int count)
        {
            if (_readPosition + count > _arraySegment.Count)
                throw new IndexOutOfRangeException();
            
            var value =  _arraySegment.Slice(_readPosition, count).ToArray();
            _readPosition += value.Length;

            return value;
        }
    }
}