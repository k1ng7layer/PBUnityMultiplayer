namespace PBUnityMultiplayer.Runtime.Utils.IdGenerator
{
    public interface IIdGenerator<T> where T: struct
    {
        T Next();
    }
}