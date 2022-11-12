/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  File Transfer module for TCP Message Server/Client
 *
 */
using System;
using System.IO;
using System.Net.Sockets;

using Shared.Classes;

#pragma warning disable IDE1005 // Delegate invocation can be simplified

namespace Shared.Communication
{
    /// <summary>
    /// Available buffer sizes for transferring files
    /// </summary>
    public enum FileBufferSize
    {
        /// <summary>
        /// 1024 bytes
        /// </summary>
        Size1024 = 1024,

        /// <summary>
        /// 2048 bytes
        /// </summary>
        Size2048 = 2048,

        /// <summary>
        /// 4096 bytes
        /// </summary>
        Size4096 = 4096,

        /// <summary>
        /// 8192 bytes
        /// </summary>
        Size8192 = 8192,

        /// <summary>
        /// 16384 bytes
        /// </summary>
        Size16384 = 16384
    }


    /// <summary>
    /// Internal class used to transfer files between client/server
    /// </summary>
    internal class FileTransfer
    {
        #region Private Members

        private readonly object _messageLockObject = new object();

        #endregion Private Members

        #region Internal Methods

        /// <summary>
        /// Processes all file commands on behalf of the client
        /// </summary>
        /// <param name="client">MessageClient object</param>
        /// <param name="message">Message received</param>
        internal void ProcessClientFiles(MessageClient client, Message message)
        {
            string[] fileDetails = message.Contents.Split('$');
            string fileName = fileDetails[0];
            ulong fileSize = Convert.ToUInt64(fileDetails[1]);

            switch (message.Title)
            {
                case "SEND_FILE":
                    SendFile(client._tcpClient, fileDetails[0], client.BufferSize);
                    message.Title = "FILE_SENT";

                    break;
                case "SENDING_FILE":
                    try
                    {
                        //where is it being saved?
                        fileName = client.RaiseFileReceive(System.IO.Path.GetFileName(fileName));

                        //tell the client we are ready to receive
                        ReceiveFile(client._tcpClient, fileName, fileSize, client.BufferSize);
                        message.Title = "FILE_RECEIVED";
                    }
                    catch (Exception err)
                    {
                        client.sendMessage(new Message(err.Message, err.StackTrace.ToString(), MessageType.Error));
                    }

                    break;
            }
        }

        internal void ProcessServerFiles(MessageServer server, Message message)
        {

        }

