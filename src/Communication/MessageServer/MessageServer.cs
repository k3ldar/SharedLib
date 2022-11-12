/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  TCP/IP Message Server
 *
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shared.Classes;

#pragma warning disable IDE1005 // Delegate invocation can be simplified
#pragma warning disable IDE1006 // naming rule violation
#pragma warning disable IDE0017 // initialization can be simplified

namespace Shared.Communication
{
    /// <summary>
    /// TCP Server object
    /// </summary>
    public class MessageServer
    {
        #region Private Members

        private string _threadName;

        internal Dictionary<string, ConnectedClient> _clientsDictionary;
        internal ushort _maxClientConnections;

        private bool _running = false;
        private bool _loginRequired;
        private ushort _clientTimeout = 0;
        private readonly int _port;

        internal object _messageLockObject = new object();

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Create a message server that listens on the indicated port
        /// </summary>
        /// <param name="port">Port to connect to</param>
        public MessageServer(int port)
        {
            _port = port;
            _clientsDictionary = new Dictionary<string, ConnectedClient>();
            _loginRequired = false;
            _maxClientConnections = 0;
            MaximumFileSize = 5242880; //5MB default limit
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Sends a message to all connected clients (broadcast)
        /// </summary>
        /// <param name="message">Message to be sent</param>
        /// <param name="allClients">Indicates that all clients will receive the message</param>
        public void SendMessage(Message message, bool allClients = true)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            EventLog.Debug(String.Format("{0} - {1} - {2} - {3} - {4}", allClients.ToString(),
                message.Type.ToString(), message.ClientID, message.Title, message.Contents));
#endif
            string activeClientID = "";
            ConnectedClient activeClient = null;
            try
            {
                //copy the clients dictionary in case its modified whilst iterating
                Dictionary<string, ConnectedClient> clients = new Dictionary<string, ConnectedClient>(_clientsDictionary);

                foreach (KeyValuePair<string, ConnectedClient> entry in clients)
                {
                    activeClientID = entry.Key;
                    activeClient = entry.Value;

                    // If client had connected to the message server
                    if (allClients || (!allClients && clients.ContainsKey(activeClientID) && activeClientID == message.ClientID))
                    {
                        try
                        {
                            // send message to client
                            if (activeClient != null && activeClient.Client.Connected)
                            {
                                // does the client need to login?
                                if (!_loginRequired || (_loginRequired && clients[activeClientID].LoggedIn))
                                {
                                    // is the client ignoring broadcast messages
                                    if (message.Type != MessageType.Broadcast ||
                                        (!clients[activeClientID].IgnoreBroadcastMessages &&
                                        message.Type == MessageType.Broadcast))
                                    {
                                        sendMessage(activeClientID, clients[activeClientID].Client, message);
                                    }
                                }
                            }
                        }
                        catch
#if DEBUG
                            (Exception err)
#endif
                        {
#if DEBUG
                            EventLog.Debug(err);
                            EventLog.Debug("MessageServer.cs Exception1 " +
                                System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                            RaiseClientDisconnected(ClientAddress(activeClient.Client), activeClientID);
                            message.Type = MessageType.Command;
                            message.Title = "CLOSING";
                            message.SetClientID(activeClientID);
                            bool shouldExit = false;
                            ProcessCommand(message, ref shouldExit);
                        }
                    }
                }

                clients = null;
                activeClient = null;
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageServer.cs Exception 2 " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                if (!HandleClientException(err, activeClientID, activeClient.Client))
                    throw;
            }
        }

        /// <summary>
        /// Starts a new Message Server listening thread, with a specific name
        /// </summary>
        /// <param name="threadName"></param>
        public void Start(string threadName)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if (!_running)
            {
                RaiseStarted();

                _threadName = threadName;

                if (String.IsNullOrEmpty(_threadName))
                    _threadName = String.Format("MessageServer Connection Thread - Port {0}", _port);

                ThreadManager.ThreadStart(new MessageServerClientConnectionThread(this, _port),
                    _threadName, ThreadPriority.Normal);

                // if it's not already running, launch the maintenance thread
                if (!ThreadManager.Exists("MessageServer Maintenance Thread"))
                {
                    ThreadManager.ThreadStart(new MessageServerMaintenance(this),
                        "MessageServer Maintenance Thread", ThreadPriority.Lowest);
                }
            }
        }

