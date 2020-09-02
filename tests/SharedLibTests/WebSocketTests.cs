using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Communication;

namespace SharedLibTests
{
    [TestClass]
    public class WebSocketTests
    {
        [TestMethod]
        public void PerformGetRequest()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            WebSocket webSocket = new WebSocket(ip, 5000);
            webSocket.Connect();
            string result = webSocket.Get(new Uri("http://localhost:5000/"));

            Console.WriteLine(result);
        }
    }
}
