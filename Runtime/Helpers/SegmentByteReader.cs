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
            return BitConverter.ToInt32(_arraySegment.Slice(_readPosition, 4));
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(_arraySegment.Slice(_readPosition, 4));
        }
        
        public float ReadUshort()
        {
            return BitConverter.ToUInt16(_arraySegment.Slice(_readPosition, 2));
        }
        
        public string ReadString(out int stringLength)
        {
            stringLength = ReadInt32();
            var stringBytes =  stringLength == 0 ? 
                Array.Empty<byte>() : _arraySegment.Slice(_readPosition, stringLength);
            var result = Encoding.UTF8.GetString(stringBytes);

            return result;
        }

        public byte[] ReadBytes(int count)
        {
            if (_readPosition + count > _arraySegment.Count)
                throw new IndexOutOfRangeException();
            
            return _arraySegment.Slice(_readPosition, count).ToArray();
        }
    }
}