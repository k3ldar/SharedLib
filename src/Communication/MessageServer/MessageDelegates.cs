/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  TCP/IP Message Server Delegates
 *
 */
using System;

namespace Shared.Communication
{
    /// <summary>
    /// Indicates a property is not valid in this context
    /// </summary>
    public class InvalidProperty : Exception { }

    /// <summary>
    /// Event Arguments for when an error occurs
    /// </summary>
    public class ErrorEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="error"></param>
        /// <param name="allowContinue"></param>
        public ErrorEventArgs(Exception error, bool allowContinue = false) { Error = error; Continue = allowContinue; }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Exception being raised
        /// </summary>
        public Exception Error { private set; get; }

        /// <summary>
        /// Determines whether to continue or not
        /// </summary>
        public bool Continue { set; get; }

        #endregion Properties
    }

    /// <summary>
    /// Event Args for allowing/denying a client conenction
    /// </summary>
    public class ClientAllowConnectEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ipAddress"></param>
        public ClientAllowConnectEventArgs(string ipAddress) { IPAddress = ipAddress; Allow = true; }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// IP Address of client trying to connect
        /// </summary>
        public string IPAddress { private set; get; }

        /// <summary>
        /// Specify whether the client is allowed to connect (true) or deny the connection (false)
        /// </summary>
        public bool Allow { set; get; }

        #endregion Properties
    }

    /// <summary>
    /// Event arguments for transferring a file
    /// </summary>
    public class TransferFileEventArgs
    {
        /// <summary>
        /// Used when calculating speed of transfer
        /// </summary>
        private readonly string[] SPEED_FORMAT = { "{0} bytes/s", "{0} KB/s", "{0} MB/s", "{0} TB/s" };

        private readonly bool _init;

        #region Constructors

        /// <summary>
        /// Constructor, used when initialising transfer between client/server
        /// </summary>
        /// <param name="fileName"></param>
        public TransferFileEventArgs(string fileName)
        {
            FileName = fileName;
            _init = true;
        }

        /// <summary>
        /// Constructor, used when initialising transfer between client/server
        /// </summary>
        /// <param name="fileName">Name of file being trnsferred</param>
        /// <param name="clientID">ID of client sending/receiving files</param>
        public TransferFileEventArgs(string fileName, string clientID)
        {
            FileName = fileName;
            ClientID = clientID;
            _init = true;
        }

        /// <summary>
        /// Constructor, used when file transfer is in progress or transfer is complete
        /// </summary>
        /// <param name="fileName">Name of file being transferred</param>
        /// <param name="received">Number of bytes received/sent</param>
        /// <param name="total">Total bytes to receive/sent</param>
        /// <param name="timeTaken">Time taken to transfer file at current speed</param>
        /// <param name="transferRate">Number of bytes transferred per second</param>
        public TransferFileEventArgs(string fileName, ulong received, ulong total, TimeSpan timeTaken, double transferRate)
        {
            FileName = fileName;
            Received = received;
            Total = total;
            TimeTaken = timeTaken;
            TransferRate = transferRate;
            _init = false;
        }

        /// <summary>
        /// Constructor, used when file transfer is in progress or transfer is complete within the server
        /// </summary>
        /// <param name="fileName">Name of file being transferred</param>
        /// <param name="received">Number of bytes received/sent</param>
        /// <param name="total">Total bytes to receive/sent</param>
        /// <param name="timeTaken">Time taken to transfer file at current speed</param>
        /// <param name="transferRate">Number of bytes transferred per second</param>
        /// <param name="clientID">ID of client sending/receiving file</param>
        public TransferFileEventArgs(string fileName, ulong received, ulong total,
            TimeSpan timeTaken, double transferRate, string clientID)
        {
            FileName = fileName;
            Received = received;
            Total = total;
            TimeTaken = timeTaken;
            TransferRate = transferRate;
            _init = false;
            ClientID = clientID;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ClientID
        /// 
        /// Only set server Side
        /// </summary>
        public string ClientID
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates the number of bytes sent/received
        /// </summary>
        public ulong Received
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the total number of bytes to send/receive
        /// </summary>
        public ulong Total
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the file download as a percentage
        /// </summary>
        public ulong Percentage
        {
            get
            {
                if (_init)
                    throw new InvalidProperty();

                return Received / Total;
            }
        }

        /// <summary>
        /// Name of the file being transferred
        /// </summary>
        public string FileName
        {
            set;
            get;
        }

        /// <summary>
        /// Retrieves time span for how long the download/upload has taken
        /// </summary>
        public TimeSpan TimeTaken
        {
            private set;
            get;
        }

        /// <summary>
        /// Retrieves time span with time remaining
        /// </summary>
        public TimeSpan TimeRemaining
        {
            get
            {
                if (_init)
                    throw new InvalidProperty();

                //calculate time remaining
                ulong dataLeft = Total - Received;
                double remaining = Math.Round(dataLeft / (TransferRate == 0.00 ? 0.01 : TransferRate));
                TimeSpan Result = DateTime.Now.AddSeconds(remaining) - DateTime.Now;

                return Result;
            }
        }

        /// <summary>
        /// Indicates the transfer speed, number of bytes per second
        /// </summary>
        public double TransferRate
        {
            private set;
            get;
        }

        /// <summary>
        /// Retrieves the transfer speed as a string (?? kb/s)
        /// </summary>
        public string TransferSpeed
        {
            get
            {
                if (_init)
                    throw new InvalidProperty();

                int i = 0;
                double speed = TransferRate;

                while (speed > 1024.0)
                {
                    ++i;
                    speed = speed / 1024;
                }

                return String.Format(SPEED_FORMAT[i], speed.ToString("N"));
            }
        }

        #endregion Properties
    }

    /// <summary>
    /// Client event arguments
    /// </summary>
    public class ClientEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor for client event args
        /// </summary>
        /// <param name="ipAddress">IP Address of client</param>
        /// <param name="clientID">Client ID</param>
        public ClientEventArgs(string ipAddress, string clientID) { IPAddress = ipAddress; ClientID = clientID; }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// IP Address of client
        /// </summary>
        public string IPAddress { private set; get; }

        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientID { private set; get; }

        #endregion Properties
    }

    /// <summary>
    /// Arguments used for client login
    /// </summary>
    public class ClientLoginArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor - Used Server Side
        /// </summary>
        /// <param name="ipAddress">IP Address of client</param>
        /// <param name="userName">Username returned by client</param>
        /// <param name="password">Password returned by client</param>
        public ClientLoginArgs(string ipAddress, string userName, string password) { IPAddress = ipAddress; Username = userName; Password = password; LoggedIn = false; }

        /// <summary>
        /// Constructor - Used Client Side
        /// </summary>
        public ClientLoginArgs() { }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// IP Address of client trying to login
        /// </summary>
        public string IPAddress { set; get; }

        /// <summary>
        /// Username for login
        /// </summary>
        public string Username { set; get; }

        /// <summary>
        /// Password for login
        /// </summary>
        public string Password { set; get; }

        /// <summary>
        /// Determines whether the client is logged in using Username/Password supplied (true = yes logged in)
        /// </summary>
        public bool LoggedIn { set; get; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for allowing client connections
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ClientAllowConnectEventHandler(object sender, ClientAllowConnectEventArgs e);

    /// <summary>
    /// Delegare for handling Errors and exceptions
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

    /// <summary>
    /// Delegate for receiving messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public delegate void MessageReceivedEventHandler(object sender, Message message);

    /// <summary>
    /// Delegare for client event arguments
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ClientEventHandler(object sender, ClientEventArgs e);

    /// <summary>
    /// Delegate for client logins
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ClientLoginHandler(object sender, ClientLoginArgs e);

    /// <summary>
    /// Delegare for receiving files
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileReceivedHandler(object sender, TransferFileEventArgs e);
}
