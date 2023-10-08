using System.Collections;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Client.Impl;
using PBUnityMultiplayer.Runtime.Helpers;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Tests.Runtime.TestUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PBUnityMultiplayer.Tests.Runtime.Client
{
    public class ClientMessageHandlerTest
    {
        private const string Scene = "ClientTestScene";
        private const string MessageText = "Hello";

        private string _message;
        
        [UnityTest]
        public IEnumerator ClientMessageHandler()
        {
            SceneManager.LoadScene(Scene);
            
            yield return new WaitForSeconds(2f);
            
            var clientManager = Object.FindObjectOfType<NetworkClientManager>();
            var transport = Object.FindObjectOfType<TransportMock>();
            
            clientManager.StartClient();
            clientManager.ConnectToServer("");
            clientManager.RegisterMessageHandler<TestHandlerMessage>(OnMessageReceived);

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
            
            transport.ProcessMessage(new TestMessage(null, byteWriter.Data));
            
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