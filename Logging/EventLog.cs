/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2012 Simon Carter
 *
 *  Purpose:  Event log and Error log management
 *
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib.Zip;

using Shared.Classes;

namespace Shared
{
    /// <summary>
    /// Event logging
    /// </summary>
    public static class EventLog
    {
        #region Private Members

        /// <summary>
        /// object used for obtaining lock for multithreaded use
        /// </summary>
        private static object _lockObject = new object();

        /// <summary>
        /// if error is reported more than once in an hour, ignore previous errors
        /// </summary>
        private static CacheManager _errorCache = new CacheManager("Error Caching", new TimeSpan(1, 0, 0));

        /// <summary>
        /// If a log item repeats more than once in 30 minutes, then ignore
        /// </summary>
        private static CacheManager _logCache = new CacheManager("Log Cache", new TimeSpan(0, 30, 0));

       
        /// <summary>
        /// Maximum size of log/error file, after this is exceeded, nothing will be logged
        /// </summary>
        private static Int64 _maximumFileSize = 10485760; //10 mb


        private static int _maximumReoccuranceCount = 10;

        /// <summary>
        /// Path for log files
        /// </summary>
        private static string _logPath = null;

        /// <summary>
        /// Path for error logs
        /// </summary>
        private static string _errorPath = null;

        #endregion Private Members

        #region Public Properties


        /// <summary>
        /// Archives old log files, older than days
        /// </summary>
        /// <param name="days">Age of file in days</param>
        public static void ArchiveOldLogFiles(int days = 7)
        {
            using (TimedLock.Lock(_lockObject))
            {
                string logPath = Utilities.AddTrailingBackSlash(Path);
                string zipFile = logPath + "Archives.zip";

                string[] files = Directory.GetFiles(logPath, "*.log");
                TimeSpan lastAccessed = new TimeSpan(days * 24, 0, 0);

                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);

                    if ((DateTime.Now - info.LastWriteTime) > lastAccessed)
                    {
                        CompressLogFile(zipFile, file, info.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Maximum size of log/error file
        /// </summary>
        public static Int64 MaximumFileSize
        {
            get
            {
                return (_maximumFileSize);
            }

            set
            {
                _maximumFileSize = value;
            }
        }

        #endregion Public Properties

        #region Internal Static Methods

        internal static void ClearCache()
        {
            _errorCache.CleanCachedItems();
        }

        #endregion Internal Static Methods

        #region Public Static Methods

        /// <summary>
        /// Debug only
        /// </summary>
        /// <param name="header"></param>
        /// <param name="s"></param>
        public static void Debug(string header, string s)
        {
            EventLog.Debug(String.Format("{0} - {1}", header, s));
        }

        /// <summary>
        /// Debug Only
        /// </summary>
        /// <param name="s"></param>
        public static void Debug(string s)
        {
            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    string LogPath = Path.Replace("\\Logs\\", "\\Debug\\");

                    if (!Directory.Exists(LogPath))
                        Directory.CreateDirectory(LogPath);

                    StreamWriter w = File.AppendText(GetLogFileName(LogPath));
                    try
                    {
                        w.WriteLine("{0} {1} - {2}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                            System.Diagnostics.Process.GetCurrentProcess().Id, s);
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }
                }
            }
            catch
            {
                //ignore, I suppose :-\
            }
        }

        /// <summary>
        /// Debug Only
        /// </summary>
        /// <param name="e"></param>
        /// <param name="extraData"></param>
        public static void Debug(Exception e, string extraData = "")
        {
            string Inner = "Unknown";
            string Message = "Unknown";
            string Source = "Unknown";
            string StackTrace = "Unknown";
            string TargetSite = "Unknown";

            if (e != null)
            {
                Inner = e.InnerException == null ? "InnerException is null" : e.InnerException.ToString();
                Message = e.Message;
                Source = e.Source;
                StackTrace = e.StackTrace;
                TargetSite = e.TargetSite == null ? "No Target Site" : e.TargetSite.ToString();
            }

            string Msg = String.Format("Date: {5} {6}\r\n\r\nError Message: {0}\r\n" +
                "\r\nInner Exception: {1}\r\n\r\nSource: {2}\r\n" +
                "\r\nStackTrace: {3}\r\n\r\nTarget Site: {4}\r\n",
                Message, Inner, Source, StackTrace, TargetSite, DateTime.Now.ToString("HH:mm:ss"),
                            DateTime.Now.ToString("dd/MM/yyyy"));

            if (!String.IsNullOrEmpty(extraData))
                Msg += String.Format("{0}\r\n\r\n{1}", Msg, extraData);

            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    string LogPath = Path.Replace("\\Logs\\", "\\Errors\\");

                    if (!Directory.Exists(LogPath))
                        Directory.CreateDirectory(LogPath);

                    StreamWriter w = File.AppendText(GetLogFileName(LogPath));
                    try
                    {
                        w.Write(Msg);
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }
                }
            }
            catch
            {
                //ignore, I suppose :-\
            }
        }


