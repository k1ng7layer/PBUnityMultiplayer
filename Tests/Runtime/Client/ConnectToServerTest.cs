using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Client.Impl;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PBUnityMultiplayer.Tests.Runtime.Client
{
    public class ConnectToServerTest
    {
        private const string Scene = "ClientTestScene";
        private const string Password = "12";
        
        [UnityTest]
        public IEnumerator SuccessfullyConnectToServer()
        {
            SceneManager.LoadScene(Scene);

            yield return new WaitForSeconds(3f);

            var clientManager = Object.FindObjectOfType<NetworkClientManager>();
            var transport = Object.FindObjectOfType<TransportMock>();

            clientManager.StartClient();
            clientManager.ConnectToServer(Password);

            yield return new WaitForSeconds(1f);
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)EConnectionResult.Success);
            byteWriter.AddInt32(0);
            byteWriter.AddString("");
            byteWriter.AddString(IPAddress.Any.ToString());
            byteWriter.AddInt32(0);
            
            var transportMessage = new TestMessage(null, byteWriter.Data);
            
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.NotNull(clientManager.LocalClient);
            Assert.AreEqual(0, clientManager.LocalClient.Id);
            Assert.AreEqual(1, clientManager.ClientsTable.Count);
            Assert.AreEqual(1, clientManager.Clients.Count());
        }
        
        [UnityTest]
        public IEnumerator UnsuccessfullyConnectToServer()
        {
            SceneManager.LoadScene(Scene);

            yield return new WaitForSeconds(3f);

            var clientManager = Object.FindObjectOfType<NetworkClientManager>();
            var transport = Object.FindObjectOfType<TransportMock>();

            clientManager.StartClient();
            clientManager.ConnectToServer(Password);

            yield return new WaitForSeconds(1f);
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)EConnectionResult.Reject);
            byteWriter.AddInt32(0);
            byteWriter.AddString("");
            
            var transportMessage = new TestMessage(null, byteWriter.Data);
            
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.Null(clientManager.LocalClient);
            Assert.AreEqual(0, clientManager.ClientsTable.Count);
            Assert.AreEqual(0, clientManager.Clients.Count());
        }
        
        [UnityTest]
        public IEnumerator MultiClientConnected()
        {
            SceneManager.LoadScene(Scene);

            yield return new WaitForSeconds(3f);

            var clientManager = Object.FindObjectOfType<NetworkClientManager>();
            var transport = Object.FindObjectOfType<TransportMock>();

            clientManager.StartClient();
            clientManager.ConnectToServer(Password);

            yield return new WaitForSeconds(1f);
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)EConnectionResult.Success);
            byteWriter.AddInt32(0);
            byteWriter.AddString("");
            byteWriter.AddString(IPAddress.Any.ToString());
            byteWriter.AddInt32(0);
            
            var transportMessage = new TestMessage(null, byteWriter.Data);
            
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.NotNull(clientManager.LocalClient);
            Assert.AreEqual(0, clientManager.LocalClient.Id);
            
            byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
            byteWriter.AddInt32(1);
            byteWriter.AddString(IPAddress.Any.ToString());
            byteWriter.AddInt32(0);
            
            transportMessage = new TestMessage(null, byteWriter.Data);
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.AreEqual(2, clientManager.ClientsTable.Count);
            Assert.AreEqual(2, clientManager.Clients.Count());
        }
        
        [UnityTest]
        public IEnumerator ClientDisconnected()
        {
            SceneManager.LoadScene(Scene);

            yield return new WaitForSeconds(3f);

            var clientManager = Object.FindObjectOfType<NetworkClientManager>();
            var transport = Object.FindObjectOfType<TransportMock>();

            clientManager.StartClient();
            clientManager.ConnectToServer(Password);

            yield return new WaitForSeconds(1f);
            
            var byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.AuthenticationResult);
            byteWriter.AddUshort((ushort)EConnectionResult.Success);
            byteWriter.AddInt32(0);
            byteWriter.AddString("");
            byteWriter.AddString(IPAddress.Any.ToString());
            byteWriter.AddInt32(0);
            
            var transportMessage = new TestMessage(null, byteWriter.Data);
            
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.NotNull(clientManager.LocalClient);
            Assert.AreEqual(0, clientManager.LocalClient.Id);
            
            byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientConnected);
            byteWriter.AddInt32(1);
            byteWriter.AddString(IPAddress.Any.ToString());
            byteWriter.AddInt32(0);
            
            transportMessage = new TestMessage(null, byteWriter.Data);
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.AreEqual(2, clientManager.ClientsTable.Count);
            Assert.AreEqual(2, clientManager.Clients.Count());
            
            byteWriter = new ByteWriter();
            byteWriter.AddUshort((ushort)ENetworkMessageType.ClientDisconnected);
            byteWriter.AddInt32(1);
            byteWriter.AddString("");
            
            transportMessage = new TestMessage(null, byteWriter.Data);
            transport.ProcessMessage(transportMessage);
            
            yield return new WaitForSeconds(1f);
            
            Assert.NotNull(clientManager.LocalClient);
            Assert.AreEqual(1, clientManager.ClientsTable.Count);
            Assert.AreEqual(1, clientManager.Clients.Count());
        }
    }
}