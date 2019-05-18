/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  FTP Wrapper class
 *
 */
using System;
using System.Net;
using System.IO;

#pragma warning disable IDE1005 // Delegate invocation can be simplified
#pragma warning disable IDE1006 // naming rule violation

namespace Shared.Classes
{
    /// <summary>
    /// FTP class
    /// </summary>
    public class ftp
    {
        #region Private Members

        private string _host = null;
        private string _username = null;
        private string _password = null;
        private int _port;
        private bool _useBinary;
        private bool _usePassive;
        private bool _keepAlive;
        private int _bufferSize = 2048;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hostIP"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="bufferSize"></param>
        /// <param name="useBinary"></param>
        /// <param name="usePassive"></param>
        /// <param name="keepAlive"></param>
        /// <param name="port"></param>
        public ftp(string hostIP, string userName, string password,
            int bufferSize = 2048, bool useBinary = true,
            bool usePassive = true, bool keepAlive = true,
            int port = 21)
        {
            _host = hostIP;
            _username = userName;
            _password = password;
            _port = port;

            _bufferSize = bufferSize;

            _keepAlive = keepAlive;
            _useBinary = useBinary;
            _usePassive = usePassive;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Download File
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <param name="localFile"></param>
        public void Download(string remoteFile, string localFile)
        {
            try
            {
                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + remoteFile);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    try
                    {
                        /* Get the FTP Server's Response Stream */
                        Stream ftpStream = ftpResponse.GetResponseStream();
                        try
                        {
                            /* Open a File Stream to Write the Downloaded File */
                            FileStream localFileStream = new FileStream(localFile, FileMode.Create);
                            try
                            {
                                /* Buffer for the Downloaded Data */
                                byte[] byteBuffer = new byte[_bufferSize];

                                int bytesRead = ftpStream.Read(byteBuffer, 0, _bufferSize);
                                /* Download the File by Writing the Buffered Data Until the Transfer is Complete */
                                try
                                {
                                    while (bytesRead > 0)
                                    {
                                        localFileStream.Write(byteBuffer, 0, bytesRead);
                                        bytesRead = ftpStream.Read(byteBuffer, 0, _bufferSize);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            finally
                            {
                                localFileStream.Close();
                                localFileStream.Dispose();
                                localFileStream = null;
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    /* Resource Cleanup */
                    ftpRequest = null;
                }
            }
            catch //(Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Upload File
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <param name="localFile"></param>
        public void Upload(string remoteFile, string localFile)
        {
            try
            {
                /* Open a File Stream to Read the File for Upload */
                FileStream localFileStream = new FileStream(localFile, FileMode.Open);
                try
                {
                    /* Create an FTP Request */
                    FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + remoteFile);
                    try
                    {
                        /* Log in to the FTP Server with the User Name and Password Provided */
                        ftpRequest.Credentials = new NetworkCredential(_username, _password);

                        /* Setup options */
                        ftpRequest.UseBinary = _useBinary;
                        ftpRequest.UsePassive = _usePassive;
                        ftpRequest.KeepAlive = _keepAlive;

                        /* Specify the Type of FTP Request */
                        ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

                        /* Establish Return Communication with the FTP Server */
                        Stream ftpStream = ftpRequest.GetRequestStream();
                        try
                        {
                            /* Buffer for the Downloaded Data */
                            byte[] byteBuffer = new byte[_bufferSize];
                            long fileSize = Utilities.FileSize(localFile);
                            long totalSent = 0;
                            bool cancelled = false;

                            RaiseFileUploadStart(localFile, fileSize);
                            int bytesSent = localFileStream.Read(byteBuffer, 0, _bufferSize);
                            try
                            {
                                /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */
                                while (bytesSent != 0)
                                {
                                    ftpStream.Write(byteBuffer, 0, bytesSent);
                                    bytesSent = localFileStream.Read(byteBuffer, 0, _bufferSize);
                                    totalSent += bytesSent;

                                    if (!RaiseFileUpload(localFile, totalSent))
                                    {
                                        cancelled = true;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            finally
                            {
                                RaiseFileUploadFinish(localFile, totalSent, cancelled);
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        /* Resource Cleanup */
                        ftpRequest = null;
                    }
                }
                finally
                {
                    localFileStream.Close();
                    localFileStream.Dispose();
                    localFileStream = null;
                }
            }
            catch //(Exception ex)
            {
                throw;
            }

            return;
        }

        /// <summary>
        /// Delete File 
        /// </summary>
        /// <param name="deleteFile"></param>
        public void Delete(string deleteFile)
        {
            try
            {
                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ConnectionString() + "/" + deleteFile);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = null;
                    try
                    {
                        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    }
                    finally
                    {
                        /* Resource Cleanup */
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    ftpRequest = null;
                }

            }
            catch //(Exception ex)
            {
                throw;
            }

            return;
        }

        /// <summary>
        /// Rename File
        /// </summary>
        /// <param name="currentFileNameAndPath"></param>
        /// <param name="newFileName"></param>
        public void Rename(string currentFileNameAndPath, string newFileName)
        {
            try
            {
                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ConnectionString() + "/" + currentFileNameAndPath);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.Rename;
                    /* Rename the File */
                    ftpRequest.RenameTo = newFileName;

                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = null;
                    try
                    {
                        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    }
                    finally
                    {
                        /* Resource Cleanup */
                        if (ftpResponse != null)
                        {
                            ftpResponse.Close();
                            ftpResponse = null;
                        }
                    }
                }
                finally
                {
                    ftpRequest = null;
                }
            }
            catch //(Exception ex)
            {
                throw;
            }

            return;
        }

        /// <summary>
        /// Create a New Directory on the FTP Server
        /// </summary>
        /// <param name="newDirectory"></param>
        public void CreateDirectory(string newDirectory)
        {
            try
            {
                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ConnectionString() + "/" + newDirectory);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;


                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = null;
                    try
                    {
                        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    }
                    finally
                    {
                        /* Resource Cleanup */
                        if (ftpResponse != null)
                        {
                            ftpResponse.Close();
                            ftpResponse = null;
                        }
                    }
                }
                finally
                {
                    ftpRequest = null;
                }

            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("550"))
                    throw;
            }

            return;
        }

        /// <summary>
        /// Get the Date/Time a File was Created
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFileCreatedDateTime(string fileName)
        {
            try
            {
                /* Store the Raw Response */
                string fileInfo = null;

                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + fileName);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = null;
                    try
                    {
                        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                        /* Establish Return Communication with the FTP Server */
                        Stream ftpStream = ftpResponse.GetResponseStream();
                        try
                        {
                            /* Get the FTP Server's Response Stream */
                            StreamReader ftpReader = new StreamReader(ftpStream);
                            try
                            {
                                /* Read the Full Response Stream */
                                fileInfo = ftpReader.ReadToEnd();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            finally
                            {
                                /* Resource Cleanup */
                                ftpReader.Close();
                                ftpReader.Dispose();
                                ftpReader = null;
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    ftpRequest = null;
                }

                /* Return File Created Date Time */
                return (fileInfo);
            }
            catch //(Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the Size of a File
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFileSize(string fileName)
        {
            try
            {
                /* Store the Raw Response */
                string fileInfo = null;

                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + fileName);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    try
                    {
                        /* Establish Return Communication with the FTP Server */
                        Stream ftpStream = ftpResponse.GetResponseStream();
                        try
                        {
                            /* Get the FTP Server's Response Stream */
                            StreamReader ftpReader = new StreamReader(ftpStream);
                            try
                            {
                                /* Read the Full Response Stream */
                                try
                                {
                                    while (ftpReader.Peek() != -1)
                                    {
                                        fileInfo = ftpReader.ReadToEnd();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            finally
                            {
                                ftpReader.Close();
                                ftpReader.Dispose();
                                ftpReader = null;
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    /* Resource Cleanup */
                    ftpRequest = null;
                }

                /* Return File Size */
                return (fileInfo);
            }
            catch //(Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// List Directory Contents File/Folder Name Only
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public string[] DirectoryListSimple(string directory)
        {
            try
            {
                /* Store the Raw Response */
                string directoryRaw = null;

                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + directory);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;

                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    try
                    {
                        /* Establish Return Communication with the FTP Server */
                        Stream ftpStream = ftpResponse.GetResponseStream();
                        try
                        {
                            /* Get the FTP Server's Response Stream */
                            StreamReader ftpReader = new StreamReader(ftpStream);
                            try
                            {
                                /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
                                try
                                {
                                    while (ftpReader.Peek() != -1)
                                    {
                                        directoryRaw += ftpReader.ReadLine() + "|";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            finally
                            {
                                ftpReader.Close();
                                ftpReader.Dispose();
                                ftpReader = null;
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    /* Resource Cleanup */
                    ftpRequest = null;
                }

                /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
                try
                {
                    string[] directoryList = directoryRaw.Split("|".ToCharArray());
                    return (directoryList);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch //(Exception ex)
            {
                throw;
            }

            /* Return an Empty string Array if an Exception Occurs */
            return (new string[] { "" });
        }

        /// <summary>
        /// List Directory Contents in Detail (Name, Size, Created, etc.)
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public string[] DirectoryListDetailed(string directory)
        {
            try
            {
                /* Store the Raw Response */
                string directoryRaw = null;

                /* Create an FTP Request */
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ConnectionString() + "/" + directory);
                try
                {
                    /* Log in to the FTP Server with the User Name and Password Provided */
                    ftpRequest.Credentials = new NetworkCredential(_username, _password);

                    /* Setup options */
                    ftpRequest.UseBinary = _useBinary;
                    ftpRequest.UsePassive = _usePassive;
                    ftpRequest.KeepAlive = _keepAlive;

                    /* Specify the Type of FTP Request */
                    ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                    /* Establish Return Communication with the FTP Server */
                    FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                    try
                    {
                        /* Establish Return Communication with the FTP Server */
                        Stream ftpStream = ftpResponse.GetResponseStream();
                        try
                        {
                            /* Get the FTP Server's Response Stream */
                            StreamReader ftpReader = new StreamReader(ftpStream);
                            try
                            {
                                /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
                                try
                                {
                                    while (ftpReader.Peek() != -1)
                                    {
                                        directoryRaw += ftpReader.ReadLine() + "|";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            finally
                            {
                                /* Resource Cleanup */
                                ftpReader.Close();
                                ftpReader.Dispose();
                                ftpReader = null;
                            }
                        }
                        finally
                        {
                            ftpStream.Close();
                            ftpStream.Dispose();
                            ftpStream = null;
                        }
                    }
                    finally
                    {
                        ftpResponse.Close();
                        ftpResponse = null;
                    }
                }
                finally
                {
                    /* Resource Cleanup */
                    ftpRequest = null;
                }

                /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
                try
                {
                    return (directoryRaw.Split("|".ToCharArray()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch //(Exception ex)
            {
                throw;
            }

            /* Return an Empty string Array if an Exception Occurs */
            return (new string[] { "" });
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Builds a valid connection string
        /// </summary>
        /// <returns></returns>
        private string ConnectionString()
        {
            if (!_host.StartsWith("ftp://"))
                _host = String.Format("ftp://{0}", _host);

            return (String.Format("{0}:{1}", _host, _port));
        }

        /// <summary>
        /// File upload start
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        private void RaiseFileUploadStart(string fileName, long fileSize)
        {
            if (FileUploadStart != null)
                FileUploadStart(this, new FileTransferStartArgs(fileName, fileSize));
        }

        /// <summary>
        /// File upload raise event
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="bytesSent"></param>
        /// <returns>true if to continue (not cancelled) otherwise false</returns>
        private bool RaiseFileUpload(string fileName, long bytesSent)
        {
            bool Result = true;

            if (FileUpload != null)
            {
                FileTransferProgressArgs args = new FileTransferProgressArgs(fileName, bytesSent);

                FileUpload(this, args);
                Result = !args.Cancel;
            }

            return (Result);
        }

        /// <summary>
        /// File upload end
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="bytesSent"></param>
        /// <param name="cancelled"></param>
        private void RaiseFileUploadFinish(string fileName, long bytesSent, bool cancelled)
        {
            if (FileUploadEnd != null)
                FileUploadEnd(this, new FileTransferEndArgs(fileName, bytesSent, cancelled));
        }

        #endregion Private Methods

        #region Events

        /// <summary>
        /// File upload event
        /// </summary>
        public event FileTransferStartDelegate FileUploadStart;

        /// <summary>
        /// file Upload Progress event
        /// </summary>
        public event FileTransferProgressDelegate FileUpload;

        /// <summary>
        /// File upload end event
        /// </summary>
        public event FileTransferEndDelegate FileUploadEnd;

        #endregion Events
    }

}