        /// <summary>
        /// Starts the thread log manager to manage log files
        /// </summary>
        /// <param name="maximumAge">Maximum age of log files</param>
        /// <returns>Path to log files</returns>
        public static string Initialise(int maximumAge)
        {
            maximumAge = Utilities.CheckMinMax(maximumAge, 1, 60);

            if (!ThreadManager.Exists("EventLog Thread Manager"))
                ThreadManager.ThreadStart(new Shared.Logging.LoggingThread(maximumAge), 
                    "EventLog Thread Manager", System.Threading.ThreadPriority.Lowest);

            return (Path);
        }

        /// <summary>
        /// Add's an exception to the event log
        /// </summary>
        /// <param name="e">Exception to add to log file</param>
        /// <param name="extraData">extra data to be added along with exception information</param>
        public static void Add(Exception e, string extraData = "")
        {
            if (e == null)
                return;

            using (TimedLock.Lock(_lockObject))
            {
                LoggingErrorCache previousError = null;

                previousError = (LoggingErrorCache)_errorCache.Get(e.Message);

                if (previousError != null)
                {
                    if (!CanLogData(previousError.FileName))
                        return;

                    if (previousError.NumberOfErrors > _maximumReoccuranceCount)
                        return;

                    // if this error has occurred before, then append the date/time to the existing message
                    StreamWriter w = File.AppendText(previousError.FileName);
                    try
                    {
                        if (previousError.NumberOfErrors == 0)
                            w.WriteLine(String.Empty);

                        if (previousError.NumberOfErrors < _maximumReoccuranceCount)
                        {
                            w.WriteLine("Further Occurance: {0}", DateTime.Now.ToString());
                        }
                        else if (previousError.NumberOfErrors == _maximumReoccuranceCount)
                        {
                            w.WriteLine("Maximum Reoccurance Rate Reached - Further reporting on this error will be supressed");
                        }
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }

                    previousError.IncrementErrors();

                    return;
                }
                else
                {
                    if (_errorPath == null)
                    {
                        string dummy = Path;
                    }

                    if (!Directory.Exists(_errorPath))
                        Directory.CreateDirectory(_errorPath);

                    // add this error to the cache
                    previousError = new LoggingErrorCache(e.Message, e.Message);
                    previousError.FileName = _errorPath + String.Format("{0}.log", DateTime.Now.ToString("ddMMyyyy HH mm ss ffff"));

                    if (!CanLogData(previousError.FileName))
                        return;

                    if (!_errorCache.Add(e.Message, previousError))
                        return;
                }

                string Inner = "Unknown";
                string Message = "Unknown";
                string Source = "Unknown";
                string StackTrace = "Unknown";
                string TargetSite = "Unknown";

                if (e != null)
                {
                    Inner = e.InnerException == null ? "InnerException is null" : e.InnerException.ToString();
                    Message = e.Message;
                    Source = e.Source;
                    StackTrace = e.StackTrace;
                    TargetSite = e.TargetSite == null ? "No Target Site" : e.TargetSite.ToString();
                }

                string Msg = String.Format("Date: {5} {6}\r\n\r\nError Message: {0}\r\n" +
                    "\r\nInner Exception: {1}\r\n\r\nSource: {2}\r\n" +
                    "\r\nStackTrace: {3}\r\n\r\nTarget Site: {4}\r\n",
                    Message, Inner, Source, StackTrace, TargetSite, DateTime.Now.ToString("HH:mm:ss"),
                                DateTime.Now.ToString("dd/MM/yyyy"));

                if (!String.IsNullOrEmpty(extraData))
                    Msg = String.Format("{0}\r\n\r\n{1}\r\n", Msg, extraData);

                try
                {
                    StreamWriter w = File.AppendText(previousError.FileName);
                    try
                    {
                        w.Write(Msg);
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }
                }
                catch
                {
                    //ignore, I suppose :-\
                }
            }
        }

        /// <summary>
        /// Adds text to the log file with a header prefixing the text
        /// 
        /// Entry added in form of header - text
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="text">Text to add</param>
        public static void Add(string header, string text)
        {
            Add(String.Format("{0} - {1}", header, text));
        }