        /// <summary>
        /// Starts a new Message Server listening thread
        /// </summary>
        public void Start()
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            Start("MessageServer Connection Thread");
        }

        /// <summary>
        /// Stops the Server
        /// </summary>
        public void Stop()
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if (_running)
            {
                SendMessage(Message.Command("SERVERSTOPPING"));
                RaiseStopped();

                MessageServerClientConnectionThread currThread = (MessageServerClientConnectionThread)ThreadManager.Find(_threadName);

                if (currThread != null)
                {
                    ThreadManager.Cancel(currThread.Name);
                    currThread.FalseConnect();
                }

                using (TimedLock.Lock(_messageLockObject))
                {
                    _clientsDictionary.Clear(); //clear all connections
                }
            }
        }

        /// <summary>
        /// Returns the logged in user name for a Client
        /// </summary>
        /// <param name="ClientID">Client ID of user logged in</param>
        /// <returns>Username of logged in client</returns>
        public string UserName(string ClientID)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            using (TimedLock.Lock(_messageLockObject))
            {
                return _clientsDictionary[ClientID].LoginName;
            }
        }

        /// <summary>
        /// retrieves user defined data for the client connection
        /// </summary>
        /// <param name="ClientID">Client ID</param>
        /// <returns>User defined object to be returned</returns>
        public object UserData(string ClientID)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            using (TimedLock.Lock(_messageLockObject))
            {
                return _clientsDictionary[ClientID].UserData;
            }
        }

        /// <summary>
        /// Sets user defined data for the client connection
        /// </summary>
        /// <param name="ClientID">Client ID</param>
        /// <param name="Data">Data to be saved with client connection</param>
        public void UserData(string ClientID, object Data)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            using (TimedLock.Lock(_messageLockObject))
            {
                _clientsDictionary[ClientID].UserData = Data;
            }
        }

        #endregion Public Methods

        #region Properties

        /// <summary>
        /// Indicates wether the server is running or not
        /// </summary>
        public bool Running
        {
            get
            {
                return _running;
            }

            internal set
            {
                _running = value;
            }
        }

        /// <summary>
        /// Indicates wether clients need to login
        /// </summary>
        public bool LoginRequird
        {
            get
            {
                return _loginRequired;
            }

            set
            {
                _loginRequired = value;
            }
        }

        /// <summary>
        /// Maximum number of Client Connections
        /// </summary>
        public ushort MaxClientConnections
        {
            get
            {
                return _maxClientConnections;
            }

            set
            {
                _maxClientConnections = value;
            }
        }

        /// <summary>
        /// Determintes the number of seconds the client received/sent data before timeout (0 = no limit)
        /// </summary>
        public ushort ClientTimeOut
        {
            get
            {
                return _clientTimeout;
            }

            set
            {
                _clientTimeout = value;
            }
        }

        /// <summary>
        /// Determines wether the server accepts files or not
        /// </summary>
        public bool AcceptFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum file size in bytes that the server will accept
        /// </summary>
        public ulong MaximumFileSize
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the client dictionary
        /// </summary>
        internal Dictionary<string, ConnectedClient> ClientDictionary
        {
            get
            {
                return _clientsDictionary;
            }
        }

        /// <summary>
        /// Retrieves the port being used by Message Server
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
        }

        #endregion Properties

        #region Internal Methods

        /// <summary>
        /// Sends the actual message to individual client
        /// </summary>
        /// <param name="clientID">Client ID where message is sent to</param>
        /// <param name="client">client message to be sent to</param>
        /// <param name="message">message to be sent</param>
        internal void sendMessage(string clientID, TcpClient client, Message message)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            EventLog.Add(String.Format("{0} - {1} - {2} - {3} - {4}", clientID,
                message.Type.ToString(), message.ClientID, message.Title, message.Contents));
