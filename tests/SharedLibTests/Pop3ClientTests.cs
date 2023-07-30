using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Communication;

namespace SharedLibTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class Pop3ClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_NullUri_Throws_ArgumentNullException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize(null, "user", "pass", 987);
        }

        [Ignore("uri no longer checked")]
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Initialize_UriNotAbsolute_Throws_ArgumentException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize("pop3.website.org", "user", "pass", 987);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_UseNameNull_Throws_ArgumentNullException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize("pop3.website.org", null, "pass", 987);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_UseNameEmptyString_Throws_ArgumentNullException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize("pop3.website.org", "", "pass", 987);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_PasswordNull_Throws_ArgumentNullException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize("pop3.website.org", "user", null, 987);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_PasswordEmptyString_Throws_ArgumentNullException()
        {
            using Pop3Client sut = new Pop3Client();
            sut.Initialize("pop3.website.org", "user", "", 987);
        }

        [TestMethod]
        public void Initialize_Success()
        {
            using Pop3Client sut = new Pop3Client();

            string pop3Server = Environment.GetEnvironmentVariable("EmailPop3ServerName");
            bool useSSL = Environment.GetEnvironmentVariable("EmailSSL").Equals("true");
            string userName = Environment.GetEnvironmentVariable("EmailUserName");
            string password = Environment.GetEnvironmentVariable("EmailUserPassword");
            string port = Environment.GetEnvironmentVariable("EmailPop3Port");

            sut.Initialize(pop3Server, userName, password, ushort.Parse(port));
            Assert.IsTrue(sut.IsConnected);
        }

        [TestMethod]
        public void GetMailCount_Success()
        {
            using Pop3Client sut = new Pop3Client();

            string pop3Server = Environment.GetEnvironmentVariable("EmailPop3ServerName");
            bool useSSL = Environment.GetEnvironmentVariable("EmailSSL").Equals("true");
            string userName = Environment.GetEnvironmentVariable("EmailUserName");
            string password = Environment.GetEnvironmentVariable("EmailUserPassword");
            string port = Environment.GetEnvironmentVariable("EmailPop3Port");

            sut.Initialize(pop3Server, userName, password, ushort.Parse(port));
            Assert.IsTrue(sut.IsConnected);
            int c = sut.GetMailCount(out int sizeInOctets);
            Assert.IsTrue(c > 2);
            Assert.IsTrue(sizeInOctets > 0);
            string message = sut.RetrieveMessage(4, out string initialResponse);
            Assert.IsFalse(String.IsNullOrEmpty(message));
        }

        [TestMethod]
        public void DeleteMail_Success()
        {
            using Pop3Client sut = new Pop3Client();

            string pop3Server = Environment.GetEnvironmentVariable("EmailPop3ServerName");
            bool useSSL = Environment.GetEnvironmentVariable("EmailSSL").Equals("true");
            string userName = Environment.GetEnvironmentVariable("EmailUserName");
            string password = Environment.GetEnvironmentVariable("EmailUserPassword");
            string port = Environment.GetEnvironmentVariable("EmailPop3Port");

            sut.Initialize(pop3Server, userName, password, ushort.Parse(port));
            Assert.IsTrue(sut.IsConnected);
            int c = sut.GetMailCount(out int sizeInOctets);
            Assert.IsTrue(c > 2);
            Assert.IsTrue(sizeInOctets > 0);
            string message = sut.RetrieveMessage(3, out string initialResponse);
            Assert.IsFalse(String.IsNullOrEmpty(message));
            string deleteResponse = sut.DeleteMessage(3);
            Assert.AreEqual("+OK", deleteResponse);
        }
    }
}
