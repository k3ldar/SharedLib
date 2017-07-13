/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  TCP/IP Message Client
 *
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.IO;

using Shared.Classes;

namespace Shared.Communication
{
    /// <summary>
    /// Client object used for receiving messages
    /// </summary>
    public class MessageClient
    {
        #region Private / Internal Membes

        private int _port;
        private string _server;
        private bool _running;
        private string _clientID;
        private bool _ignoreBroadcasts;
        private bool _timeout;
        private FileBufferSize _bufferSize = FileBufferSize.Size4096;
        private bool _loggedIn = false;

        internal TcpClient _tcpClient;

        private object _messageLockObject = new object();

        #endregion Private / Internal Members

        #region Constructors

        /// <summary>
        /// Constructor, used for localhost only
        /// </summary>
        /// <param name="port">Port to connect to</param>
        public MessageClient(int port)
        {
            _port = port;
            _server = "localhost";
            _running = false;
            _ignoreBroadcasts = false;
            _bufferSize = FileBufferSize.Size4096;
        }

        /// <summary>
        /// Constructor, specify server ip address/name and port
        /// </summary>
        /// <param name="server">Server Name or IP Address</param>
        /// <param name="port">Port to connect to</param>
        public MessageClient(string server, int port)
        {
            this._port = port;
            _server = server;
            _running = false;
            _bufferSize = FileBufferSize.Size4096;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Initialises the client and starts listening for messages from the server
        /// </summary>
        public void StartListening()
        {
#if DEBUG
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
                _timeout = false;

                using (TimedLock.Lock(_messageLockObject))
                {
                    string threadName = String.Format("Client Listening Thread {0}", _server);

                    if (ThreadManager.Exists(threadName))
                        return;

                    _tcpClient = new TcpClient(_server, _port);
                    _tcpClient.SendTimeout = 30000;
                    _tcpClient.ReceiveTimeout = 30000;
                    MessageClientListeningThread newListeningThread = new MessageClientListeningThread(this);
                    newListeningThread.MessageReceived += newListeningThread_MessageReceived;
                    ThreadManager.ThreadStart(newListeningThread, threadName, ThreadPriority.Lowest);
                }

                RaiseConnected();
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                if (err.Message.Contains("A connection attempt failed because the connected party did not properly respond") ||
                    err.Message.Contains("No connection could be made because the target machine actively refused it"))
                {
                    RaiseConnectionRefused();
                }
                else if (!HandleClientException(err))
                {
                    EventLog.Add(err, String.Format("Server: {0}; Port: {1}", _server, _port));
                    throw;
                }
            }
        }

        void newListeningThread_MessageReceived(object sender, Message message)
        {
#if DEBUG
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if (MessageReceived != null)
                MessageReceived(this, message);
        }

