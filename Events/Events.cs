/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Event Arguments and Delegates
 *
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Shared.Classes;

namespace Shared
{
    /// <summary>
    /// Cancel Event Handler Arguments
    /// </summary>
    public sealed class CancelArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CancelArgs()
        {
            Cancel = false;
        }

        /// <summary>
        /// Cancel Flag
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Cancel Event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CancelEventHandler(object sender, CancelArgs e);


    /// <summary>
    /// Schema Validation, event raised when schema is mismatched between 2 databases
    /// </summary>
    public sealed class SchemaValidationArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SchemaValidationArgs(string objectType, string objectName1, string objectName2, 
            string message, string sql, bool existDifferentName)
        {
            ObjectType = objectType;
            ObjectName1 = objectName1;
            ObjectName2 = objectName2;
            ExistDifferentName = existDifferentName;
            Message = message;
            SQL = sql;
        }


        /// <summary>
        /// Type of Object
        /// </summary>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Name of Object 1
        /// </summary>
        public string ObjectName1 { get; private set; }

        /// <summary>
        /// Name of Object 2
        /// </summary>
        public string ObjectName2 { get; private set; }

        /// <summary>
        /// Similar object with different name found
        /// </summary>
        public bool ExistDifferentName { get; private set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// SQL
        /// </summary>
        public string SQL { get; private set; }
    }

    /// <summary>
    /// Schema Validation Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SchemaValidationHandler(object sender, SchemaValidationArgs e);

    /// <summary>
    /// Toast Notification Arguments
    /// </summary>
    public sealed class ToastNotificationArgs: EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <param name="eventType"></param>
        public ToastNotificationArgs(string uniqueID, ToastEventType eventType)
        {
            UniqueID = uniqueID;
            EventType = eventType;
        }

        /// <summary>
        /// Unique ID
        /// </summary>
        public string UniqueID { get; private set; }