        /// <summary>
        /// Adds text to the log file
        /// </summary>
        /// <param name="text">text to add to log file</param>
        public static void Add(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    CacheItem item = _logCache.Get(text);

                    // already cached, don't bother again until item uncached
                    if (item != null)
                        return;

                    _logCache.Add(text, new CacheItem(text, text));
                    
                    string LogPath = Path;

                    if (!Directory.Exists(LogPath))
                        Directory.CreateDirectory(LogPath);

                    string fileName = GetLogFileName(LogPath);

                    if (!CanLogData(fileName))
                        return;
                    
                    StreamWriter w = File.AppendText(fileName);
                    try
                    {
                        w.WriteLine("{0} {1} - {2}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                            System.Diagnostics.Process.GetCurrentProcess().Id, text.Replace("\r\n", " "));
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }
                }
            }
            catch
            {
                //ignore, I suppose :-\
            }
        }

        /// <summary>
        /// Adds text to the log file
        /// </summary>
        /// <param name="text">text to add to log file</param>
        public static void DebugText(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    string LogPath = Path.Replace("\\Logs\\", "\\Debug\\");

                    if (!Directory.Exists(LogPath))
                        Directory.CreateDirectory(LogPath);

                    string fileName = GetLogFileName(LogPath); 

                    if (!CanLogData(fileName))
                        return;

                    StreamWriter w = File.AppendText(fileName);
                    try
                    {
                        w.WriteLine("{0} {1} - {2}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                            System.Diagnostics.Process.GetCurrentProcess().Id, text.Replace("\r\n", " "));
                    }
                    finally
                    {
                        // Update the underlying file.
                        w.Flush();
                        w.Close();
                        w.Dispose();
                        w = null;
                    }
                }
            }
            catch
            {
                //ignore, I suppose :-\
            }
        }

        #endregion Public Static Methods

        #region Public Static Properties

        /// <summary>
        /// Get/Set the path of the event log file.
        /// </summary>
        public static string Path
        {
            get
            {
                if (String.IsNullOrEmpty(_logPath))
                {
                    string Result = XML.GetXMLValue("Settings", "Path");

                    if (!System.IO.Directory.Exists(Result))
                    {
                        Result = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                        Result = System.IO.Path.GetDirectoryName(Result);
                        Result = Result.Substring(6);
                    }

                    Result += "\\Logs\\";

                    _errorPath = Result.Replace("\\Logs\\", "\\Errors\\");

                    _logPath = Result;

                    return (Result);
                }
                else
                {
                    return (_logPath);
                }
            }
        }

        #endregion Public Static Properties

        #region Private Static Methods

        private static bool CanLogData(string file)
        {
            bool Result = true;

            FileInfo info = new FileInfo(file);

            if (File.Exists(file) && info.Length > _maximumFileSize)
                Result = false;

            return (Result);
        }

        private static string GetLogFileName(string path)
        {
            return (String.Format("{0}{1}.log", path, DateTime.Now.ToString("ddMMyyyy")));
        }

        private static void CompressLogFile(string zipFile, string logFile, string logFileName)
        {
            try
            {
                if (File.Exists(zipFile))
                {
                    ZipFile zipArchive = new ZipFile(zipFile);

                    // Must call BeginUpdate to start, and CommitUpdate at the end.
                    zipArchive.BeginUpdate();
                    try
                    {
                        zipArchive.Add(logFile, logFileName);
                    }
                    finally
                    {
                        // Both CommitUpdate and Close must be called.
                        zipArchive.CommitUpdate();
                        zipArchive.Close();
                        zipArchive = null;
                    }
                }
                else
                {
                    byte[] buff;

                    ZipOutputStream zipOut = new ZipOutputStream(File.Open(zipFile, FileMode.OpenOrCreate));
                    try
                    {
                        zipOut.SetLevel(9);

                        ZipEntry entry = new ZipEntry(logFileName);
                        FileStream sReader = File.OpenRead(logFile);
                        try
                        {
                            buff = new byte[Convert.ToInt32(sReader.Length)];
                            sReader.Read(buff, 0, (int)sReader.Length);
                            entry.DateTime = DateTime.Now;
                            entry.Size = sReader.Length;
                        }
                        finally
                        {
                            sReader.Close();
                            sReader.Dispose();
                            sReader = null;
                        }

                        zipOut.PutNextEntry(entry);
                        zipOut.Write(buff, 0, buff.Length);
                    }
                    finally
                    {
                        zipOut.Finish();
                        zipOut.Close();
                        zipOut.Dispose();
                        zipOut = null;
                    }
                }

                //delete backup file
                File.Delete(logFile);
            }
            catch (Exception err)
            {
                if (err.Message.Contains("The file exists"))
                    File.Delete(logFile);
            }

        }

        #endregion Private Static Methods
    }
}