#endif

            if (client.Connected)
            {
                try
                {
                    if (!_clientsDictionary.ContainsKey(clientID))
                        return;

                    int bufferSize = (int)_clientsDictionary[clientID].BufferSize;

                    if (String.IsNullOrEmpty(message.ClientID))
                        message.SetClientID("SERVER");

                    char[] characters = Message.MessageToStringArray(message);
                    byte[] toSend = System.Text.Encoding.UTF8.GetBytes(characters, 0, characters.Length);

                    client.GetStream().Write(toSend, 0, toSend.Length);

                    //update last sent time
                    using (TimedLock.Lock(_messageLockObject))
                    {
                        if (_clientsDictionary.ContainsKey(clientID))
                            _clientsDictionary[clientID].LastSent = DateTime.Now;
                    }
                }
                catch (Exception err)
                {
                    if (err.Message.Contains("Unable to write data to the transport connection") ||
                        err.Message.Contains("無法寫入資料至傳輸連接")) // windows Taiwan locale
                    {
                        ThreadManager.Cancel(String.Format("Client Connection - {0} {1}",
                            clientID, client.Client.RemoteEndPoint.ToString()));
                        client.Close();
                    }
                    else
                    {
                        EventLog.Add(err);
                        throw;
                    }

#if DEBUG
                    EventLog.Debug(err);
                    EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                    //throw;
                }
            }
        }

        /// <summary>
        /// Sends a message to an individual client based on logged on username
        /// </summary>
        /// <param name="message">Message to be sent to use</param>
        /// <param name="user">Name of user message is to be sent to</param>
        /// <param name="ignoreCase">Inidcates wether the case of the username should be </param>
        internal void sendMessage(Message message, string user, bool ignoreCase = true)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            EventLog.Add(String.Format("{0} - {1} - {2} - {3} - {4}", "true",
                message.Type.ToString(), message.ClientID, message.Title, message.Contents));
#endif
            int sendCount = 0;

            //copy the clients dictionary in case its modified whilst iterating
            Dictionary<string, ConnectedClient> clients = new Dictionary<string, ConnectedClient>(_clientsDictionary);
            try
            {
                foreach (KeyValuePair<string, ConnectedClient> entry in clients)
                {
                    ConnectedClient connClient = null;

                    connClient = (ConnectedClient)entry.Value;

                    if ((ignoreCase && (connClient.LoginName.ToLower() == user.ToLower())) || (connClient.LoginName == user))
                    {
                        sendMessage(connClient.ClientID, _clientsDictionary[connClient.ClientID].Client, message);
                        sendCount++;
                    }
                }

                if (sendCount == 0)
                    sendMessage(message.ClientID, _clientsDictionary[message.ClientID].Client, new Message(
                        "USER_NOT_FOUND", user, MessageType.Error));
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("An existing connection was forcibly closed by the remote host"))
                {
                    EventLog.Add(err);
                }

