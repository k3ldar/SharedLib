/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  Class for reading MailEnable and FileZilla log files
 *
 */
using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable IDE1005 // Delegate invocation can be simplified

namespace Shared.Classes
{
    /// <summary>
    /// Scans a log file
    /// </summary>
    public sealed class LogScanner
    {
        #region Private Members

        private readonly List<LogLine> _logEntries = new List<LogLine>();

        private readonly long _lastPosition;

        private readonly string _fileName;

        #endregion Private Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logFileType">Type of log file</param>
        /// <param name="fileName">Log file name and path</param>
        /// <param name="lastPosition">Last position scanned within the file</param>
        public LogScanner(LogFileType logFileType, string fileName, long lastPosition)
        {
            if (!File.Exists(fileName))
                throw new ArgumentException("File does not exist {0}", fileName);

            _fileName = fileName;
            FileType = logFileType;
            _lastPosition = lastPosition;
        }

        #region Properties

        /// <summary>
        /// Type of log file
        /// </summary>
        public LogFileType FileType { get; private set; }

        /// <summary>
        /// List of log entries
        /// </summary>
        public List<LogLine> LogEntries
        {
            get
            {
                return _logEntries;
            }
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Begins processing of log file
        /// </summary>
        /// <returns>Position reached within the file</returns>
        public long ProcessEntries()
        {
            long Result = 0;

            switch (FileType)
            {
                case LogFileType.MailEnable:
                    Result = ProcessMailEnableFile(_fileName, true);
                    break;

                case LogFileType.FileZilla:
                    Result = ProcessFileZillaFile(_fileName, true);
                    break;

                default:
                    throw new Exception("Invalid File Type");
            }

            return Result;
        }

        #endregion Public Methods

        #region Private Methods

        private long ProcessMailEnableFile(string fileName, bool loginFailuresOnly)
        {
            long Result = 0;

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try
            {
                StreamReader rdr = new StreamReader(fs);
                try
                {
                    rdr.BaseStream.Position = _lastPosition;
                    string text = rdr.ReadToEnd();
                    string[] lines = text.Split('\n');

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#") || String.IsNullOrEmpty(line.Trim()))
                            continue;

                        string[] parts = line.Trim().Split(' ');

                        if (parts.Length < 13)
                            continue;

                        if ((!loginFailuresOnly) || (loginFailuresOnly && parts[9] == "504+Invalid+Username+or+Password"))
                        {
                            LogLine logLine = new LogLine(LogFileType.MailEnable, parts[0], parts[1],
                                parts[2], parts[3], parts[4], parts[5], parts[6], parts[7], parts[8].Replace("+", " "), parts[9],
                                parts[10], parts[11], parts[12], parts[13]);
                            _logEntries.Add(logLine);
                            RaiseOnLineFound(logLine);
                        }
                    }
                }
                finally
                {
                    Result = rdr.BaseStream.Position;
                    rdr.Close();
                    rdr.Dispose();
                    rdr = null;
                }
            }
            finally
            {
                fs.Close();
                fs.Dispose();
                fs = null;
            }

            return Result;
        }

        private long ProcessFileZillaFile(string fileName, bool loginFailuresOnly)
        {
            long Result = 0;

            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try
            {
                StreamReader rdr = new StreamReader(fs);
                try
                {
                    rdr.BaseStream.Position = _lastPosition;
                    string text = rdr.ReadToEnd();
                    string[] lines = text.Split('\n');

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#") || String.IsNullOrEmpty(line.Trim()))
                            continue;

                        string[] parts = line.Replace(")>", "").Replace("(", "").Replace(")", "").Trim().Split(' ');

                        if (parts.Length < 13)
                            continue;

                        if ((!loginFailuresOnly) || loginFailuresOnly && parts[9] == "530" && parts[13] == "incorrect!")
                        {
                            LogLine logLine = new LogLine(LogFileType.FileZilla, parts[1], parts[2], parts[8]);
                            _logEntries.Add(logLine);
                            RaiseOnLineFound(logLine);
                        }
                    }
                }
                finally
                {
                    Result = rdr.BaseStream.Position;
                    rdr.Close();
                    rdr.Dispose();
                    rdr = null;
                }
            }
            finally
            {
                fs.Close();
                fs.Dispose();
                fs = null;
            }

            return Result;
        }

        #region Event Wrappers

        private void RaiseOnLineFound(LogLine line)
        {
            if (OnLineFound != null)
                OnLineFound(this, new LogScannerArgs(_fileName, line));
        }

