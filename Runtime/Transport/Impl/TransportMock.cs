using System;
using System.Collections.Generic;
using System.Net;
using PBUdpTransport.Models;
using PBUdpTransport.Utils;

namespace PBUnityMultiplayer.Runtime.Transport.Impl
{
    public readonly struct TestMessage
    {
        public readonly IPEndPoint RemoteEp;
        public readonly ArraySegment<byte> Message;
        
        public TestMessage(IPEndPoint remoteEp, ArraySegment<byte> msg)
        {
            RemoteEp = remoteEp;
            Message = msg;
        }
    }
    
    public class TransportMock : TransportBase
    {
        private TransportMessage _transportMessage;
        private readonly Queue<TestMessage> _messages = new();
        private bool _running;
        
        public void ProcessMessage(TestMessage message)
        {
            _messages.Enqueue(message);
        }

        public override event Action<EndPoint, ArraySegment<byte>> DataReceived;

        public override void StartTransport(IPEndPoint localEndPoint)
        {
            _running = true;
        }

        public override void Send(byte[] data, int connectionHash, ESendMode sendMode)
        {
            
        }

        public override void Tick()
        {
            if(!_running)
                return;
            
            if (_messages.Count > 0)
            {
                var msg = _messages.Dequeue();
                
                DataReceived?.Invoke(msg.RemoteEp, msg.Message);
            }
        }

        public override void Stop()
        {
            _running = false;
        }
    }
}