#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            }
        }

        /// <summary>
        /// Determines wether the server is running or not
        /// </summary>
        /// <returns></returns>
        internal bool IsRunning()
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            return _running;
        }

        /// <summary>
        /// Processes internal command messages from clients
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <param name="exit">if true client thread should close</param>
        /// <returns>true if execution can continue, otherwise false (we handled it)</returns>
        internal bool ProcessCommand(Message message, ref bool exit)
        {
            exit = false;
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            bool Result = true;

            if (String.IsNullOrEmpty(message.ClientID) || !_clientsDictionary.ContainsKey(message.ClientID))
                return false;

            TcpClient client = _clientsDictionary[message.ClientID].Client;


            if (String.IsNullOrEmpty(message.ClientID))
                return false;

            switch (message.Title)
            {
                case "CLOSING":  //Client is about to disconnect
                    Result = false;
                    exit = true;

                    //if key not found then stop
                    if (!_clientsDictionary.ContainsKey(message.ClientID))
                        return Result;

                    RaiseClientDisconnected(ClientAddress(client), message.ClientID);

                    break;
                case "LOGIN":  // Client is about to login
                    Result = false;
                    string[] loginDetails = message.Contents.Split('$');

                    if (loginDetails[0].Length == 0 || loginDetails[1].Length == 0 ||
                        !RaiseOnLogin(ClientAddress(client), loginDetails[0], StringCipher.Decrypt(loginDetails[1],
                        _clientsDictionary[message.ClientID].RandomPassword)))
                    {
                        sendMessage(message.ClientID, client, Message.Command("LOGIN_FAILED"));
                        RaiseClientDisconnected(ClientAddress(client), message.ClientID);
                    }
                    else
                    {
                        _clientsDictionary[message.ClientID].LoggedIn = true;
                        _clientsDictionary[message.ClientID].LoginName = loginDetails[0];
                        sendMessage(message.ClientID, client, Message.Command("LOGGED_IN"));
                    }

                    break;
                case "IGNORE_BROADCAST":
                    _clientsDictionary[message.ClientID].IgnoreBroadcastMessages = Convert.ToBoolean(message.Contents);
                    break;
                case "SET_BUFFER_SIZE":
                    _clientsDictionary[message.ClientID].BufferSize = (FileBufferSize)Convert.ToUInt32(message.Contents);
                    break;
                default:
                    RaiseClientCommand(message);
                    break;
            }

            return Result;
        }

        internal void ProcessFileRequests(Message message)
        {
            string newFileName = String.Empty;

            switch (message.Title)
            {
                case "REQUEST_FILE":
                    newFileName = RaiseFileReceive(message.Contents, message.ClientID);
                    FileInfo fi = new FileInfo(newFileName);

                    Message fileMessage = Message.FileToStringMessage(newFileName);
                    sendMessage(message.ClientID, _clientsDictionary[message.ClientID].Client, fileMessage);
                    break;
                default:
                    //File messages are handled internally, process and continue
                    try
                    {
                        newFileName = RaiseFileReceive(message.Title, message.ClientID);

                        byte[] rebin = Convert.FromBase64String(message.Contents);
                        using (FileStream fs = new FileStream(newFileName, FileMode.Create))
                        {
                            using (BinaryWriter bw = new BinaryWriter(fs))
                                bw.Write(rebin);
                        }

                        RaiseFileReceived(this, new TransferFileEventArgs(newFileName,
                            (ulong)message.Contents.Length, (ulong)message.Contents.Length,
                            new TimeSpan(), 0.0, message.ClientID));
                        MessageReceived(this, message);
                        message.Contents = String.Empty;
                        message.Type = MessageType.Acknowledge;
                        sendMessage(message.ClientID, _clientsDictionary[message.ClientID].Client, message);
                    }
                    catch (Exception err)
                    {
                        HandleClientException(err, message.ClientID, _clientsDictionary[message.ClientID].Client);
                        sendMessage(message.ClientID, _clientsDictionary[message.ClientID].Client,
                            new Message(err.Message, err.StackTrace.ToString(), MessageType.Error));
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles exceptions when handling client connections
        /// </summary>
        /// <param name="error">Exception raised</param>
        /// <param name="clientID">clientID when error raised</param>
        /// <param name="client">TCP Client for client that has caused/raised an exception</param>
        /// <returns>bool, true if exception handled, otherwise false</returns>
        internal bool HandleClientException(Exception error, string clientID, TcpClient client)
        {
#if DEBUG
            EventLog.Debug(error);
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            bool Result = true;

            try
            {
                if (!client.Connected)
                {
                    RaiseClientDisconnected(_clientsDictionary[clientID].ClientIP, clientID);
                    return Result;
                }


                //if it doesn't respond in time to the request?
                if (error.Message.Contains("A connection attempt failed because the connected party did not properly " +
                    "respond after a period of time, or established connection failed because connected host has failed to respond"))
                {
                    return true;
                }

                if (error.Message == "End of Stream encountered before parsing was completed.")
                {
                    //client already disconnected
                    RaiseClientDisconnected(ClientAddress(client), clientID);
                }
                else
                    if (error.Message.Contains("An existing connection was forcibly closed by the remote host"))
                {
                    //remove the client from the list as its disconnected
                    Message msg = Message.Command("CLOSING");
                    msg.SetClientID(clientID);
                    bool shouldExit = false;
                    ProcessCommand(msg, ref shouldExit);
                    RaiseClientDisconnected(ClientAddress(client), clientID);
                }
                else
                {
                    //something else happened?
                    if (!RaiseError(error))
                    {
                        RaiseStopped();
                        Result = false;
                    }
                }
            }
            catch
            {
#if DEBUG
                EventLog.Debug("send Result = false");
                EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                //if an error occurs then indicate not continue
                Result = false;
            }

            return Result;
        }

        internal void newClient_MessageReceived(object sender, Message message)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            if (MessageReceived != null)
                MessageReceived(this, message);
        }

        /// <summary>
        /// Determines how many clients are currently connected
        /// </summary>
        /// <returns></returns>
        internal ushort ConnectedClientCount()
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            ushort Result = 0;

            using (TimedLock.Lock(_messageLockObject))
            {
                foreach (KeyValuePair<string, ConnectedClient> kvp in _clientsDictionary)
                {
                    if (kvp.Value.Client.Connected)
                        Result++;
                }
            }

            return Result;
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Retrieves the clients ip address
        /// </summary>
        /// <param name="client">client who's ip address sought</param>
        /// <returns>ip address of client</returns>
        private string ClientAddress(TcpClient client)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        }

        #endregion Private Methods

        #region Events

        #region Internal Methods

        /// <summary>
        /// Raises event to indicates a client want to connects
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        internal bool RaiseAllowClientConnect(string ipAddress)
        {
            ClientAllowConnectEventArgs args = new ClientAllowConnectEventArgs(ipAddress);

            if (AllowClientConnect != null)
                AllowClientConnect(this, args);

            return args.Allow;
        }

        /// <summary>
        /// Raises the ClientConnected event handler
        /// </summary>
        /// <param name="ipAddress">IP Address of client</param>
        /// <param name="clientID">ClientID of client who is connecting</param>
        internal void RaiseClientConnected(string ipAddress, string clientID)
        {
            if (ClientConnected != null)
                ClientConnected(this, new ClientEventArgs(ipAddress, clientID));
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Raises Internal error event handler
        /// </summary>
        /// <param name="error">Error that occured</param>
        /// <returns>bool true to continue false to stop the server</returns>
        private bool RaiseError(Exception error)
        {
            ErrorEventArgs args = new ErrorEventArgs(error);

            if (OnError != null)
                OnError(this, args);

            return args.Continue;
        }

        /// <summary>
        /// Raised the ClientDisconnected event handler
        /// </summary>
        /// <param name="IPAddress">IP Address of client that is disconnecting</param>
        /// <param name="ClientID">ClientID of client who is disconnecting</param>
        private void RaiseClientDisconnected(string IPAddress, string ClientID)
        {
            if (ClientDisconnected != null)
                ClientDisconnected(this, new ClientEventArgs(IPAddress, ClientID));
        }

        /// <summary>
        /// Raises the Started Event
        /// </summary>
        private void RaiseStarted()
        {
            if (Started != null)
                Started(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the stopped event
        /// </summary>
        private void RaiseStopped()
        {
            if (Stopped != null)
                Stopped(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises Client Command event
        /// </summary>
        /// <param name="message"></param>
        private void RaiseClientCommand(Message message)
        {
            if (ClientCommand != null)
                ClientCommand(this, message);
        }

        /// <summary>
        /// Raises an event to indicate the client wants to login
        /// </summary>
        /// <param name="IPAddress">IP Address of client</param>
        /// <param name="UserName">Username provided by client</param>
        /// <param name="Password">Password provided by client</param>
        /// <returns></returns>
        private bool RaiseOnLogin(string IPAddress, string UserName, string Password)
        {
            ClientLoginArgs args = new ClientLoginArgs(IPAddress, UserName, Password);

            if (ClientLogin != null)
                ClientLogin(this, args);

            return args.LoggedIn;
        }

        /// <summary>
        /// Raise a file received event
        /// 
        /// Contains progess of file received/sent
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">ReceiveFileEventArgs</param>
        private void RaiseFileReceived(object sender, TransferFileEventArgs e)
        {
            if (FileReceived != null)
                FileReceived(this, e);
        }

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// Raises a FileReceive event
        /// </summary>
        /// <param name="FileName">Filename being received</param>
        /// <param name="ClientID">ID of client requesting file transfer</param>
        /// <returns>Client should return a fully qualified path/file name of the file to be returned</returns>
        internal string RaiseFileReceive(string FileName, string ClientID)
        {
            TransferFileEventArgs args = new TransferFileEventArgs(FileName, ClientID);

            if (FileReceive != null)
                FileReceive(this, args);

            return args.FileName;
        }

        #endregion Internal Methods

        /// <summary>
        /// Event raised when a client command is received
        /// </summary>
        public event MessageReceivedEventHandler ClientCommand;

        /// <summary>
        /// Event raised when a message is received
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// Event raised when a client attempts to connect
        /// </summary>
        public event ClientAllowConnectEventHandler AllowClientConnect;

        /// <summary>
        /// Event raised when a client connects
        /// </summary>
        public event ClientEventHandler ClientConnected;

        /// <summary>
        /// Event raised when a client disconnects
        /// </summary>
        public event ClientEventHandler ClientDisconnected;

        /// <summary>
        /// Event raised when the Message Server is started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Event raised when the Message Server is stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Event raised when an exception is raised
        /// </summary>
        public event ErrorEventHandler OnError;

        /// <summary>
        /// Event raised when a client logs in
        /// </summary>
        public event ClientLoginHandler ClientLogin;

        /// <summary>
        /// Event raised when a file is about to be received
        /// </summary>
        public event FileReceivedHandler FileReceive;

        /// <summary>
        /// Event raised after a file is received
        /// </summary>
        public event FileReceivedHandler FileReceived;

        #endregion Events
    }

    /// <summary>
    /// Internal thread used to listen for client connections
    /// </summary>
    internal class MessageServerClientConnectionThread : ThreadManager
    {
        private Int64 _nextClientID = 0;
        private readonly int _port;
        private TcpListener _tcpListener;

        #region Constructors

        internal MessageServerClientConnectionThread(MessageServer parent, int port)
            : base(parent, new TimeSpan())
        {
            HangTimeout = 0;
            ContinueIfGlobalException = false;

            _port = port;
            _nextClientID = 0;
        }

        #endregion Constructors

        #region Overridden Methods

        /// <summary>
        /// Thread body for listening for client connections
        /// </summary>
        protected override bool Run(object parameters)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            MessageServer parentServer = (MessageServer)parameters;

            _tcpListener = new TcpListener(IPAddress.Any, _port);
            try
            {
                _tcpListener.Start();
                parentServer.Running = true;

                while (parentServer.IsRunning())
                {
                    try
                    {
                        if (HasCancelled())
                            return false;

                        TcpClient connectedTcpClient = _tcpListener.AcceptTcpClient();
#if DEBUG
                        EventLog.Debug("Accept TCP Client");
                        EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                        using (TimedLock.Lock(parentServer._messageLockObject))
                        {
                            if (HasCancelled())
                                return false;

                            // assign next client id
                            string activeNewClientID = String.Format("Client:{0}", _nextClientID);

                            while (parentServer._clientsDictionary.ContainsKey(activeNewClientID))
                            {
                                _nextClientID++;
                                activeNewClientID = String.Format("Client:{0}", _nextClientID);
                            }

                            try
                            {
                                // are there too many clients connected.
                                if (parentServer._maxClientConnections > 0 &&
                                    (parentServer.ConnectedClientCount() >= parentServer._maxClientConnections))
                                {
                                    parentServer.sendMessage(activeNewClientID, connectedTcpClient,
                                        new Message("Error", "Too Many Clients", MessageType.Error));
                                    connectedTcpClient.Close();
                                    continue;
                                }

                                //is the client allowed to connect
                                if (parentServer.RaiseAllowClientConnect(ClientAddress(connectedTcpClient)))
                                {
                                    ConnectedClient client = new ConnectedClient(activeNewClientID, connectedTcpClient);
                                    client.BufferSize = FileBufferSize.Size4096;
                                    parentServer._clientsDictionary.Add(activeNewClientID, client);

                                    // Remember the new connection
                                    ClientMessageListeningThread newClient = new ClientMessageListeningThread(
                                        client, parentServer, this);
                                    newClient.MessageReceived += parentServer.newClient_MessageReceived;
                                    ThreadManager.ThreadStart(newClient, String.Format("Client Connection - {0} {1}",
                                        activeNewClientID, connectedTcpClient.Client.RemoteEndPoint.ToString()),
                                        ThreadPriority.Lowest);

                                    parentServer.sendMessage(activeNewClientID, connectedTcpClient, Message.Command(
                                        "NEWCLIENTID", activeNewClientID));

                                    parentServer.RaiseClientConnected(ClientAddress(connectedTcpClient), activeNewClientID);

                                    if (parentServer.LoginRequird)
                                        parentServer.sendMessage(activeNewClientID, connectedTcpClient,
                                            Message.Command("LOGIN_REQUIRED", client.RandomPassword));
                                }
                                else
                                {
                                    //not allowed to connect
                                    parentServer.sendMessage(activeNewClientID, connectedTcpClient,
                                        Message.Command("CONNECTION_REFUSED"));
                                    connectedTcpClient.Close();
                                }

                                //increment next connection id
                                ++_nextClientID;
                            }
                            catch (Exception err)
                            {
#if DEBUG
                                EventLog.Debug(err);
                                EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                                if (!parentServer.HandleClientException(err, activeNewClientID, connectedTcpClient))
                                    throw;
                            }
                        }
                    }
                    catch (Exception error)
                    {
#if DEBUG
                        EventLog.Debug(error);
                        EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                        if (!error.Message.Contains("A blocking operation was interrupted by a call to WSACancelBlockingCall"))
                            if (parentServer.IsRunning())
                                if (!parentServer.HandleClientException(error, "SERVER", null))
                                    throw;
                    }
                }
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                if (err.Message.Contains("Only one usage of each socket address") ||
                    err.Message.Contains("An attempt was made to access a socket in a way forbidden by its access permissions"))
                    return false;
                else
                    throw;
            }
            finally
            {
                if (parentServer.Running)
                    parentServer.Running = false;

                _tcpListener.Stop();
                _tcpListener = null;
            }

            return false;
        }

        public override void Abort()
        {
            FalseConnect();
        }

        internal void FalseConnect()
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            //the server thread is blocked until it has a connection
            // send a false connection now to stop blocking and
            // allow the server to stop
            try
            {
                TcpClient tcpClient = new TcpClient("localhost", _port);
                try
                {
                    if (!tcpClient.Connected)
                    {
                        tcpClient.Connect("localhost", _port);
                        tcpClient.GetStream().Write(new byte[] { 0 }, 0, 1);
                    }
                }
                finally
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("actively refused"))
                    throw;
            }
        }

        #endregion Overridden Methods

        #region Private Methods

        /// <summary>
        /// Retrieves the clients ip address
        /// </summary>
        /// <param name="client">client who's ip address sought</param>
        /// <returns>ip address of client</returns>
        private string ClientAddress(TcpClient client)
        {
            return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        }

        #endregion Private Methods
    }

    internal class MessageServerMaintenance : ThreadManager
    {
        #region Private Members

        private readonly MessageServer _parentMessageServer;

        #endregion Private Members

        #region Constructors

        internal MessageServerMaintenance(MessageServer parent)
            : base(parent, new TimeSpan(0, 0, 5))
        {
            HangTimeout = 3;

            _parentMessageServer = parent;
        }

        #endregion Constructors

        #region Overridden Methods

        protected override bool Run(object parameters)
        {
            return false;
            //MessageServer messageServer = (MessageServer)parameters;

            //if (_parentMessageServer.ClientTimeOut > 0)
            //{
            //    //copy the clients dictionary in case its modified whilst iterating
            //    Dictionary<string, ConnectedClient> clients = new Dictionary<string, ConnectedClient>(_parentMessageServer.ClientDictionary);

            //    foreach (KeyValuePair<string, ConnectedClient> entry in clients)
            //    {
            //        if (!entry.Value.Client.Connected)
            //            continue;

            //        TimeSpan spanSent = DateTime.Now - entry.Value.LastReceived;
            //        TimeSpan spanReceived = DateTime.Now - entry.Value.LastSent;

            //        if (spanSent.Seconds > _parentMessageServer.ClientTimeOut && spanReceived.Seconds > _parentMessageServer.ClientTimeOut)
            //        {
            //            using (TimedLock.Lock(_parentMessageServer._messageLockObject))
            //            {
            //                _parentMessageServer.sendMessage(entry.Key, entry.Value.Client, Message.Command("TIMEOUT"));

            //                if (_parentMessageServer.ClientDictionary.ContainsKey(entry.Key))
            //                    _parentMessageServer.ClientDictionary[entry.Key].Client.Close();
            //            }
            //        }
            //    }
            //}

            //return (!HasCancelled());
        }

        #endregion Overridden Methods
    }

    internal class ClientMessageListeningThread : ThreadManager
    {
        #region Private Members

        private readonly MessageServer _parentMessageServer;

        private readonly StringBuilder _completeMessage;

        #endregion Private Members

        #region Constructors

        internal ClientMessageListeningThread(ConnectedClient connectedClient,
                MessageServer parent, MessageServerClientConnectionThread parentThread)
            : base(connectedClient, new TimeSpan(), parentThread)
        {
            HangTimeout = 0;
            _parentMessageServer = parent;
            _completeMessage = new StringBuilder();
        }

        #endregion Constructors

        #region Overridden Methods

        protected override bool Run(object parameters)
        {
#if DEBUG
            EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            byte brokenMessageCount = 0;
            ConnectedClient connectedClient = (ConnectedClient)parameters;
            try
            {
                while (_parentMessageServer.IsRunning())
                {
                    IndicateNotHanging();

                    //if client is no longer connected then exit
                    if (!connectedClient.Client.Connected)
                        return false;

                    // Block until an instance Message is received
                    byte[] bytes = new byte[(int)connectedClient.BufferSize];

                    // Read can return anything from 0 to numBytesToRead. 
                    // This method blocks until at least one byte is read.

                    do
                    {
                        int bytesRead = connectedClient.Client.GetStream().Read(bytes, 0, bytes.Length);

                        _completeMessage.AppendFormat("{0}", Encoding.UTF8.GetString(bytes, 0, bytesRead));

                    } while (connectedClient.Client.GetStream().DataAvailable);


                    //if we haven't got the complete message, keep going until it's here
                    if (!_completeMessage.ToString().EndsWith("#END#") && brokenMessageCount < 100)
                    {
                        brokenMessageCount++;
                        continue;
                    }

                    brokenMessageCount = 0;

                    // Returns the data received from the host to the console.
                    string[] messages = _completeMessage.ToString().Split(new string[] { "#END#" }, StringSplitOptions.None);

                    _completeMessage.Clear();

                    foreach (string msg in messages)
                    {
                        if (String.IsNullOrEmpty(msg.Trim()))
                            continue;

                        //multiple messages in one send, needs to be broken down into individual messages
                        Message message = Message.StringToMessage(msg);

#if DEBUG
                        EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif

                        connectedClient.LastReceived = DateTime.Now;

                        if (String.IsNullOrEmpty(message.ClientID))
                            connectedClient.Client.Close();

                        // Notify all registered event handlers about the message.
                        if (message != null)
                        {
                            switch (message.Type)
                            {
                                case MessageType.Command:
                                    bool shouldExit = false;

                                    //Command message, are we intercepting and processing it?  if not send it on.
                                    if (!_parentMessageServer.ProcessCommand(message, ref shouldExit))
                                    {
                                        if (shouldExit)
                                            return false;
                                        else
                                            continue;
                                    }

                                    break;
                                case MessageType.File:
                                    _parentMessageServer.ProcessFileRequests(message);

                                    continue;
                                case MessageType.User:
                                    //User messages handled internally, process and continue
                                    _parentMessageServer.sendMessage(message, message.Title);

                                    continue;
                                default:
                                    //all other messages sent direct to host application

                                    //if its a broadcast message then re-broadcast to all clients
                                    if (message.Type == MessageType.Broadcast)
                                        _parentMessageServer.SendMessage(message);

                                    break;
                            }
                        }

                        if (MessageReceived != null && message != null)
                        {
                            MessageReceived(this, message);
                        }
                    } //end of each
                }
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("MessageServer.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                //if the error is not handled then pass it on
                if (!_parentMessageServer.HandleClientException(err, connectedClient.ClientID, connectedClient.Client))
                    throw;
            }

            return !HasCancelled();
        }

        #endregion Overridden Methods

        #region Events

        internal event MessageReceivedEventHandler MessageReceived;

        #endregion Events
    }
}
