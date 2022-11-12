using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using Shared.Classes;

namespace Shared.Communication
{
    public sealed class WebSocket
    {
        #region Private Members

        private readonly Socket _socket;
        private readonly IPEndPoint _endPoint;

        #endregion Private Members

        #region Constructors

        private WebSocket()
        {
            UrlEncoding = Encoding.UTF8;
            UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727) SmokeTest/v1.0";
            MaxRedirects = 4;
            Cookies = new NVPCodec();
            Headers = new NVPCodec();
        }

        public WebSocket(IPAddress host, int port)
            : this()
        {
            _endPoint = new IPEndPoint(host, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.LingerState = new LingerOption(true, 60);
            _socket.NoDelay = true;
            _socket.ReceiveTimeout = 30000;
        }

        #endregion Constructors

        #region Properties

        public NVPCodec Headers { get; private set; }

        public NVPCodec Cookies { get; private set; }

        public Encoding UrlEncoding { get; set; }

        public string UserAgent { get; set; }

        public int MaxRedirects { get; set; }

        #endregion Properties

        #region Public Methods

        public void Connect()
        {
            if (!_socket.Connected)
                _socket.Connect(_endPoint);
        }

        public void Disconnect()
        {
            if (_socket.Connected)
                _socket.Disconnect(true);
        }

        public void Close()
        {
            _socket.Close();

        }

        public string Get(Uri url)
        {
            return Get(url, 0);
        }

        #endregion Public Methods

        #region Private Methods

        private void AddCookiesToCookieContainer(in Uri url, in string[] headers)
        {
            foreach (string s in headers)
            {
                if (s.StartsWith("Set-Cookie", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] cookie = s.Substring(12).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    string cookieName = cookie[0].Split('=')[0];
                    NVPCodec cookieNvp = new NVPCodec();
                    cookieNvp.Decode(String.Join("&", cookie));

                    Cookies.Add(cookieName, Convert.ToBase64String(UrlEncoding.GetBytes(cookieNvp[cookieName])));
                }
            }
        }

        private string GetCookies()
        {
            if (Cookies.Count == 0)
                return String.Empty;

            StringBuilder Result = new StringBuilder("$Version=\"1\";\r\n", 500 * Cookies.Count);

            foreach (string cookie in Cookies.AllKeys)
            {
                Result.Append(cookie);
                Result.Append('=');
                Result.Append('\"');
                Result.Append(UrlEncoding.GetString(Convert.FromBase64String(Cookies[cookie])));
                Result.Append('\"');
                Result.Append("; $Path=\"/\";\r\n");
            }

            return Result.ToString();
        }

        private string Get(Uri url, int redirectAttempt)
        {
            if (redirectAttempt > MaxRedirects)
                throw new Exception("too many redirects");

            string host = url.Host;
            string accept = "text/html";

            string Request = String.Format("GET {0} HTTP/1.1\r\nHost: {1}\r\nConnection: keep-alive\r\nAccept: {2}\r\nUser-Agent: {3}{4}\r\n\r\n",
                url.AbsolutePath, host, accept, UserAgent, GetCookies());

            _socket.Send(UrlEncoding.GetBytes(Request));

            StringBuilder Result = new StringBuilder(_socket.ReceiveBufferSize);
            StringBuilder headerString = new StringBuilder(1024);

            int contentLength = 0;
            byte[] bodyBuff = new byte[0];

            while (true)
            {
                // read the header byte by byte, until \r\n\r\n
                byte[] buffer = new byte[1];
                _socket.Receive(buffer, 0, 1, 0);
                headerString.Append(UrlEncoding.GetString(buffer));

                if (headerString.Length > 4 &&
                    headerString[headerString.Length - 4] == '\r' &&
                    headerString[headerString.Length - 3] == '\n' &&
                    headerString[headerString.Length - 2] == '\r' &&
                    headerString[headerString.Length - 1] == '\n')
                {
                    headerString.Replace("\r\n", "\n");
                    string[] headers = headerString.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] responseBuffer = new char[3];
                    headerString.CopyTo(9, responseBuffer, 0, 3);

                    if (!Int32.TryParse(new string(responseBuffer), out int response))
                        throw new InvalidCastException("Failed to retrieve response code");

                    AddCookiesToCookieContainer(url, headers);

                    switch (response)
                    {
                        case 307:
                            return Get(url, redirectAttempt + 1);
                    }

                    Regex reg = new Regex("\\\r\nContent-Length: (.*?)\\\r\n");
                    Match m = reg.Match(headerString.ToString());
                    contentLength = int.Parse(m.Groups[1].ToString());

                    if (contentLength > 0)
                    {
                        // read the body
                        bodyBuff = new byte[contentLength];
                        _socket.Receive(bodyBuff, 0, contentLength, 0);

                        Result.Append(UrlEncoding.GetString(bodyBuff));
                    }

                    break;
                }
            }

            return Result.ToString();
        }

        #endregion Private Methods
    }
}
