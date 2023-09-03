namespace PBUnityMultiplayer.Runtime.Utils.IdGenerator.Impl
{
    public class NetworkObjectIdGenerator : IIdGenerator<ushort>
    {
        public NetworkObjectIdGenerator()
        {
            
        }
        private ushort _next;
        
        public ushort Next()
        {
            return _next++;
        }
    }
}