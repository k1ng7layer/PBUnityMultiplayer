using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Server.Impl;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Tests.Runtime.TestUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PBUnityMultiplayer.Tests.Runtime.Server
{
    public class SeverMessageHandlerTest
    {
        private const string Scene = "ServerClientConnectTest";
        private const string MessageText = "Hello";

        private string _message;
        
        [UnityTest]
        public IEnumerator ServerMessageHandlerTest()
        {
            SceneManager.LoadScene(Scene);
            
            yield return new WaitForSeconds(2f);
            
            yield return ServerUtils.ConnectClientToServer();
            
            var serverManager = Object.FindObjectOfType<NetworkServerManager>();
            var client = serverManager.Clients.First();
            var transport = Object.FindObjectOfType<TransportMock>();
            
            serverManager.RegisterMessageHandler<TestHandlerMessage>(OnMessageReceived);

            var message = new TestHandlerMessage
            {
                payload = MessageText,
            };
            var byteWriter = new ByteWriter();
            var handlerId = typeof(TestHandlerMessage).FullName;
            
            var payload = BinarySerializationHelper.Serialize(message);
            
            byteWriter.AddUshort((ushort)ENetworkMessageType.NetworkMessage);
            byteWriter.AddString(handlerId);
            byteWriter.AddInt32(payload.Length);
            byteWriter.AddBytes(payload);
            
            transport.ProcessMessage(new TestMessage((IPEndPoint)client.RemoteEndpoint, byteWriter.Data));
            
            //yield return new WaitUntilWithTimeOut((() => _message == MessageText), 2f);
            yield return new WaitForSeconds(2f);
            
            Assert.AreEqual(MessageText, _message);
        }

        private void OnMessageReceived(TestHandlerMessage handlerMessage)
        {
            var message = handlerMessage.payload;
            _message = message;
            Assert.AreEqual(MessageText, message);
        }
    }
}