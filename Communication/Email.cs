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
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;

#pragma warning disable IDE0017 // object initialization can be simplified

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
        /// 
        /// Reads settings from saved/encrypted file
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

        /// <summary>
        /// Constructor
        /// 
        /// User defined details
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="host"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        public Email(string sender, string host, string userName, string password, int port, bool ssl)
        {
            Sender = sender;
            Host = host;
            User = userName;
            Password = password;
            Port = port;
            SSL = ssl;
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

        /// <summary>
        /// Sends and email with optional attachments
        /// </summary>
        /// <param name="senderName"></param>
        /// <param name="recipientName"></param>
        /// <param name="recipientEmail"></param>
        /// <param name="message"></param>
        /// <param name="subject"></param>
        /// <param name="isHtml"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public bool SendEmail(string senderName, string recipientName, string recipientEmail, 
            string message, string subject, bool isHtml, params string[] attachments)
        {
            return (SmtpHelper.Send(message, subject, recipientEmail, recipientName, User, Sender, User, Password, Host, Port, SSL, isHtml, attachments));
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
            return (Send(message, subject, recipient, recipient, recipient, recipient, userName, password, host, port, ssl, false));
        }

        internal static bool Send(string message, string subject, 
            string recipientEmail, string recipientName,
            string senderEmail, string senderName,
            string userName, string password, string host, int port, bool ssl,
            bool isHtml,
            params string[] attachments)
        {
            SmtpClient smtpClient = new SmtpClient(host);
            try
            {
                smtpClient.Port = port;
                smtpClient.EnableSsl = ssl;

                smtpClient.Credentials = new NetworkCredential(userName, password);
                try
                {
                    MailMessage msg = new MailMessage(new MailAddress(senderEmail, senderName), new MailAddress(recipientEmail, recipientName));
                    try
                    {
                        msg.Subject = subject;
                        msg.Body = message;
                        msg.IsBodyHtml = isHtml;

                        foreach (string file in attachments)
                        {
                            if (String.IsNullOrEmpty(file) || !File.Exists(file))
                                continue;

                            System.Net.Mime.ContentType contentType = new System.Net.Mime.ContentType();
                            contentType.MediaType = System.Net.Mime.MediaTypeNames.Application.Octet;
                            contentType.Name = Path.GetFileName(file);
                            msg.Attachments.Add(new Attachment(file, contentType));
                        }

                        smtpClient.Send(msg);
                        return (true);
                    }
                    finally
                    {
                        msg.Dispose();
                        msg = null;
                    }
                }
                catch
                {
                    return (false);
                }
            }
            finally
            {
                smtpClient.Dispose();
                smtpClient = null;
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
