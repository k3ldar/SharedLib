/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Pop 3 client Class
 *
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#if NET5

namespace Shared.Communication
{
    public sealed class Pop3Client : IPop3Client
    {
        private static readonly StringSplitOptions TrimOptions = StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries;

        private const int TimeOutMilliseconds = 1000 * 300;

        private bool _disposed;
        TcpClient _tcpClient = null;
        private SslStream _streamReader = null;
        private StreamReader _reader;
        private StreamWriter _writer;
        private string _uri;
        private string _userName;
        private string _password;
        private ushort _port;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public void Dispose()
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                WriteLine("QUIT", out string quitResponse);
                _tcpClient.Close();
                _tcpClient.Dispose();
            }

            _streamReader?.Dispose();

            _disposed = true;
        }

        public void Initialize(string uri, string userName, string password, ushort port)
        {
            if (_disposed)
                throw new ObjectDisposedException(this.ToString());

            if (_tcpClient != null)
                return;

            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException(nameof(uri));

            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            if (String.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            _uri = uri;
            _userName = userName;
            _password = password;
            _port = port;

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _tcpClient = new TcpClient();

            if (!Connect())
                _disposed = true;
        }

        public int GetMailCount(out int sizeInOctets)
        {
            sizeInOctets = 0;
            bool result = WriteLine("LIST", out string countResponse);

            if (!result)
                return -1;

            string[] parts = countResponse.Split(new char[] { ' ' }, TrimOptions);

            if (!Int32.TryParse(parts[1], out int mailCount))
                return -1;

            if (parts.Length > 1 && !Int32.TryParse(parts[2], out sizeInOctets))
            {
                sizeInOctets = -1;
            }

            string line;

            do
            {
                line = _reader.ReadLine();

                if (line.Equals("."))
                    break;
            } while (line != null);

            return mailCount;
        }


        public string RetrieveMessage(int messageNumber, out string readResponse)
        {
            ValidateConnection(true);

            _writer.WriteLine($"RETR {messageNumber}");
            _writer.Flush();

            readResponse = _reader.ReadLine();

            if (String.IsNullOrEmpty(readResponse) || (readResponse.Length > 0 && readResponse[0] == '-'))
                return null;

            string line;

            StringBuilder result = new();

            while ((line = _reader.ReadLine()) != null)
            {
                result.AppendLine(line);

                if (line.Equals("."))
                    break;
            }

            return result.ToString();
        }

        public string DeleteMessage(int messageNumber)
        {
            ValidateConnection(false);

            WriteLine($"DELE {messageNumber}", out string response);

            return response;
        }

        #region Private Methods

        private bool Connect()
        {
            if (IsConnected)
                return true;

            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect(_uri, _port);
            }

            _streamReader = new(_tcpClient.GetStream(), true, new RemoteCertificateValidationCallback(ValidateServerCertificate))
            {
                ReadTimeout = TimeOutMilliseconds,
                WriteTimeout = TimeOutMilliseconds
            };

            _streamReader.AuthenticateAsClient(_uri);
            _reader = new StreamReader(_streamReader);
            _writer = new StreamWriter(_streamReader);

            string connectionMsg = ReadLine();

            if (!Login())
                return false;

            return _tcpClient.Connected;
        }

        public static bool ValidateServerCertificate(
          object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // refuse connection
            return false;
        }

        private string ReadLine()
        {
            ValidateConnection(true);

            return _reader.ReadLine();
        }

        private bool WriteLine(string data, out string response)
        {
            ValidateConnection(false);

            _writer.WriteLine(data);
            _writer.Flush();

            response = ReadLine();

            if (!String.IsNullOrEmpty(response) && response.Length > 0)
                return response[..3].Equals("+OK", StringComparison.InvariantCultureIgnoreCase);

            return false;
        }

        private bool Login()
        {
            if (WriteLine($"USER {_userName}", out _))
                if (WriteLine($"PASS {_password}", out _))
                    return true;

            return false;
        }

        [DebuggerHidden]
        private void ValidateConnection(bool isRead)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Pop3Client));

            if (!IsConnected)
                throw new InvalidOperationException("No Connection");

            if (isRead && !_streamReader.CanRead)
                throw new InvalidOperationException("Stream can not be read");

            if (!isRead && !_streamReader.CanWrite)
                throw new InvalidOperationException("Stream can not be written to");
        }

        #endregion Privte Methods
    }
}

#endif