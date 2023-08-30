namespace PBUnityMultiplayer.Runtime.Utils.IdGenerator.Impl
{
    public class NetworkObjectIdGenerator : IIdGenerator<ushort>
    {
        private ushort _next;
        
        public ushort Next()
        {
            return _next++;
        }
    }
}