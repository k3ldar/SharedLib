using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Communication;

namespace SharedLibTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class WebSocketTests
    {
        [Ignore("Requires running socket")]
        [TestMethod]
        public void PerformGetRequest()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            WebSocket webSocket = new WebSocket(ip, 15000);
            webSocket.Connect();
            string result = webSocket.Get(new Uri("http://localhost:15000/"));

            Console.WriteLine(result);
        }
    }
}
