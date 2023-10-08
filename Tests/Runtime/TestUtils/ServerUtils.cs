using System.Collections;
using System.Net;
using NUnit.Framework;
using PBUnityMultiplayer.Runtime.Core.Server.Impl;
using PBUnityMultiplayer.Runtime.Transport.Impl;
using PBUnityMultiplayer.Runtime.Transport.PBUdpTransport.Helpers;
using PBUnityMultiplayer.Runtime.Utils;
using UnityEngine;

namespace PBUnityMultiplayer.Tests.Runtime.TestUtils
{
    public static class ServerUtils
    {
        public static IEnumerator ConnectClientToServer()
        {
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
    }
}