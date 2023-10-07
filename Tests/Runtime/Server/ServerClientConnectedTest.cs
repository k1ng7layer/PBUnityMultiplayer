using System.Collections;
using System.Net;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Server.Impl;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PBUnityMultiplayer.Tests.Runtime.Server
{
    public class ServerClientConnectedTest
    {
        private const string Scene = "ServerClientConnectTest";
        
        [UnityTest]
        public IEnumerator NewClientConnectedWithPasswordTest()
        {
            SceneManager.LoadScene(Scene);

            yield return new WaitForSeconds(3f);

            var serverManager = Object.FindObjectOfType<NetworkServerManager>();
            var transport = Object.FindObjectOfType<TransportMock>();

            serverManager.StartServer();
            
            yield return new WaitForSeconds(1f);
            
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.ConnectionRequest);
            byteWriter.AddString("12");

            var clientEndpoint = new IPEndPoint(IPAddress.Any, 9999);

            var transportMessage = new TestMessage(clientEndpoint, byteWriter.Data);
            
            transport.AddIncomeMessageToReturn(transportMessage);
            
            yield return new WaitForSeconds(2f);
            
            Assert.AreEqual(1, serverManager.ConnectedClients.Count);
        }
        
    }
}