        /// <summary>
        /// Process all file commands on behalf of the server
        /// </summary>
        /// <param name="server">MessageServer object</param>
        /// <param name="message">Message received</param>
        /// <param name="client">MessageClient object</param>
        internal void ProcessServerFiles(MessageServer server, Message message, ConnectedClient client)
        {
            switch (message.Title)
            {
                case "REQUEST_FILE":
                    string newFileName = server.RaiseFileReceive(message.Contents, client.ClientID);
                    FileInfo fi = new FileInfo(newFileName);
                    server.sendMessage(message.ClientID, client.Client, new Message("SENDING_FILE",
                        String.Format("{0}${1}", message.Contents, fi.Length), MessageType.File));

                    SendFile(client, newFileName, client.BufferSize);
                    message.Title = "FILE_SENT";

                    break;
                case "SEND_FILE":
                    if (server.AcceptFiles)
                    {
                        string[] fileDetails = message.Contents.Split('$');
                        ulong fileSize = Convert.ToUInt64(fileDetails[1]);

                        if (server.MaximumFileSize > 0 && fileSize > server.MaximumFileSize)
                        {
                            server.sendMessage(client.ClientID, client.Client, new Message("FILE_TOO_BIG",
                                message.Contents, MessageType.Error));
                        }
                        else
                        {
                            string fileName = server.RaiseFileReceive(System.IO.Path.GetFileName(
                                fileDetails[0]), client.ClientID);

                            //tell the client we are ready to receive
                            server.sendMessage(client.ClientID, client.Client, new Message("SEND_FILE",
                                message.Contents, MessageType.File));

                            //receive the file
                            ReceiveFile(client, fileName, fileSize, client.BufferSize);
                            message.Title = "FILE_RECEIVED";
                        }
                    }
                    else
                    {
                        server.sendMessage(client.ClientID, client.Client, new Message(
                            "FILES_NOT_ALLOWED", "", MessageType.Error));
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Receives a file via tcp socket and saves it to specified location
        /// 
        /// Client Side Only
        /// </summary>
        /// <param name="client">Client Tcp socket</param>
        /// <param name="FileName">Full Path/Name of file to be saved</param>
        /// <param name="FileSize">Size of file being transferred</param>        
        /// <param name="BufferSize">Size of buffer used to receive file</param>
        internal void ReceiveFile(TcpClient client, string FileName, ulong FileSize, FileBufferSize BufferSize)
        {
            NetworkStream netStream = client.GetStream();
            byte[] receivedBuffer = null;

            FileStream fStream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
            try
            {
                DateTime start = DateTime.Now;
                int packets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(FileSize) / Convert.ToDouble(BufferSize)));

                ulong totalLength = (uint)FileSize;
                ulong currentPacketLength;
                ulong totalReceived = 0;

                for (int i = 0; i < packets; i++)
                {
                    if (totalLength > (ulong)BufferSize)
                    {
                        currentPacketLength = (ulong)BufferSize;
                        totalLength -= currentPacketLength;
                    }
                    else
                    {
                        currentPacketLength = totalLength;
                    }

                    receivedBuffer = new byte[currentPacketLength];
                    totalReceived += (uint)netStream.Read(receivedBuffer, 0, (int)currentPacketLength);
                    fStream.Write(receivedBuffer, 0, (int)receivedBuffer.Length);

                    TimeSpan span = DateTime.Now - start;
                    double speed = span.Seconds > 0 && totalReceived > 0 ? totalReceived / (ulong)span.Seconds : 0;
                    RaiseFileReceived(this, new TransferFileEventArgs(FileName, totalReceived, FileSize, span, speed));
                }
            }
            finally
            {
                fStream.Close();
                fStream.Dispose();
                fStream = null;
            }
        }

        /// <summary>
        /// Receives a file via tcp socket and saves it to specified location
        /// 
        /// Server Side Only
        /// </summary>
        /// <param name="client">ConnectedClient object representing the client</param>
        /// <param name="FileName">Full Path/Name of file to be saved</param>
        /// <param name="FileSize">Size of file being transferred</param>        
        /// <param name="BufferSize">Size of buffer used to receive file</param>
        internal void ReceiveFile(ConnectedClient client, string FileName, ulong FileSize, FileBufferSize BufferSize)
        {
            NetworkStream netStream = client.Client.GetStream();
            byte[] receivedBuffer = null;

            FileStream fStream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
            try
            {
                DateTime start = DateTime.Now;
                int packets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(FileSize) / Convert.ToDouble(BufferSize)));

                ulong totalLength = FileSize;
                ulong currentPacketLength;
                ulong totalReceived = 0;

                for (int i = 0; i < packets; i++)
                {
                    if (totalLength > (ulong)BufferSize)
                    {
                        currentPacketLength = (uint)BufferSize;
                        totalLength -= currentPacketLength;
                    }
                    else
                    {
                        currentPacketLength = totalLength;
                    }

                    receivedBuffer = new byte[currentPacketLength];
                    totalReceived += (uint)netStream.Read(receivedBuffer, 0, (int)currentPacketLength);
                    fStream.Write(receivedBuffer, 0, (int)receivedBuffer.Length);

                    TimeSpan span = DateTime.Now - start;
                    double speed = span.Seconds > 0 && totalReceived > 0 ? totalReceived / (ulong)span.Seconds : 0;
                    RaiseFileReceived(this, new TransferFileEventArgs(FileName,
                        totalReceived, FileSize, span, speed, client.ClientID));
                }
            }
            finally
            {
                fStream.Close();
                fStream.Dispose();
                fStream = null;
            }
        }


