using System.Net;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Transport
{
    public interface ITransport
    {
        void Start();
        void Send(byte[] data, IPEndPoint remoteEndpoint, ESendMode sendMode);
    }
}