        #endregion Event Wrappers

        #endregion Private Methods

        #region Events

        /// <summary>
        /// Event raised when a new line entry is found
        /// </summary>
        public LogScannerDelegate OnLineFound;

        #endregion Events
    }

    /// <summary>
    /// Type of log file
    /// </summary>
    public enum LogFileType
    {
        /// <summary>
        /// MailEnable log file
        /// </summary>
        MailEnable,

        /// <summary>
        /// FileZilla Server Log Files
        /// </summary>
        FileZilla
    }

    /// <summary>
    /// Valid line from a log file
    /// </summary>
    public sealed class LogLine
    {
        #region Constructors

        /// <summary>
        /// Constructor for mailenable
        /// </summary>
        /// <param name="logFileType"></param>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="remoteIP"></param>
        /// <param name="agent"></param>
        /// <param name="account"></param>
        /// <param name="serverIPAddress"></param>
        /// <param name="serverPort"></param>
        /// <param name="method"></param>
        /// <param name="uRIStem"></param>
        /// <param name="uRIQuery"></param>
        /// <param name="serverName"></param>
        /// <param name="serverBytes"></param>
        /// <param name="clientBytes"></param>
        /// <param name="userName"></param>
        public LogLine(LogFileType logFileType, string date, string time,
            string remoteIP, string agent, string account, string serverIPAddress,
            string serverPort, string method, string uRIStem, string uRIQuery,
            string serverName, string serverBytes, string clientBytes, string userName)
        {
            if (logFileType != LogFileType.MailEnable)
                throw new ArgumentException();

            string[] dateParts = date.Split('-');
            string[] timeParts = time.Split(':');
            DateTime = new DateTime(Utilities.StrToInt(dateParts[0], 1),
                Utilities.StrToInt(dateParts[1], 1), Utilities.StrToInt(dateParts[2], 1),
                Utilities.StrToInt(timeParts[0], 1), Utilities.StrToInt(timeParts[1], 1),
                Utilities.StrToInt(timeParts[2], 1));

            RemoteIP = remoteIP;
            Agent = agent;
            Account = account;
            ServerIPAddress = serverIPAddress;
            ServerPort = Utilities.StrToUInt(serverPort, 0);
            Method = method;
            URIStem = uRIStem;
            URIQuery = uRIQuery;
            ServerName = serverName;
            ServerBytes = Utilities.StrToUInt(serverBytes, 0);
            ClientBytes = Utilities.StrToUInt(clientBytes, 0);
            Username = userName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LogLine(LogFileType logFileType, string date, string time, string remoteIP)
        {
            if (logFileType != LogFileType.FileZilla)
                throw new ArgumentException();

            string[] dateParts = date.Split('/');
            string[] timeParts = time.Split(':');
            DateTime = new DateTime(Utilities.StrToInt(dateParts[2], 1),
                Utilities.StrToInt(dateParts[0], 1), Utilities.StrToInt(dateParts[1], 1),
                Utilities.StrToInt(timeParts[0], 1), Utilities.StrToInt(timeParts[1], 1),
                Utilities.StrToInt(timeParts[2], 1));
            RemoteIP = remoteIP;
        }

        #endregion Constructors

        #region Properties

        // #Fields: date time c-ip agent account s-ip s-port cs-method cs-uristem cs-uriquery s-computername sc-bytes cs-bytes cs-username

        /// <summary>
        /// Log entry date/time
        /// </summary>
        public DateTime DateTime { get; private set; }

        /// <summary>
        /// Remote IP Address
        /// </summary>
        public string RemoteIP { get; private set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string Agent { get; private set; }

        /// <summary>
        /// Account
        /// </summary>
        public string Account { get; private set; }

        /// <summary>
        /// Server IP Address
        /// </summary>
        public string ServerIPAddress { get; private set; }

        /// <summary>
        /// Server Port
        /// </summary>
        public uint ServerPort { get; private set; }

        /// <summary>
        /// Method
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// URIStem
        /// </summary>
        public string URIStem { get; private set; }

        /// <summary>
        /// URIQuery
        /// </summary>
        public string URIQuery { get; private set; }

        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// Server Bytes
        /// </summary>
        public uint ServerBytes { get; private set; }

        /// <summary>
        /// Client Bytes
        /// </summary>
        public uint ClientBytes { get; private set; }

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; private set; }

        #endregion Properties
    }
}