        /// <summary>
        /// Sends a file to a tcp client
        /// 
        /// Client Side Only
        /// </summary>
        /// <param name="client">Client Tcp socket</param>
        /// <param name="FileName">Full Path/Name of file to be sent</param>
        /// <param name="BufferSize">Size of buffer used to receive file</param>
        internal void SendFile(TcpClient client, string FileName, FileBufferSize BufferSize)
        {
            byte[] SendingBuffer = null;
            NetworkStream netstream = client.GetStream();

            FileStream fStream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            try
            {
                DateTime start = DateTime.Now;
                int packets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(fStream.Length) /
                    Convert.ToDouble((uint)BufferSize)));

                uint totalLength = (uint)fStream.Length;
                uint currentPacketLength;
                uint totalSent = 0;

                for (int i = 0; i < packets; i++)
                {
                    if (totalLength > (uint)BufferSize)
                    {
                        currentPacketLength = (uint)BufferSize;
                        totalLength -= currentPacketLength;
                    }
                    else
                    {
                        currentPacketLength = totalLength;
                    }

                    SendingBuffer = new byte[currentPacketLength];
                    totalSent += (uint)fStream.Read(SendingBuffer, 0, (int)currentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);

                    TimeSpan span = DateTime.Now - start;
                    double speed = span.Seconds > 0 && totalSent > 0 ? totalSent / span.Seconds : 0.00;
                    RaiseFileReceived(this, new TransferFileEventArgs(FileName, totalSent, (uint)fStream.Length, span, speed));
                }
            }
            finally
            {
                fStream.Close();
                fStream.Dispose();
                fStream = null;
            }
        }


        /// <summary>
        /// Sends a file to a tcp client from the sever
        /// 
        /// Server Side Only
        /// </summary>
        /// <param name="client">ConnectedClient object representing the client</param>
        /// <param name="FileName">Full Path/Name of file to be sent</param>
        /// <param name="BufferSize">Size of buffer used to receive file</param>
        internal void SendFile(ConnectedClient client, string FileName, FileBufferSize BufferSize)
        {
            byte[] SendingBuffer = null;
            NetworkStream netstream = client.Client.GetStream();

            FileStream fStream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            try
            {
                DateTime start = DateTime.Now;
                int packets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(fStream.Length) /
                    Convert.ToDouble((uint)BufferSize)));

                uint totalLength = (uint)fStream.Length;
                uint currentPacketLength;
                uint totalSent = 0;

                for (int i = 0; i < packets; i++)
                {
                    if (totalLength > (uint)BufferSize)
                    {
                        currentPacketLength = (uint)BufferSize;
                        totalLength -= currentPacketLength;
                    }
                    else
                    {
                        currentPacketLength = totalLength;
                    }

                    SendingBuffer = new byte[currentPacketLength];
                    totalSent += (uint)fStream.Read(SendingBuffer, 0, (int)currentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);

                    using (TimedLock.Lock(_messageLockObject))
                    {
                        client.LastSent = DateTime.Now;
                    }

                    TimeSpan span = DateTime.Now - start;
                    double speed = span.Seconds > 0 && totalSent > 0 ? totalSent / span.Seconds : 0.00;
                    RaiseFileReceived(this, new TransferFileEventArgs(FileName, totalSent,
                        (uint)fStream.Length, span, speed, client.ClientID));
                }
            }
            finally
            {
                fStream.Close();
                fStream.Dispose();
                fStream = null;
            }
        }

        #endregion Internal Methods

        #region Events

        #region Private Methods

        private void RaiseFileReceived(object sender, TransferFileEventArgs e)
        {
            if (FileReceived != null)
                FileReceived(this, e);
        }

        #endregion Private Methods

        public event FileReceivedHandler FileReceived;

        #endregion Events
    }
}