        /// <summary>
        /// Disconnects and stops listening for messages
        /// </summary>
        public void StopListening()
        {
#if DEBUG
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            using (TimedLock.Lock(_messageLockObject))
            {
                if (_running)
                {
                    if (IsConnected)
                        sendMessage(new Message("CLOSING", "", MessageType.Command));

                    RaiseDisconnected();
                    _tcpClient.Close();
                    
                    ThreadManager.Cancel(String.Format("Client Listening Thread {0}", _server));
                }

                _tcpClient = null;
            }
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public void SendMessage(Message message)
        {
#if DEBUG
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            EventLog.Debug(String.Format("{0} - {1} - {2} - {3}", 
                message.Type.ToString(), message.ClientID, message.Title, message.Contents));
#endif
            using (TimedLock.Lock(_messageLockObject))
            {
                if (_running)
                {
                    sendMessage(message);
                }
            }
        }

        /// <summary>
        /// Sends a file to the server
        /// </summary>
        /// <param name="FileName">File to be sent</param>
        public void SendFile(string FileName)
        {
            FileInfo f = new FileInfo(FileName);

            if (f.Length > 20971520) //20 mb limit
            {
                RaiseError(new Exception("File exceeds maximum file size"));
            }
            else
            {
                sendMessage(Message.FileToStringMessage(FileName));
                RaiseFileReceived(this, new TransferFileEventArgs(FileName));
            }
        }

        /// <summary>
        /// Instructs the server to send a file
        /// 
        /// Server must manage its own path system as only the file name will be sent
        /// </summary>
        /// <param name="FileName">Name of file to be sent by server</param>
        public void ReceiveFile(string FileName)
        {
            sendMessage(new Message("REQUEST_FILE", FileName, MessageType.File));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handles exceptions when handling client connections
        /// </summary>
        /// <param name="error">Exception raised</param>
        /// <returns>bool value</returns>
        internal bool HandleClientException(Exception error)
        {
#if DEBUG
            EventLog.Debug(error);
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            // connection is about to close
            if (_timeout)
            {
                RaiseDisconnected();
                return (true);
            }

            //if it doesn't respond in time to the request?
            if (error.Message.Contains("A connection attempt failed because the connected party did not properly respond " +
                "after a period of time, or established connection failed because connected host has failed to respond"))
            {
                return (true);
            }

            if (error.Message.Contains("does not contain a valid BinaryHeader") || 
                error.Message.Contains("The input stream is not a valid binary format."))
            {
                //we have sent/received junk, disconnect the client
                RaiseDisconnected();

                if (_tcpClient != null)
                    _tcpClient.Close();

                return (true);
            }

            if (error.Message.Contains("An established connection was aborted by the software in your host machine"))
            {
                RaiseDisconnected();
                return (true);
            }

            if (error.Message.Contains("An existing connection was forcibly closed by the remote host"))
            {
                RaiseDisconnected();
                return (true);
            }

            if (error.Message.Contains("End of Stream encountered before parsing was completed"))
            {
                RaiseDisconnected();
                return (true);
            }

            if (error.Message.Contains("The operation is not allowed on non-connected sockets."))
            {
                RaiseDisconnected();
                return (true);
            }

            if (error.Message.Contains("not access a disposed object"))
            {
                RaiseDisconnected();
                return (true);
            }

            //trying to send message when connection closed
            if (error.Message.Contains("Unable to read data from the transport connection") || 
                error.Message.Contains("無法從傳輸連接讀取資料:")) // occurred on windows in Taiwan!
            {
                if (_running)
                {
                    RaiseDisconnected();
                }

                return (true);
            }

            return (RaiseError(error));
        }

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// Set's a flag to indicate time out has occurred
        /// </summary>
        internal bool TimeOut
        {
            get
            {
                return (_timeout);
            }

            set
            {
                _timeout = value;
            }
        }

        /// <summary>
        /// Sends the message to the server
        /// </summary>
        /// <param name="message">Message to be sent</param>
        internal void sendMessage(Message message)
        {
#if DEBUG
            EventLog.Debug(String.Format("{0} - {1}", message.Title, message.Contents));
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
                if (_tcpClient != null && _tcpClient.Connected)
                {
                    message.SetClientID(_clientID);

                    if (String.IsNullOrEmpty(message.ClientID))
                        message.SetClientID("SERVER");


                    char[] characters = Message.MessageToStringArray(message);
                    byte[] toSend = System.Text.Encoding.UTF8.GetBytes(characters, 0, characters.Length);

                    _tcpClient.GetStream().Write(toSend, 0, toSend.Length);
                }
            }
            catch (Exception err)
            {
#if DEBUG
            EventLog.Debug(err);
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                if (!HandleClientException(err))
                    throw;
            }
        }

        #endregion Internal Methods

        #region Properties

        /// <summary>
        /// Determines whether broadcast messages are ignored or not
        /// </summary>
        public bool IgnoreBroadcasts
        {
            get
            {
                return (_ignoreBroadcasts);
            }

            set
            {
                _ignoreBroadcasts = value;
                SendMessage(new Message("IGNORE_BROADCAST", _ignoreBroadcasts.ToString(), MessageType.Command));
            }
        }

        /// <summary>
        /// Returns true if the client is running and connected to the server
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return (_running);
            }
        }

        /// <summary>
        /// Returns the unique Client ID as identified by the server
        /// </summary>
        public string ClientID
        {
            get
            {
                return (_clientID);
            }

            internal set
            {
                _clientID = value;
            }
        }

        /// <summary>
        /// Returns the server the client is connected to or ("Not Connected")
        /// </summary>
        public string Server
        {
            get
            {
                if (IsRunning)
                    return (_server);
                else
                    return ("Not Connected");
            }
        }

