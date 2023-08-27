using System;
using System.Numerics;
using System.Text;

namespace PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers
{
    internal class ByteWriter
    {
        public int WritePos { get; internal set; }
        
        public byte[] Data;
        
        public ByteWriter(int length = 8)
        {
            Data = new byte[length];
        }
        
         public void AddInt(int value)
         { 
            CheckSpaceAndCopy(4); 
            
            Data[WritePos] = (byte)(value >> 24);
            Data[WritePos + 1] = (byte)(value >> 16);
            Data[WritePos + 2] = (byte)(value >> 8);
            Data[WritePos + 3] = (byte)value;

            WritePos += sizeof(int);
        }
        
        public void AddFloat(float value)
        {
            CheckSpaceAndCopy(4);
            
            var bytes = BitConverter.GetBytes(value);
            
            Data[WritePos] = bytes[0];
            Data[WritePos + 1] = bytes[1];
            Data[WritePos + 2] = bytes[2];
            Data[WritePos + 3] = bytes[3];
            
            WritePos += sizeof(float);
        }
        
        public void AddString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            CheckSpaceAndCopy(bytes.Length + 4);
            
            AddInt(bytes.Length);
            
            for (int i = 0; i < bytes.Length; i++)
            {
                Data[WritePos + i] = bytes[i];
            }
            
            WritePos += bytes.Length;
        }
        
        public void AddBytes(byte[] bytes)
        {
            CheckSpaceAndCopy(bytes.Length);
            
            for (int i = 0; i < bytes.Length; i++)
            {
                Data[WritePos + i] = bytes[i];
            }
            
            WritePos += bytes.Length;
        }
        
        public void AddUshort(ushort value)
        {
            CheckSpaceAndCopy(2);
            
            var bytes = BitConverter.GetBytes(value);

            Data[WritePos] = bytes[0];
            Data[WritePos + 1] = bytes[1];

            WritePos += sizeof(ushort);
        }
        
        protected void AddBool(bool value)
        {
            CheckSpaceAndCopy(1);
            
            var bytes = BitConverter.GetBytes(value);

            Data[WritePos] = bytes[0];

            WritePos += 1;
        }
        
        public void AddVector3(Vector3 value)
        {
            AddFloat(value.X);
            AddFloat(value.Y);
            AddFloat(value.Z);
        }
        
        public void AddQuaternion(Quaternion value)
        {
            AddFloat(value.X);
            AddFloat(value.Y);
            AddFloat(value.Z);
            AddFloat(value.W);
        }

        private void CheckSpaceAndCopy(int requiredSpace)
        {
            if (WritePos + requiredSpace > Data.Length)
            {
                var newArray = new byte[Data.Length + 4];
                Buffer.BlockCopy(Data, 0, newArray, 0, Data.Length);
                Data = newArray;
            }
        }
    }
}