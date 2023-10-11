using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Server.Impl;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using PBUnityMultiplayer.Tests.Runtime.TestUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PBUnityMultiplayer.Tests.Runtime.Server
{
    
    public class ServerConnectionTest
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
            
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(2f);
            
            Assert.AreEqual(1, serverManager.ConnectedClients.Count);
        }

        [UnityTest]
        public IEnumerator ClientDisconnectedFromServerTest()
        {
            SceneManager.LoadScene(Scene);
            
            yield return new WaitForSeconds(2f);
            
            yield return ServerUtils.ConnectClientToServer();
            
            var serverManager = Object.FindObjectOfType<NetworkServerManager>();
            var client = serverManager.Clients.First();
            var transport = Object.FindObjectOfType<TransportMock>();
            
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
            byteWriter.AddInt32(client.Id);
            byteWriter.AddString("");
            
            transport.ProcessMessage(new TestMessage((IPEndPoint)client.RemoteEndpoint, byteWriter.Data));

            yield return new WaitForSecondsRealtime(2f);
            
            Assert.True(serverManager.ConnectedClients.Count == 0);
        }

        [UnityTest]
        public IEnumerator DisconnectClientByTimeOutTest()
        {
            SceneManager.LoadScene(Scene);
            
            yield return new WaitForSeconds(2f);
            
            yield return ServerUtils.ConnectClientToServer();
            
            var serverManager = Object.FindObjectOfType<NetworkServerManager>();
            var client = serverManager.Clients.First();
            var transport = Object.FindObjectOfType<TransportMock>();
            var timeOut = (float)serverManager.Configuration.ClientTimeOutMilliseconds / 1000f;
            var pingSendRate = (timeOut / 2f)/1000f;
            
            SendPingMessage(transport, client.Id);
            
            Assert.AreEqual(1, serverManager.ConnectedClients.Count);

            yield return new WaitForSeconds(pingSendRate);
         
            SendPingMessage(transport, client.Id);
            
            yield return new WaitForSeconds(pingSendRate);
            
            Assert.True(serverManager.ConnectedClients.Count == 1);
            
            SendPingMessage(transport, client.Id);
            
            yield return new WaitForSeconds(pingSendRate);
            
            Assert.True(serverManager.ConnectedClients.Count == 1);
            
            yield return new WaitForSeconds(timeOut + 0.5f);
          
            Assert.IsEmpty(serverManager.ConnectedClients);
        }

        [UnityTest]
        public IEnumerator ClientReadyTest()
        {
            SceneManager.LoadScene(Scene);
            
            yield return new WaitForSeconds(2f);
            
            yield return ServerUtils.ConnectClientToServer();
            
            var serverManager = Object.FindObjectOfType<NetworkServerManager>();
            var client = serverManager.Clients.First();
            var transport = Object.FindObjectOfType<TransportMock>();
            SendPingMessage(transport, client.Id);
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientReady);
            byteWriter.AddInt32(client.Id);
            
            transport.ProcessMessage(new TestMessage((IPEndPoint)client.RemoteEndpoint, byteWriter.Data));

            yield return new WaitForSecondsRealtime(1f);
            
            Assert.True(client.IsReady);
        }

        private void SendPingMessage(TransportMock transport, int clientId)
        {
            var byteWriter = new ByteWriter();
              
            byteWriter.AddUshort((ushort)ENetworkMessageType.Ping);
            byteWriter.AddInt32(clientId);

            var message = new TestMessage(null, byteWriter.Data);
            transport.ProcessMessage(message);
        }
        
    }
}