        /// <summary>
        /// Determines whether the client is connected to the server or not
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_tcpClient == null)
                    return (false);
                else
                    return (_tcpClient.Connected);
            }
        }

        /// <summary>
        /// Secifies the buffer size for sending/receiving files files
        /// </summary>
        public FileBufferSize BufferSize
        {
            get
            {
                return (_bufferSize);
            }

            set
            {
                _bufferSize = value;

                //update server with new buffer size
                sendMessage(new Message("SET_BUFFER_SIZE", Convert.ToString((UInt32)value), MessageType.Command));
            }
        }

        /// <summary>
        /// Indicates whether the client is logged into the server or not
        /// </summary>
        public bool LoggedIn
        {
            get
            {
                return (_loggedIn);
            }
        }

        #endregion Properties

        #region events

        #region Private Methods

        /// <summary>
        /// Raises client connected event
        /// </summary>
        internal void RaiseConnected()
        {
            _running = true;

            if (Connected != null)
                Connected(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises client disconnected event
        /// </summary>
        internal void RaiseDisconnected()
        {
            _running = false;

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises client ID changed event
        /// </summary>
        internal void RaiseClientIDChanged()
        {
            if (ClientIDChanged != null)
                ClientIDChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises Internal error event handler
        /// </summary>
        /// <param name="error">Error that occured</param>
        /// <returns>bool true to continue false to stop the server</returns>
        internal bool RaiseError(Exception error)
        {
            ErrorEventArgs args = new ErrorEventArgs(error);

            if (OnError != null)
                OnError(this, args);

            return (args.Continue);
        }

        /// <summary>
        /// Raise LoginRequired Event
        /// </summary>
        /// <param name="passwordCode">Random encryption password created by the server</param>
        /// <returns></returns>
        internal string RaiseLoginRequired(string passwordCode)
        {
            string Result = "$";

            ClientLoginArgs args = new ClientLoginArgs();

            if (ClientLogin != null)
            {
                ClientLogin(this, args);

                if (args.Username != null || args.Password != null)
                    Result = String.Format("{0}${1}", args.Username, StringCipher.Encrypt(args.Password, passwordCode));
            }

            return (Result);
        }

        /// <summary>
        /// Raises login failed event
        /// </summary>
        internal void RaiseLoginFailed()
        {
            if (ClientLoginFailed != null)
                ClientLoginFailed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises login success event
        /// </summary>
        internal void RaiseLoggedIn()
        {
            //show that we are logged in
            _loggedIn = true;

            if (ClientLoginSuccess != null)
                ClientLoginSuccess(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises file received event
        /// 
        /// Used to monitor progres of file being sent/received
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">ReceiveFileEventArgs event arguments</param>
        internal void RaiseFileReceived(object sender, TransferFileEventArgs e)
        {
            if (FileReceived != null)
                FileReceived(this, e);
        }

        /// <summary>
        /// Raises connection refused event
        /// </summary>
        internal void RaiseConnectionRefused()
        {
            if (ConnectionRefused != null)
                ConnectionRefused(this, EventArgs.Empty);
        }

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// Raises a FileReceive event
        /// </summary>
        /// <param name="FileName">Filename being received</param>
        /// <returns>Client should return a fully qualified path/file name of the file to be returned</returns>
        internal string RaiseFileReceive(string FileName)
        {
            TransferFileEventArgs args = new TransferFileEventArgs(FileName);

            if (FileReceive != null)
                FileReceive(this, args);

            return (args.FileName);
        }

        #endregion Internal Methods

        /// <summary>
        /// Event raised when a message is received
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// Event raised when client id is changed
        /// </summary>
        public event EventHandler ClientIDChanged;

        /// <summary>
        /// Event raised when a connection is successfully made
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Event raised when a connection disconnects
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Exception raised when an error occurs
        /// </summary>
        public event ErrorEventHandler OnError;

        /// <summary>
        /// Exception raised when a client logs in
        /// </summary>
        public event ClientLoginHandler ClientLogin;

        /// <summary>
        /// Event raised when a client login fails
        /// </summary>
        public event EventHandler ClientLoginFailed;

        /// <summary>
        /// Event raised when a client succesfully logs in
        /// </summary>
        public event EventHandler ClientLoginSuccess;

        /// <summary>
        /// Event raised when a file is about to be received
        /// </summary>
        public event FileReceivedHandler FileReceive;

        /// <summary>
        /// Event raised when a file has been received
        /// </summary>
        public event FileReceivedHandler FileReceived;

        /// <summary>
        /// Event raised when a client connection is refused
        /// </summary>
        public event EventHandler ConnectionRefused;

        #endregion events
    }

    /// <summary>
    /// Thread used to listen for client connections
    /// </summary>
    internal class MessageClientListeningThread : ThreadManager
    {
        #region Private Members

        private StringBuilder _completeMessage;
        private MessageClient _parentMessageClient;

        #endregion Private Members

        internal MessageClientListeningThread(MessageClient parent)
            : base (parent, new TimeSpan())
        {
            HangTimeout = 0;
            _parentMessageClient = parent;
            ContinueIfGlobalException = false;
            _completeMessage = new StringBuilder();
        }

        protected override bool Run(object parameters)
        {
#if DEBUG
            EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif

            byte brokenMessageCount = 0;

            try
            {
                while (_parentMessageClient.IsRunning)
                {
                    // Block until an instance Message is received
                    byte[] bytes = new byte[(int)_parentMessageClient.BufferSize];

                    // Read can return anything from 0 to numBytesToRead. 
                    // This method blocks until at least one byte is read.

                    do
                    {
                        int bytesRead = _parentMessageClient._tcpClient.GetStream().Read(bytes, 0, bytes.Length);

                        _completeMessage.AppendFormat("{0}", Encoding.UTF8.GetString(bytes, 0, bytesRead));

                    } while (_parentMessageClient._tcpClient.GetStream().DataAvailable);

                    //if we haven't got the complete message, keep going until it's here
                    if (!_completeMessage.ToString().EndsWith("#END#") && brokenMessageCount < 100)
                    {
                        brokenMessageCount++;
                        continue;
                    }

                    brokenMessageCount = 0;

                    string[] messages = _completeMessage.ToString().Split(new string[] { "#END#" }, StringSplitOptions.None);

                    _completeMessage.Clear();

                    foreach (string msg in messages)
                    {
                        if (String.IsNullOrEmpty(msg.Trim()))
                            continue;

                        Message message = Message.StringToMessage(msg);

#if DEBUG
                        EventLog.Debug("MessageClient.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif


                        //have we stopped running?
                        if (!_parentMessageClient.IsRunning)
                            return (false);

                        if (MessageReceived != null && message != null)
                        {
                            switch (message.Type)
                            {
                                case MessageType.Command:
                                    //some command messages are handled internally
                                    switch (message.Title)
                                    {
                                        case "NEWCLIENTID":
                                            _parentMessageClient.ClientID = message.Contents;
                                            _parentMessageClient.RaiseClientIDChanged();
                                            break;
                                        case "SERVERSTOPPING":
                                            _parentMessageClient.StopListening();
                                            break;
                                        case "LOGIN_REQUIRED":
                                            string loginDetails = _parentMessageClient.RaiseLoginRequired(message.Contents);

                                            _parentMessageClient.sendMessage(new Message("LOGIN", loginDetails, MessageType.Command));
                                            break;
                                        case "LOGIN_FAILED":
                                            _parentMessageClient.StopListening();
                                            _parentMessageClient.RaiseLoginFailed();
                                            break;
                                        case "LOGGED_IN":
                                            _parentMessageClient.RaiseLoggedIn();
                                            break;
                                        case "TIMEOUT":
                                            _parentMessageClient.TimeOut = true;
                                            break;
                                        case "CONNECTION_REFUSED":
                                            _parentMessageClient.RaiseConnectionRefused();
                                            break;
                                        default: //not an internal message process as normal
                                            message.SetClientID(_parentMessageClient.ClientID);
                                            MessageReceived(this, message);
                                            break;
                                    }

                                    break;
                                case MessageType.File:
                                    string newFileName = _parentMessageClient.RaiseFileReceive(message.Title);
                                    //FileTransfer transfer = new FileTransfer();
                                    //transfer.FileReceived += RaiseFileReceived;
                                    //transfer.ProcessClientFiles(this, message);

                                    byte[] rebin = Convert.FromBase64String(message.Contents);
                                    using (FileStream fs = new FileStream(newFileName, FileMode.Create))
                                    {
                                        using (BinaryWriter bw = new BinaryWriter(fs))
                                            bw.Write(rebin);
                                    }

                                    _parentMessageClient.RaiseFileReceived(this, new TransferFileEventArgs(newFileName));
                                    MessageReceived(this, message);

                                    continue;
                                default:

                                    // any other message forward onto event handler
                                    message.SetClientID(_parentMessageClient.ClientID);
                                    MessageReceived(this, message);
                                    break;
                            }
                        }
                    }
                }
            }
            catch 
#if DEBUG 
                (ObjectDisposedException errDisposed)
#else
                (ObjectDisposedException)
#endif
            {
#if DEBUG
                EventLog.Debug(errDisposed);
                EventLog.Debug("MessageClient.cs ObjectDisposedException " + 
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                return (false);
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageClient.cs Exception " + 
                    System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                if (!_parentMessageClient.HandleClientException(err))
                    throw;
            }

            return (!HasCancelled());
        }

        public override void Abort()
        {
            if (_parentMessageClient != null)
                _parentMessageClient.sendMessage(new Message("CLOSING", "", MessageType.Command));
        }

        #region Events

        internal event MessageReceivedEventHandler MessageReceived;

        #endregion Events
    }
}
