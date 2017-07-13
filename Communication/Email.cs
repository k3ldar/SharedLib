/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Email Send Class
 *
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;

namespace Shared.Communication
{
    /// <summary>
    /// Send email class
    /// </summary>
    public sealed class Email
    {
        #region Private Members


        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public Email()
        {
            try
            {
                Sender = StringCipher.Decrypt(XML.GetXMLValue("Email", "Sender"), StringCipher.DefaultPassword);
                Host = StringCipher.Decrypt(XML.GetXMLValue("Email", "Host"), StringCipher.DefaultPassword);
                Port = Convert.ToInt32(StringCipher.Decrypt(XML.GetXMLValue("Email", "Port"), StringCipher.DefaultPassword));
                User = StringCipher.Decrypt(XML.GetXMLValue("Email", "User"), StringCipher.DefaultPassword);
                Password = StringCipher.Decrypt(XML.GetXMLValue("Email", "Password"), StringCipher.DefaultPassword);
                SSL = StringCipher.Decrypt(XML.GetXMLValue("Email", "SSL"), StringCipher.DefaultPassword).ToLower() == "true";
            }
            catch
            {

            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Saves the email settings to file
        /// </summary>
        public void Save()
        {
            XML.SetXMLValue("Email", "Sender", StringCipher.Encrypt(Sender, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Host", StringCipher.Encrypt(Host, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Port", StringCipher.Encrypt(Port.ToString(), StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "User", StringCipher.Encrypt(User, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Password", StringCipher.Encrypt(Password, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "SSL", StringCipher.Encrypt(SSL.ToString(), StringCipher.DefaultPassword));
        }

        /// <summary>
        /// Send a test email 
        /// </summary>
        /// <returns></returns>
        public bool SendTestEmail()
        {

            return (SmtpHelper.TestSend("Test email", "Connection Test", User, User, Password, Host, Port, SSL));
        }

        #endregion Public Methods

        #region Properties

        /// <summary>
        /// Email Sender
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Email Host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port used to send emails
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Email user name
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Email Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Indicates wether SSL is used for sending emails
        /// </summary>
        public bool SSL { get; set; }

        #endregion Properties
    }

    internal static class SmtpHelper
    {

        internal static bool TestSend(string message, string subject, string recipient,
            string userName, string password, string host, int port, bool ssl)
        {
            SmtpClient SMTPServer = new SmtpClient(host);
            SMTPServer.Port = port;
            SMTPServer.EnableSsl = ssl;

            SMTPServer.Credentials = new System.Net.NetworkCredential(userName, password);
            try
            {
                SMTPServer.Send(new MailMessage(recipient, recipient, subject, message));
                return (true);
            }
            catch
            {
                return (false);
            }
        }

        /// <summary>
        /// test the smtp connection by sending a HELO command
        /// </summary>
        /// <param name="smtpServerAddress"></param>
        /// <param name="port"></param>
        internal static bool TestConnection(string smtpServerAddress, int port)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(smtpServerAddress);
            IPEndPoint endPoint = new IPEndPoint(hostEntry.AddressList[0], port);

            using (Socket tcpSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                //try to connect and test the rsponse for code 220 = success
                tcpSocket.Connect(endPoint);

                if (!CheckResponse(tcpSocket, 220))
                {
                    return false;
                }

                // send HELO and test the response for code 250 = proper response
                SendData(tcpSocket, string.Format("HELO {0}\r\n", Dns.GetHostName()));

                return (CheckResponse(tcpSocket, 250));
            }
        }

        private static void SendData(Socket socket, string data)
        {
            byte[] dataArray = Encoding.ASCII.GetBytes(data);
            socket.Send(dataArray, 0, dataArray.Length, SocketFlags.None);
        }

        private static bool CheckResponse(Socket socket, int expectedCode)
        {
            while (socket.Available == 0)
            {
                System.Threading.Thread.Sleep(100);
            }

            byte[] responseArray = new byte[1024];
            socket.Receive(responseArray, 0, socket.Available, SocketFlags.None);
            string responseData = Encoding.ASCII.GetString(responseArray);
            int responseCode = Convert.ToInt32(responseData.Substring(0, 3));

            return (responseCode == expectedCode);
        }
    }

}