        /// <summary>
        /// Toast Event Type
        /// </summary>
        public ToastEventType EventType { get; private set; }
    }

    /// <summary>
    /// Toast Notification arguments
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ToastNotificationHandler(object sender, ToastNotificationArgs e);

    /// <summary>
    /// File Progress Args
    /// </summary>
    public class FileProgressArgs: EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="total"></param>
        /// <param name="sent"></param>
        public FileProgressArgs(string fileName, int total, int sent)
        {
            Filename = fileName;
            Total = total;
            Sent = sent;
        }

        /// <summary>
        /// Filename
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Total File Size
        /// </summary>
        public int Total { get; private set; }

        /// <summary>
        /// Bytes sent
        /// </summary>
        public int Sent { get; set; }
    }

    /// <summary>
    /// File Progress Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileProgressHandler(object sender, FileProgressArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class AddToLogFileArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public AddToLogFileArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Delegate for log file message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void AddToLogFileHandler(object sender, AddToLogFileArgs e);

    /// <summary>
    /// Tooltip Event Arguments
    /// </summary>
    public class ToolTipEventArgs : EventArgs
    {
        #region Private Members

        #endregion Private Members

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listViewItem"></param>
        public ToolTipEventArgs(ListViewItem listViewItem)
            : this()
        {
            ListViewItem = listViewItem;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ToolTipEventArgs()
        {
            AllowShow = true;
            ShowBaloon = false;
            Icon = ToolTipIcon.None;
            Title = String.Empty;
            Text = String.Empty;
            ListViewItem = null;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Determines if balloon type hint is shown
        /// </summary>
        public bool ShowBaloon { get; set; }

        /// <summary>
        /// Title of tooltip
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Tooltip text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Icon to be shown with tooltip
        /// </summary>
        public ToolTipIcon Icon { get; set; }

        /// <summary>
        /// Determines if a tooltip is shown
        /// </summary>
        public bool AllowShow { get; set; }

        /// <summary>
        /// List view item
        /// </summary>
        public ListViewItem ListViewItem { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// Tooltip event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ToolTipEventHandler(object sender, ToolTipEventArgs e);

    /// <summary>
    /// Event arguments for User Session
    /// </summary>
    public class UserSessionArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="session">UserSession class</param>
        public UserSessionArgs(UserSession session)
        {
            Session = session;
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// User Session
        /// </summary>
        public UserSession Session { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// User Session Event Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UserSessionHandler(object sender, UserSessionArgs e);


    /// <summary>
    /// Event arguments for User Session
    /// </summary>
    public class UserSessionRequiredArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sessionID">UserSession class</param>
        public UserSessionRequiredArgs(string sessionID)
        {
            SessionID = sessionID;
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// unique Session ID
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// User Session
        /// </summary>
        public UserSession Session { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// User Session Event Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UserSessionRequiredHandler(object sender, UserSessionRequiredArgs e);






    /// <summary>
    /// Event arguments for User Page View
    /// </summary>
    public class UserPageViewArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="session">UserSession class</param>
        /// <param name="page"></param>
        public UserPageViewArgs(UserSession session, PageViewData page)
        {
            Page = page;
            Session = session;
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// User Page View
        /// </summary>
        public PageViewData Page { get; private set; }

        /// <summary>
        /// User Session
        /// </summary>
        public UserSession Session { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// User Session Event Handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UserPageViewHandler(object sender, UserPageViewArgs e);






    /// <summary>
    /// Event arguments for User Session
    /// </summary>
    public class IpAddressArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ipAddress">IP Address whose details are required</param>
        public IpAddressArgs(string ipAddress)
        {
            IPAddress = ipAddress;
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// IP Address that data is required for
        /// </summary>
        public string IPAddress { get; private set; }

        /// <summary>
        /// Country for visitor
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Visitor Region
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Visitor city
        /// </summary>
        public string CityName { get; set; }

        /// <summary>
        /// Latitude for ip address
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Longitude for ip address
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Unique ID, if used, for the IP Address information
        /// </summary>
        public Int64 IPUniqueID { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// Event Handler for obtaining ip Address
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void IpAddressHandler(object sender, IpAddressArgs e);



    /// <summary>
    /// Ftp File upload / download start
    /// </summary>
    public class FileTransferStartArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">File being transferred</param>
        /// <param name="fileSize">Size of file</param>
        public FileTransferStartArgs(string fileName, long fileSize)
        {
            Filename = fileName;
            FileSize = fileSize;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of file being transferred
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Size of the file being transferred
        /// </summary>
        public long FileSize { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileTransferStartDelegate(object sender, FileTransferStartArgs e);

    /// <summary>
    /// Ftp File upload / download start
    /// </summary>
    public class FileTransferProgressArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">File being transferred</param>
        /// <param name="bytesSent">Size of file</param>
        public FileTransferProgressArgs(string fileName, long bytesSent)
        {
            Filename = fileName;
            BytesSent = bytesSent;
            Cancel = false;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of file being transferred
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Size of the file being transferred
        /// </summary>
        public long BytesSent { get; private set; }

        /// <summary>
        /// Determines wether the operation was cancelled or not
        /// </summary>
        public bool Cancel { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileTransferProgressDelegate(object sender, FileTransferProgressArgs e);

    /// <summary>
    /// Ftp File upload / download end
    /// </summary>
    public class FileTransferEndArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">File being transferred</param>
        /// <param name="fileSize">Size of file</param>
        /// <param name="cancelled"></param>
        public FileTransferEndArgs(string fileName, long fileSize, bool cancelled)
        {
            Filename = fileName;
            FileSize = fileSize;
            Cancelled = cancelled;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of file being transferred
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Size of the file being transferred
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Determines wether file transfer was cancelled or not
        /// </summary>
        public bool Cancelled { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FileTransferEndDelegate(object sender, FileTransferEndArgs e);

    /// <summary>
    /// cached item not found event args
    /// </summary>
    public class CacheItemNotFoundArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of cached item</param>
        public CacheItemNotFoundArgs (string name)
        {
            Name = name;
            CachedItem = null;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of cached item
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// New cached item, to be added to the list
        /// </summary>
        public CacheItem CachedItem { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// Event raised when a cached item is not found, providing a chance to get it and add it to the list
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CacheItemNotFoundDelegate(object sender, CacheItemNotFoundArgs e);

    /// <summary>
    /// cached item not found event args
    /// </summary>
    public class CacheItemArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">Name of cached item</param>
        public CacheItemArgs(CacheItem item)
        {
            CachedItem = item;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Cached item, to be added to the list
        /// </summary>
        public CacheItem CachedItem { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Event raised for a cached item 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CacheItemDelegate(object sender, CacheItemArgs e);

    /// <summary>
    /// cached item not found event args
    /// </summary>
    public class LogScannerArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="line"></param>
        public LogScannerArgs(string fileName, LogLine line)
        {
            FileName = fileName;
            Line = line;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// File where entry generated
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Cached item, to be added to the list
        /// </summary>
        public LogLine Line { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Event raised for a cached item 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LogScannerDelegate(object sender, LogScannerArgs e);

    /// <summary>
    /// Price column override event arguments
    /// </summary>
    public class PriceColumnOverrideArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="session">Current Web Session</param>
        /// <param name="request">Current WebRequest</param>
        /// <param name="priceColumn">Current Price Column</param>
        /// <param name="webSessionID">Web Session ID</param>
        /// <param name="allowOverride">Indicates wether the price column is overridden or not</param>
        /// <param name="userSession">Current User Session</param>
        public PriceColumnOverrideArgs(System.Web.SessionState.HttpSessionState session,
            System.Web.HttpRequest request, int priceColumn, string webSessionID, bool allowOverride,
            UserSession userSession)
        {
            PriceColumn = priceColumn;
            OverridePriceColumn = allowOverride;
            Session = session;
            Request = request;
            WebSessionID = webSessionID;
            UserSession = userSession;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Current Users Session
        /// </summary>
        public System.Web.SessionState.HttpSessionState Session { get; private set; }

        /// <summary>
        /// Http Request object
        /// </summary>
        public System.Web.HttpRequest Request { get; private set; }

        /// <summary>
        /// Indicates that the price column should be overridden
        /// </summary>
        public bool OverridePriceColumn { get; set; }

        /// <summary>
        /// Price column to be used
        /// </summary>
        public int PriceColumn { get; set; }

        /// <summary>
        /// Users web session ID
        /// </summary>
        public string WebSessionID { get; set; }

        /// <summary>
        /// User Session for current user
        /// </summary>
        public UserSession UserSession { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// Price column delegate
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void PriceColumnOverrideDelegate(object sender, PriceColumnOverrideArgs e);

    /// <summary>
    /// Settings load arguments
    /// </summary>
    public sealed class SettingsLoadArgs
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">Parent Node</param>
        public SettingsLoadArgs(TreeNode parent)
        {
            Parent = parent;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Parent Node
        /// </summary>
        public TreeNode Parent { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for loading settings
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SettingsLoadDelegate(object sender, SettingsLoadArgs e);

    /// <summary>
    /// Update available event arguments
    /// </summary>
    public sealed class UpdateAvailableArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="productName">Name of product</param>
        /// <param name="updateAvailable">Indicates wether an update is available or not</param>
        public UpdateAvailableArgs(string productName, bool updateAvailable)
        {
            ProductName = productName;
            UpdateAvailable = updateAvailable;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Bool value which indicates wether an update is available or not
        /// </summary>
        public bool UpdateAvailable { get; private set; }

        /// <summary>
        /// Name of product
        /// </summary>
        public string ProductName { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for update available event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UpdateAvailableDelegate(object sender, UpdateAvailableArgs e);

    /// <summary>
    /// Thread manager event arguments
    /// </summary>
    public sealed class ThreadManagerEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threadManager">ThreadManager instance</param>
        public ThreadManagerEventArgs(ThreadManager threadManager)
        {
            Thread = threadManager;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ThreadManager object
        /// </summary>
        public ThreadManager Thread { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate used in ThreadManager events
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ThreadManagerEventDelegate(object sender, ThreadManagerEventArgs e);

    /// <summary>
    /// Thread manager exception event arguments
    /// </summary>
    public sealed class ThreadManagerExceptionEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threadManager">ThreadManager instance</param>
        /// <param name="error">Exception that was raised</param>
        public ThreadManagerExceptionEventArgs(ThreadManager threadManager, Exception error)
        {
            Thread = threadManager;
            Error = error;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ThreadManager object
        /// </summary>
        public ThreadManager Thread { get; private set; }

        /// <summary>
        /// Error being raised
        /// </summary>
        public Exception Error { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate used in ThreadManager exception events
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ThreadManagerExceptionEventDelegate(object sender, ThreadManagerExceptionEventArgs e);

    /// <summary>
    /// Update available event arguments
    /// </summary>
    public sealed class LogScannedArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">Name of product</param>
        /// <param name="position">Indicates wether an update is available or not</param>
        public LogScannedArgs(string fileName, long position)
        {
            Filename = fileName;
            Position = position;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of file being scanned
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Position within file that scanning starts / ends
        /// </summary>
        public long Position { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Delegate for update available event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LogScannedDelegate(object sender, LogScannedArgs e);
}
