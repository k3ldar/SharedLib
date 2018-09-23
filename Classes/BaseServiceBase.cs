/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  Base Service Class with optional MessageServer communication class
 *
 */

using System;
using System.ServiceProcess;

using Shared.Classes;
using Shared.Communication;

namespace Shared
{
    /// <summary>
    /// Base service class
    /// 
    /// Contains methods/events which descendant classes can use when creating a service application
    /// </summary>
    public partial class BaseService : ServiceBase
    {
        #region Private Members

        /// <summary>
        /// TCP Message Server
        /// </summary>
        internal MessageServer _messageServer;

        /// <summary>
        /// Indicates wether an error occured with the message server
        /// </summary>
        internal bool _messageServerError = false;

        #endregion Private Members

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public BaseService()
        {
            MessageServerTimeOut = 60;
            MessageServerIsActive = false;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Indicates the message server is active and listening
        /// </summary>
        public bool MessageServerIsActive { get; private set; }

        /// <summary>
        /// Port used by message server
        /// </summary>
        public int MessageServerPort { get; set; }

        /// <summary>
        /// Timeout in seconds for message server
        /// </summary>
        public int MessageServerTimeOut { get; set; }

        /// <summary>
        /// Indicates wether the message server is active or not
        /// </summary>
        public bool MessageServerActive { get; set; }

        /// <summary>
        /// Indicates wether message service requires a login
        /// </summary>
        public bool MessageServerLogin { get; set; }


        /// <summary>
        /// Indicates wether a stop has been requested
        /// </summary>
        public bool StopRequested { get; private set; }

        #endregion Properties

        #region Protected Methods

        #region Service Methods

        /// <summary>
        /// start method
        /// 
        /// Called when service application is starting
        /// </summary>
        /// <param name="args">arguments passed to service</param>
        protected override void OnStart(string[] args)
        {
            ThreadManager.Initialise();

            Shared.EventLog.Add("Service Start");
            StopRequested = false;

            if (MessageServerActive)
                InitializeTCPServer();
        }

        /// <summary>
        /// Stop method
        /// 
        /// Called when service is being stopped
        /// </summary>
        protected override void OnStop()
        {
            StopRequested = true;
            Shared.EventLog.Add("Service Stop");

            TCPServerStop();
            Shared.Classes.ThreadManager.CancelAll(1);
            Classes.ThreadManager.Finalise();
        }

        #endregion Service Methods

        /// <summary>
        /// Message Received from tcp communication class
        /// </summary>
        /// <param name="sender">Message sender</param>
        /// <param name="message">Message being received</param>
        protected virtual void MessageReceived(object sender, Message message)
        {

        }

        /// <summary>
        /// User requested login to message server
        /// </summary>
        /// <param name="userName">username</param>
        /// <param name="password">user password</param>
        /// <param name="ipAddress">Users ip Address</param>
        /// <returns>true if login allowed, otherwise false</returns>
        protected virtual bool MessageServerLoginAttempt(string userName, string password, string ipAddress)
        {
            return (false);
        }

        /// <summary>
        /// Determines whether the client can connect or not
        /// </summary>
        /// <param name="ipAddress">ip Address trying to connect</param>
        /// <returns>true if allowed, otherwise false</returns>
        protected virtual bool MessageServerAllowConnect(string ipAddress)
        {
            return (true);
        }

        /// <summary>
        /// Method used so can run as application
        /// 
        /// Should be overridden in descendant class
        /// </summary>
        public virtual void RunAsApplication()
        {
            Shared.EventLog.Add("Run As Application Start");
            StopRequested = false;

            if (MessageServerActive)
                InitializeTCPServer();
        }

        /// <summary>
        /// Sends a message to a client
        /// </summary>
        /// <param name="message"></param>
        /// <param name="allClients"></param>
        protected void MessageSend(Message message, bool allClients = false)
        {
            if (_messageServer.IsRunning())
                _messageServer.SendMessage(message, allClients);
        }

        #endregion Protected Methods

        #region TCP Message Server

        /// <summary>
        /// Stops the TCP Server
        /// </summary>
        protected void TCPServerStop()
        {
            if (_messageServer != null)
                _messageServer.Stop();
        }

        /// <summary>
        /// Initialises the TCP Server
        /// </summary>
        internal void InitializeTCPServer()
        {
            Shared.EventLog.Add(String.Format("Initialising TCP: Port: {0}; Timeout: {1}", 
                MessageServerPort, MessageServerTimeOut));
            InitializeTCPServer(MessageServerPort, MessageServerTimeOut);
        }

        /// <summary>
        /// Initialises the TCP Server
        /// </summary>
        /// <param name="port">Port listening to</param>
        /// <param name="timeout">Timeout in seconds</param>
        protected void InitializeTCPServer(int port, int timeout = 60)
        {
            _messageServerError = false;

            if (!MessageServerActive)
                return;

            _messageServer = new MessageServer(Utilities.CheckMinMax(port, 0, 65535));
            _messageServer.MessageReceived += _messageServer_MessageReceived;
            _messageServer.ClientConnected += _messageServer_ClientConnected;
            _messageServer.ClientDisconnected += _messageServer_ClientDisconnected;
            _messageServer.OnError += _messageServer_OnError;
            _messageServer.Started += _messageServer_Started;
            _messageServer.Stopped += _messageServer_Stopped;
            _messageServer.AllowClientConnect += _messageServer_AllowClientConnect;
            _messageServer.ClientLogin += _messageServer_ClientLogin;
            _messageServer.ClientTimeOut = (ushort)Utilities.CheckMinMax(timeout, 5, 180); // 60 second timeout
            _messageServer.LoginRequird = MessageServerLogin;
            _messageServer.Start();

            if (!Classes.ThreadManager.Exists("Service Message Server Manager"))
            {
                MessageServerManagementThread thread = new MessageServerManagementThread(this);
                Classes.ThreadManager.ThreadStart(thread, "Service Message Server Manager", 
                    System.Threading.ThreadPriority.Lowest);
            }
        }

        private void _messageServer_ClientLogin(object sender, ClientLoginArgs e)
        {
            e.LoggedIn = MessageServerLoginAttempt(e.Username, e.Password, e.IPAddress);
        }

        /// <summary>
        /// Determines whether the connection is allowed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_AllowClientConnect(object sender, ClientAllowConnectEventArgs e)
        {
            e.Allow = MessageServerAllowConnect(e.IPAddress);
        }

        /// <summary>
        /// Message server stopped event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_Stopped(object sender, EventArgs e)
        {
            MessageServerIsActive = false;
            Shared.EventLog.Add("TCP Server Stopped");
        }

        /// <summary>
        /// Message server start event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_Started(object sender, EventArgs e)
        {
            Shared.EventLog.Add("TCP Server Started");
            MessageServerIsActive = true;
        }

        /// <summary>
        /// Message server error event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_OnError(object sender, Shared.Communication.ErrorEventArgs e)
        {
            Shared.EventLog.Add(String.Format("TCP Client error: {0}", e.Error.Message));

            if (e.Error.StackTrace != null)
                Shared.EventLog.Add(e.Error.StackTrace.ToString());

            e.Continue = false;
            _messageServerError = true;
        }

        /// <summary>
        /// Client disconnected event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_ClientDisconnected(object sender, ClientEventArgs e)
        {
            Shared.EventLog.Add(String.Format("TCP Client disconnected: {1} - {0}", e.ClientID, e.IPAddress));
        }

        /// <summary>
        /// Client connected event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _messageServer_ClientConnected(object sender, ClientEventArgs e)
        {
            Shared.EventLog.Add(String.Format("TCP Client connected: {1} -  {0}", e.ClientID, e.IPAddress));
        }

        /// <summary>
        /// Message received event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _messageServer_MessageReceived(object sender, Message message)
        {
            string LocalConfig = Utilities.CurrentPath() + "\\SDConfig.xml";

            if (message.Type == MessageType.Command)
            {
                switch (message.Title)
                {
                    case "STOPTCPSERVICE":
                        if (_messageServer != null)
                            _messageServer.Stop();

                        _messageServer = null;

                        break;

                    case "STARTTCPSERVICE":
                        InitializeTCPServer();

                        break;

                    case "STOPAPPLICATION":
                        StopRequested = true;
                        
                        break;

                    default:
                        MessageReceived(sender, message);
                        break;
                }
            }
            else
            {
                MessageReceived(sender, message);
            }
        }

        #endregion TCP Message Server
    }

    /// <summary>
    /// Message server management thread
    /// 
    /// Ensures the message server is running
    /// </summary>
    internal sealed class MessageServerManagementThread : Classes.ThreadManager
    {
        #region Constructor

        internal MessageServerManagementThread(BaseService serviceBase)
            : base (serviceBase, new TimeSpan(0, 0, 20), null, 2000, 200, false)
        {
            this.HangTimeout = 0;
        }

        #endregion Constructor

        #region Overridden Methods

        protected override bool Run(object parameters)
        {
            BaseService serviceBase = (BaseService)parameters;

            if (serviceBase.MessageServerActive && serviceBase._messageServerError)
            {
                if (serviceBase._messageServerError)
                {
                    if (serviceBase._messageServer != null)
                    {
                        serviceBase._messageServer.Stop();
                    }

                    serviceBase._messageServer = null;
                }

                if ((serviceBase._messageServer != null && !serviceBase._messageServer.Running) || 
                    (serviceBase._messageServer == null))
                {
                    serviceBase.InitializeTCPServer();
                }

            }

            return (serviceBase.MessageServerActive);
        }

        #endregion Overridden Methods
    }
}
