/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2013 Simon Carter
 *
 *  Purpose:  Message to be sent between tcp client/server
 *
 */
using System;
using System.IO;

namespace Shared.Communication
{
    /// <summary>
    /// Enumeration that define the type of message
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Error Message
        /// </summary>
        Error,

        /// <summary>
        /// Warning Message
        /// </summary>
        Warning,

        /// <summary>
        /// Information Message
        /// </summary>
        Info,

        /// <summary>
        /// Acknowledgement
        /// </summary>
        Acknowledge,

        /// <summary>
        /// Command message
        /// </summary>
        Command,

        /// <summary>
        /// Broadcasts message to all clients
        /// </summary>
        Broadcast,

        /// <summary>
        /// File Transfer message
        /// </summary>
        File,

        /// <summary>
        /// Sends a message to an individual user
        /// </summary>
        User
    }

    /// <summary>
    /// Custom message used in communication
    /// </summary>
    [Serializable]
    public class Message
    {
        #region Private Members

        private MessageType _messageType;
        private string _messageTitle;
        private string _messageContents;
        private string _clientID;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">Title of message</param>
        /// <param name="contents">Content of message</param>
        /// <param name="type">message type</param>
        public Message(string title, string contents, MessageType type)
        {
            this.Title = title;
            this.Contents = contents;
            this.Type = type;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Get Client ID of the connection as set by the server
        /// </summary>
        public string ClientID
        {
            get
            {
                return _clientID;
            }
        }

        /// <summary>
        /// Get/Set the type of message
        /// </summary>
        public MessageType Type
        {
            get
            {
                return this._messageType;
            }

            set
            {
                this._messageType = value;
            }
        }

        /// <summary>
        /// Get/Set the title of the message
        /// </summary>
        public string Title
        {
            get
            {
                return this._messageTitle;
            }

            set
            {
                if (value.Length > 300)
                    throw new Exception("Title too long, maximum of 300 characters!");

                this._messageTitle = value;
            }
        }

        /// <summary>
        /// Get/Set the content of the message
        /// </summary>
        public string Contents
        {
            get
            {
                return this._messageContents;
            }

            set
            {
                this._messageContents = value;
            }
        }

        #endregion Properties

        #region Internal Methods

        /// <summary>
        /// Methods used internally to set the client id
        /// </summary>
        /// <param name="ClientID">New Client ID</param>
        internal void SetClientID(string ClientID)
        {
            _clientID = ClientID;
        }

        #endregion Internal Methods

        #region Static Methods

        /// <summary>
        /// Creates a Command Message
        /// </summary>
        /// <param name="command">Command to be passed</param>
        /// <param name="parameters">Optional parameters to be passed in the body of the command Message</param>
        /// <returns>Message Object</returns>
        public static Message Command(string command, string parameters = "")
        {
            return new Message(command, parameters, MessageType.Command);
        }


        /// <summary>
        /// Creates a Error Message for Feature Not Supported
        /// </summary>
        /// <param name="contents">Optional contents for message</param>
        /// <returns></returns>
        public static Message FeatureNotSupported(string contents = "")
        {
            return new Message("FEATURE_NOT_SUPPORTED", contents, MessageType.Error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Message FileToStringMessage(string fileName)
        {
            Message Result = new Message(Path.GetFileName(fileName), "", MessageType.File);

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
                    Result.Contents = Convert.ToBase64String(bin);
                }
            }

            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringMessage"></param>
        /// <returns></returns>
        public static Message StringToMessage(string stringMessage)
        {
#if DEBUG
            EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
                MessageType type = CharToMessageType(stringMessage[0]);

                int sepCharsTitle = stringMessage.IndexOf("#A!");
                int sepCharsClientID = stringMessage.IndexOf("#B!");


                string title = stringMessage.Substring(1, sepCharsTitle - 1);

                string clientID = stringMessage.Substring(sepCharsTitle + 3, sepCharsClientID - 3 - sepCharsTitle);
                string contents = stringMessage.Substring(sepCharsClientID + 3).TrimEnd('\0');
                Message Result = new Message(title.Trim(), contents.Trim(), type);
                Result.SetClientID(clientID);
                return Result;
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                EventLog.Add(err, stringMessage);
                return new Message("Error", err.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// Converts string array to message
        /// </summary>
        /// <param name="charArray"></param>
        /// <returns></returns>
        public static Message StringArrayToMessage(char[] charArray)
        {
#if DEBUG
            EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
                MessageType type = CharToMessageType(charArray[0]);

                string msgArray = new string(charArray);
                int sepCharsTitle = msgArray.IndexOf("#A!");
                int sepCharsClientID = msgArray.IndexOf("#B!");


                string title = msgArray.Substring(1, sepCharsTitle - 1);

                string clientID = msgArray.Substring(sepCharsTitle + 3, sepCharsClientID - 3 - sepCharsTitle);
                string contents = msgArray.Substring(sepCharsClientID + 3).TrimEnd('\0');
                Message Result = new Message(title.Trim(), contents.Trim(), type);
                Result.SetClientID(clientID);
                return Result;
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                EventLog.Add(err);
                return new Message("Error", err.Message, MessageType.Error);
            }
        }

        /// <summary>
        /// Converts message to string array
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static char[] MessageToStringArray(Message message)
        {
#if DEBUG
            EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            try
            {
#if DEBUG
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name + " After MessageTypeToChar");
#endif
                string msgBody = String.Format("{3}{0}#A!{1}#B!{2}#END#",
                    message.Title, message.ClientID, message.Contents,
                    MessageTypeToChar(message.Type));
#if DEBUG
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name + " After msgBody set");
                EventLog.Debug(msgBody);
#endif
                char[] Result = msgBody.ToCharArray();
#if DEBUG
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name + " before return");
#endif

                return Result;
            }
            catch (Exception err)
            {
#if DEBUG
                EventLog.Debug(err);
                EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                string msgBody = String.Format("{3}{0}#A!{1}#B!{2}",
                    message.Title, message.ClientID, err.Message,
                    MessageTypeToChar(MessageType.Error));
                return msgBody.ToCharArray();
            }
        }

        /// <summary>
        /// Gets the message type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static MessageType CharToMessageType(char type)
        {
#if DEBUG
            EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            switch (type)
            {
                case 'A':
                    return MessageType.Acknowledge;
                case 'B':
                    return MessageType.Broadcast;
                case 'C':
                    return MessageType.Command;
                case 'D':
                    return MessageType.Error;
                case 'E':
                    return MessageType.File;
                case 'F':
                    return MessageType.Info;
                case 'G':
                    return MessageType.User;
                case 'H':
                    return MessageType.Warning;
                default:
#if DEBUG
                    EventLog.Debug("Message.cs - Unknown Message Type" + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
                    throw new Exception("Invalid Message Type");
            }
        }

        /// <summary>
        /// Converts message type to char
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static char MessageTypeToChar(MessageType type)
        {
#if DEBUG
            EventLog.Debug(String.Format("Type: {0}", type.ToString()));
            EventLog.Debug("Message.cs " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
            switch (type)
            {
                case MessageType.Acknowledge:
                    return 'A';
                case MessageType.Broadcast:
                    return 'B';
                case MessageType.Command:
                    return 'C';
                case MessageType.Error:
                    return 'D';
                case MessageType.File:
                    return 'E';
                case MessageType.Info:
                    return 'F';
                case MessageType.User:
                    return 'G';
                case MessageType.Warning:
                    return 'H';
                default:
                    throw new Exception("Invalid Message Type");
            }
        }

        #endregion Static Methods
    }
}
