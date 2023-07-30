/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  used with Message Server, represents an internally connected TCP client
 *
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace Shared.Communication
{
    /// <summary>
    /// Represents a connected client
    /// </summary>
    internal class ConnectedClient
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientID">Client Connection</param>
        /// <param name="client">TCP Client object</param>
        internal ConnectedClient(string clientID, TcpClient client)
        {
            BufferSize = FileBufferSize.Size4096;
            Client = client;
            LoggedIn = false;
            ClientID = clientID;
            ConnectionStarted = DateTime.Now;
            LastReceived = ConnectionStarted;
            ClientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            LoginName = "not logged in";
            RandomPassword = Utilities.RandomString(Utilities.RandomNumber(15, 25));
        }

        #region Properties

        /// <summary>
        /// Holds the client IP address
        /// </summary>
        internal string ClientIP
        {
            private set;
            get;
        }

        /// <summary>
        /// Random password used for encrypting/decrypting user details
        /// </summary>
        internal string RandomPassword
        {
            private set;
            get;
        }

        /// <summary>
        /// Client ID for connection
        /// </summary>
        internal string ClientID
        {
            private set;
            get;
        }

        /// <summary>
        /// TCP Connection
        /// </summary>
        internal TcpClient Client
        {
            get;
            set;
        }

        /// <summary>
        /// Thread used to listen for messages from the client
        /// </summary>
        internal Thread ListenThread
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the time the client connected
        /// </summary>
        internal DateTime ConnectionStarted
        {
            private set;
            get;
        }

        /// <summary>
        /// DateTime data was last received from the client
        /// </summary>
        internal DateTime LastReceived
        {
            set;
            get;
        }

        /// <summary>
        /// Indicates when data was last sent to the client
        /// </summary>
        internal DateTime LastSent
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates wether the client is logged in or not
        /// </summary>
        internal bool LoggedIn
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates wether the client wants to ignore broadcast messages or not.
        /// </summary>
        internal bool IgnoreBroadcastMessages
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the buffer size specified by the client
        /// </summary>
        internal FileBufferSize BufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the logged in user/client
        /// </summary>
        internal string LoginName
        {
            get;
            set;
        }

        /// <summary>
        /// User definded object for client
        /// </summary>
        internal object UserData
        {
            get;
            set;
        }

        #endregion Properties
    }
}
