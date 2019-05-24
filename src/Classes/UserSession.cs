/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2015 - 2017 Simon Carter
 *
 *  Purpose:  Session Manager - Used to manage web sessions
 *
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#pragma warning disable IDE1005 // Delegate invocation can be simplified
#pragma warning disable IDE0029 // simplified null checks

namespace Shared.Classes
{
    /// <summary>
    /// UserSessionManager
    /// 
    /// Class to manage user web sessions
    /// </summary>
    [Serializable]
    public class UserSessionManager : ThreadManager, IDisposable
    {
        #region Private Members

        private static readonly string[] knownBots = { ".co.uk/bot", ".com/bot", ".net/bot", "googlebot", "bingpreview", 
                "/bingbot.htm", "ahrefs.com", "yandex.com", "semrushbot",
                "google.com/bot.html", "baidu.com/search/spider.html", "baidu.", "girafabot", 
                "buzzbot", "experibot", "livelapbot", "mediatoolkitbot", "stashbot", "applebot", "tweetmemebot", 
                "leikibot", "rogerbot", "msnbot", "istellabot", "vebot", "uxcrawlerbot", "twitterbot", 
                "socialrankiobot", "safednsbot", "yandexmobilebot", "laserlikebot", "yoozbot", "spbot", "obot", 
                "linkdexbot", "aihitbot", "yandexmetrika", "yandexbot", "tt_snbreact_bot", "uptimebot", "veoozbot", 
                "linkisbot", "mail.ru_bot", "mj12bot", "paperlibot", "seokicks-robot", "semrushbot", "seznambot", 
                "dotbot", "duckduckgo", "everyonesocialbot", "exabot", "thefind.com/crawler", "fatbot", "bot@linkfluence.net", 
                "http://www.trendiction.de/bot", "+http://duckduckgo.com", "leikibot", "surveybot", "trendictionbot", 
                "blexbot", "cliqzbot", ") webmonitor", "sieradelta", "sdbot", "mojeekbot", ".ly/bot",
                "onpagebot", "surdotlybot", "favico.be/bot", "companiesintheuk.co.uk/bot", ".k39.us/bot",
                "seozoom.it/bot"};

        private static readonly UserSessionManager _sessionManager = new UserSessionManager();

        private static readonly object _sessionLockObject = new object();

        private static List<UserSession> _userSessions = new List<UserSession>();

        private static readonly object _tempLockObject = new object();

        private static List<UserSession> _tempUserSessions = new List<UserSession>();

        private static CacheManager _userSessionCacheManager = null;

        private static Int64 _loopCounter = Int64.MinValue;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public UserSessionManager()
            :base(null, new TimeSpan(0, 0, 5))
        {
            this.HangTimeout = 0;
            Classes.ThreadManager.ThreadStart(this, "UserSessionManager", 
                System.Threading.ThreadPriority.Lowest);
        }

        #endregion Constructors

        #region Overridden Methods

        /// <summary>
        /// Thread run method
        /// </summary>
        /// <param name="parameters">Thread parameters</param>
        /// <returns>true if execution should continue, otherwise false</returns>
        protected override bool Run(object parameters)
        {
            // move sessions from temp storage to processing storage
            using (TimedLock.Lock(_tempLockObject))
            {
                for (int i = _tempUserSessions.Count -1; i >= 0; i--)
                {
                    using (TimedLock.Lock(_sessionLockObject))
                    {
                        _userSessions.Add(_tempUserSessions[i]);
                        System.Threading.Thread.Sleep(0);
                    }

                    _tempUserSessions.RemoveAt(i);
                }
            }

            using (TimedLock.Lock(_sessionLockObject))
            {
                for (int i = _userSessions.Count -1; i >= 0; i--)
                {
                    UserSession session = _userSessions[i];

                    switch (session.Status)
                    {
                        case SessionStatus.Updated:
                            _userSessions.Remove(session);
                            break;
                        case SessionStatus.Initialising:
                            if (InitialiseWebsite)
                                InitialiseSession(session);

                            _userSessions.Remove(session);
                            break;
                        case SessionStatus.Closing:
                            FinaliseSession(session);
                            _userSessions.Remove(session);
                            session.Dispose();
                            session = null;
                            break;
                    }
                }
            }

            if (_loopCounter % 3 == 0)
            {
                if (OnSessionSave != null && UserSessions != null)
                {
                    foreach (CacheItem item in UserSessions.Items)
                    {
                        UserSession session = (UserSession)item.GetValue(true);

                        // every 15 seconds or so save the data
                        using (TimedLock.Lock(_sessionLockObject))
                        {
                            using (TimedLock.Lock(session))
                            {
                                if (session.SaveStatus == SaveStatus.RequiresSave || session.PageSaveStatus == SaveStatus.RequiresSave)
                                {
                                    RaiseSessionSave(session);
                                    session.PageSaveStatus = SaveStatus.Saved;
                                }
                            }

                        }

                        System.Threading.Thread.Sleep(0);
                    }
                }

                if (_loopCounter > Int64.MaxValue - 50)
                    _loopCounter = Int64.MinValue;
            }

            _loopCounter++;

            return (!this.HasCancelled());
        }

        /// <summary>
        /// Thread Cancel Method
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="isUnResponsive"></param>
        public override void CancelThread(int timeout = 10000, bool isUnResponsive = false)
        {
            base.CancelThread(timeout, isUnResponsive);
        }

        #endregion Overridden Methods

        #region Public Methods

        /// <summary>
        /// Raises the sesssion created event for any listners
        /// </summary>
        /// <param name="session"></param>
        public void SessionCreated(UserSession session)
        {
            RaiseSessionCreated(session);
        }

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
#if DEBUG
            System.GC.SuppressFinalize(this);
#endif
            using (TimedLock.Lock(_sessionLockObject))
            {
                List<CacheItem> allSessions = _userSessionCacheManager.Items;

                for (int i = allSessions.Count - 1; i >= 0; i--)
                {
                    UserSession session = (UserSession)allSessions[i].Value;

                    FinaliseSession(session);
                    session.Dispose();
                    session = null;
                }
            }
        }

        /// <summary>
        /// Called in a seperate thread, updates thread with basic data to stop blocking
        /// </summary>
        /// <param name="session"></param>
        public void InitialiseSession(UserSession session)
        {
            if (session == null)
                return;

            try
            {
                RaiseGetIPDetails(session);
                
                // is it a bot
                session.IsBot = CheckIfBot(session);

                // referral type
                session.Referal = GetReferralType(session);

                session.SaveStatus = SaveStatus.RequiresSave;

                RaiseSessionCreated(session);
            }
            catch (Exception err)
            {
                EventLog.Add(err);
                throw;
            }
            finally
            {
                session.Status = SessionStatus.Updated;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Determines the Referal Type for the sesssion
        /// </summary>
        /// <param name="session">Session</param>
        /// <returns>Referral Type</returns>
        private ReferalType GetReferralType(UserSession session)
        {
            if (String.IsNullOrEmpty(session.InitialReferrer))
                return (ReferalType.Direct);

            Uri uri = new Uri(session.InitialReferrer.ToLower());

            string referrer = uri.Host;

            //Direct, Organic, Referal, or site specific
            switch (referrer)
            {
                case "t.co":
                    return (ReferalType.Twitter);

                case "m.facebook.com":
                case "facebook.com":
                case "l.facebook.com":
                case "lm.facebook.com":
                case "fb.me":
                    return (ReferalType.Facebook);
               
                case "r.search.yahoo.com":
                case "uk.search.yahoo.com":
                case "search.yahoo.com":
                    return (ReferalType.Yahoo);

                case "www.bing.com":
                case "m.bing.com":
                    return (ReferalType.Bing);

                // other search engines
                case "m.baidu.com":
                case "www.baidu.com":
                case "www.ask.com":
                case "ask.com":
                case "yandex.ru":
                    return (ReferalType.Organic);

                // anything else is a referral
                default:
                    if (referrer.Contains("google"))
                        return (ReferalType.Google);
                    else
                        return (ReferalType.Referal);
            }
        }

        /// <summary>
        /// Session is closing, are we saving any data from it?
        /// </summary>
        /// <param name="session">Session being removed</param>
        private void FinaliseSession(UserSession session)
        {
            try
            {
                if (session.SaveStatus == SaveStatus.Pending)
                    session.SaveStatus = SaveStatus.RequiresSave;

                foreach (PageViewData page in session.Pages)
                {
                    if (page.SaveStatus == SaveStatus.Pending)
                        page.SaveStatus = SaveStatus.RequiresSave;

                    System.Threading.Thread.Sleep(0);
                }

                RaiseSessionClosing(session);
            }
            catch (Exception err)
            {
                EventLog.Add(err);
                throw;
            }
            finally
            {
                session.Status = SessionStatus.Updated;
            }
        }

        private bool CheckIfBot(UserSession session)
        {
            if (session == null || String.IsNullOrEmpty(session.UserAgent))
                return (false);

            string agent = session.UserAgent.ToLower();

            foreach (string s in knownBots)
            {
                if (agent.Contains(s))
                    return (true);
            }

            return (false);
        }

        #region Event Wrappers

        /// <summary>
        /// Raises the session created event
        /// </summary>
        /// <param name="session">Session being created</param>
        private void RaiseSessionCreated(UserSession session)
        {
            if (OnSessionCreated != null)
                OnSessionCreated(this, new UserSessionArgs(session));
        }

        /// <summary>
        /// Raises the sesssion closing method
        /// </summary>
        /// <param name="session">Session being closed</param>
        internal void RaiseSessionClosing(UserSession session)
        {
            if (OnSessionClosing != null)
                OnSessionClosing(this, new UserSessionArgs(session));
        }

        /// <summary>
        /// Raises the session save method
        /// </summary>
        /// <param name="session">Session to be saved</param>
        internal void RaiseSessionSave(UserSession session)
        {
            if (OnSessionSave != null)
                OnSessionSave(this, new UserSessionArgs(session));
        }


        internal UserSession RaiseSessionRequired(string sessionID)
        {
            if (OnSessionRetrieve != null)
            {
                UserSessionRequiredArgs args = new UserSessionRequiredArgs(sessionID);
                OnSessionRetrieve(this, args);
                return (args.Session);
            }

            return (null);
        }

        /// <summary>
        /// Raises an event to save the page data
        /// </summary>
        /// <param name="session"></param>
        /// <param name="page"></param>
        internal void RaiseSavePage(UserSession session, PageViewData page)
        {
            if (OnSavePage != null && page.SaveStatus == SaveStatus.RequiresSave)
            {
                OnSavePage(this, new UserPageViewArgs(session, page));
            }
        }

        /// <summary>
        /// Raisess an event to obtain ip details 
        /// </summary>
        /// <param name="session">Session who's ip details need raising</param>
        private void RaiseGetIPDetails(UserSession session)
        {
            if (IPAddressDetails != null)
            {
                IpAddressArgs args = new IpAddressArgs(session.IPAddress);

                IPAddressDetails(this, args);

                session.UpdateIPDetails(args.IPUniqueID, args.Latitude, args.Longitude, 
                    args.Region, args.CityName, args.CountryCode);
            }
        }

        #endregion Event Wrappers

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public static void UpdateSession(UserSession session)
        {
            if (session.Status == SessionStatus.Updated)
                return;

            using (TimedLock.Lock(_tempLockObject))
            {
                _tempUserSessions.Add(session);

                if (session.Status == SessionStatus.Continuing)
                    session.Status = SessionStatus.Initialising;
            }
        }

        #endregion Internal Methods

        #region Static Methods

        /// <summary>
        /// Initialises the SessionManager
        /// </summary>
        /// <param name="sessionDuration">Duration of session</param>
        public static void InitialiseSessionManager(TimeSpan sessionDuration)
        {
            if (_userSessionCacheManager == null)
            {
                _userSessionCacheManager = new CacheManager("Web User Sessions", sessionDuration, true, false);
                _userSessionCacheManager.ItemRemoved += _userSessionManger_ItemRemoved;
                _userSessionCacheManager.ItemNotFound += _userSessionCacheManager_ItemNotFound;
            }
        }

        /// <summary>
        /// Add's a new session to the Session Manager
        /// </summary>
        /// <param name="session"></param>
        public static void Add(UserSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (_userSessionCacheManager == null)
                throw new Exception("SessionManager has not been initialised!");

            _userSessionCacheManager.Add(session.SessionID, 
                new CacheItem(session.SessionID, session));
        }

        /// <summary>
        /// Called after a user logs in to update the username and email for the live session
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="username">Visitors Name</param>
        /// <param name="email">Visitors Email Address</param>
        /// <param name="userID">ID of current user</param>
        public static void Login(string sessionID, string username, string email, Int64 userID)
        {
            CacheItem cachedItem = _userSessionCacheManager.Get(sessionID);

            if (cachedItem != null)
            {
                UserSession session = (UserSession)cachedItem.Value;
                session.UserName = username;
                session.UserEmail = email;
                session.UserID = userID;
                session.SaveStatus = SaveStatus.RequiresSave;
            }
        }

        /// <summary>
        /// Event called when a user session is removed from cache for inactivity etc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _userSessionManger_ItemRemoved(object sender, Shared.CacheItemArgs e)
        {
            //save statistics on user session
            if (e.CachedItem == null || e.CachedItem.Value == null)
                return;

            UserSession session = (UserSession)e.CachedItem.Value;
            session.Status = SessionStatus.Closing;
            UpdateSession(session);
        }

        static void _userSessionCacheManager_ItemNotFound(object sender, CacheItemNotFoundArgs e)
        {
            if (Instance != null)
            { 
                UserSession session = Instance.RaiseSessionRequired(e.Name);

                if (session != null)
                {
                    e.CachedItem = new CacheItem(e.Name, session);
                }
            }
        }

        #endregion Static Methods

        #region Static Properties

        /// <summary>
        /// Returns a cloned list of all user sessions
        /// </summary>
        public static List<UserSession> Clone
        {
            get
            {
                List<UserSession> Result = new List<UserSession>();

                using (TimedLock.Lock(_sessionLockObject))
                {
                    foreach (CacheItem item in _userSessionCacheManager.Items)
                    {
                        Result.Add(((UserSession)item.Value).Clone());
                    }
                }

                return (Result);
            }
        }

        /// <summary>
        /// Returns all cache manager items
        /// </summary>
        public static CacheManager UserSessions
        {
            get
            {
                return (_userSessionCacheManager);
            }
        }

        /// <summary>
        /// Count of active user sessions
        /// </summary>
        public static int Count
        {
            get
            {
                return (_userSessionCacheManager.Count);
            }
        }

        /// <summary>
        /// Get's the active instance of the UserSessionManager
        /// </summary>
        public static UserSessionManager Instance
        {
            get
            {
                return (_sessionManager);
            }
        }

        /// <summary>
        /// Indicates wether it's a static website or not
        /// 
        /// If set to false then country data will be retrieved from the database
        /// </summary>
        public static bool StaticWebSite { get; set; }

        /// <summary>
        /// If true the website is automatically initialised, if false, the app is responsible
        /// </summary>
        public static bool InitialiseWebsite { get; set; }

        /// <summary>
        /// If true, the page/session save is called immediately, if false, it is called within a thread
        /// </summary>
        public static bool SaveImmediately { get; set; }

        #endregion Static Properties

        #region Events

        /// <summary>
        /// Event raised after a user session has been created
        /// </summary>
        public event UserSessionHandler OnSessionCreated;

        /// <summary>
        /// Event raised prior to a user session being closed
        /// 
        /// Data can be saved at this point
        /// </summary>
        public event UserSessionHandler OnSessionClosing;

        /// <summary>
        /// Event raised when session data (full / partial) can be saved
        /// 
        /// The IsDirty property will indicate which data needs saving, if true it hasn't been saved, if false it has been saved already
        /// </summary>
        public event UserSessionHandler OnSessionSave;

        /// <summary>
        /// Event raised when a session needs to be retrieved from the database
        /// </summary>
        public event UserSessionRequiredHandler OnSessionRetrieve;

        /// <summary>
        /// Event raised when page view save is required
        /// </summary>
        public event UserPageViewHandler OnSavePage;

        /// <summary>
        /// Event raised when IP Address details are required
        /// </summary>
        public event IpAddressHandler IPAddressDetails;

        #endregion Events
    }

    /// <summary>
    /// User Session object
    /// 
    /// Stores information about user sessions
    /// </summary>
    [Serializable]
    public class UserSession : IDisposable
    {
        #region Private Members

        /// <summary>
        /// Is Mobile Device
        /// </summary>
        private static Regex MobileCheck = new Regex("android|(android|bb\\d+|meego).+mobile|avantgo|bada\\/|blackberry|blazer|" +
            "compal|elaine|fennec|hiptop|iemobile|ip(hone|od|ad)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|" +
            "opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\\.(browser|link)|" +
            "vodafone|wap|windows (ce|phone)|xda|xiino",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Mobile Version
        /// </summary>
        private static Regex MobileVersionCheck = new Regex("1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|" +
            "s\\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\\-m|r |s )|avan|be(ck|ll|nq)|" +
            "bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\\-(n|u)|c55\\/|capi|ccwa|cdm\\-|cell|chtm|cldc|cmd\\-|co(mp|nd)|craw|da(it|ll|" +
            "ng)|dbte|dc\\-s|devi|dica|dmob|do(c|p)o|ds(12|\\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|" +
            "fly(\\-|_)|g1 u|g560|gene|gf\\-5|g\\-mo|go(\\.w|od)|gr(ad|un)|haie|hcit|hd\\-(m|p|t)|hei\\-|hi(pt|ta)|hp( i|ip)|hs\\-c|" +
            "ht(c(\\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\\-(20|go|ma)|i230|iac( |\\-|\\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|" +
            "ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\\/)|klon|kpt |kwc\\-|kyo(c|k)|le(no|xi)|lg( g|\\/(k|l|u)|50|54|\\-[a-w])|" +
            "libw|lynx|m1\\-w|m3ga|m50\\/|ma(te|ui|xo)|mc(01|21|ca)|m\\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\\-| |" +
            "o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\\-|on|tf|wf|wg|wt)|" +
            "nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\\-2|po(ck|" +
            "rt|se)|prox|psio|pt\\-g|qa\\-a|qc(07|12|21|32|60|\\-[2-7]|i\\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\\/|sa(ge|ma|" +
            "mm|ms|ny|va)|sc(01|h\\-|oo|p\\-)|sdk\\/|se(c(\\-|0|1)|47|mc|nd|ri)|sgh\\-|shar|sie(\\-|m)|sk\\-0|sl(45|id)|sm(al|ar|b3|" +
            "it|t5)|so(ft|ny)|sp(01|h\\-|v\\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\\-|tdg\\-|tel(i|m)|tim\\-|t\\-mo|" +
            "to(pl|sh)|ts(70|m\\-|m3|m5)|tx\\-9|up(\\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\\-v)|vm40|voda|vulc|" +
            "vx(52|53|60|61|70|80|81|83|85|98)|w3c(\\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\\-|your|zeto|zte\\-",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        #endregion Protected Members

        #region Private Members

        /// <summary>
        /// Primary page views
        /// </summary>
        private List<PageViewData> _pageViews = new List<PageViewData>();

        /// <summary>
        /// Private lock object for when adding pages
        /// </summary>
        private readonly object _pageViewLockObject = new object();

        // <summary>
        // Lock object for when user requests a lock
        // </summary>
        //private TimedLock _timedLock;

        #endregion Private Members

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        public UserSession()
        {
            Status = SessionStatus.Initialising;
            Bounced = true;
            Culture = "en-GB";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="created"></param>
        /// <param name="sessionID"></param>
        /// <param name="userAgent"></param>
        /// <param name="initialReferer"></param>
        /// <param name="ipAddress"></param>
        /// <param name="hostName"></param>
        /// <param name="isMobile"></param>
        /// <param name="isBrowserMobile"></param>
        /// <param name="mobileRedirect"></param>
        /// <param name="referralType"></param>
        /// <param name="bounced"></param>
        /// <param name="isBot"></param>
        /// <param name="mobileManufacturer"></param>
        /// <param name="mobileModel"></param>
        /// <param name="userID"></param>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <param name="saleCurrency"></param>
        /// <param name="saleAmount"></param>
        public UserSession(long id, DateTime created, string sessionID, string userAgent, string initialReferer,
            string ipAddress, string hostName, bool isMobile, bool isBrowserMobile, bool mobileRedirect,
            ReferalType referralType, bool bounced, bool isBot, string mobileManufacturer, string mobileModel,
            long userID, int screenWidth, int screenHeight, string saleCurrency, decimal saleAmount)
            : this()
        {
            this.InternalSessionID = id;
            Created = created;
            SessionID = sessionID;
            UserAgent = userAgent;
            InitialReferrer = initialReferer;
            IPAddress = ipAddress;
            HostName = hostName;
            IsMobileDevice = isMobile;
            IsBrowserMobile = isBrowserMobile;
            MobileRedirect = mobileRedirect;
            this.Referal = referralType;
            Bounced = Bounced;
            IsBot = isBot;
            MobileManufacturer = mobileManufacturer;
            MobileModel = mobileModel;
            UserID = userID;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            CurrentSaleCurrency = saleCurrency;
            CurrentSale = saleAmount;
            Status = SessionStatus.Continuing;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Date/Time Session Created
        /// </summary>
        public DateTime Created { get; protected set; }

        /// <summary>
        /// internal indicator on wether it's been processed by the UserSessionManger or not
        /// </summary>
        public SessionStatus Status { get; internal set; }

        /// <summary>
        /// Unique Session ID
        /// </summary>
        public string SessionID { get; protected set; }

        /// <summary>
        /// Internal session id
        /// </summary>
        public Int64 InternalSessionID { get; set; }

        /// <summary>
        /// IP Address of User
        /// </summary>
        public string IPAddress { get; protected set; }

        /// <summary>
        /// Host computer name for user
        /// </summary>
        public string HostName { get; protected set; }

        /// <summary>
        /// User Agent
        /// </summary>
        public string UserAgent { get; protected set; }

        /// <summary>
        /// Is mobile device
        /// </summary>
        public bool IsMobileDevice { get; protected set; }

        /// <summary>
        /// Is mobile device based on browser capabilities
        /// </summary>
        public bool IsBrowserMobile { get; protected set; }

        /// <summary>
        /// Determines wether the user should be redirected to the mobile site
        /// </summary>
        public bool MobileRedirect { get; set; }

        /// <summary>
        /// Type of referral for session
        /// </summary>
        public ReferalType Referal { get; set; }

        /// <summary>
        /// Initial referring website
        /// </summary>
        public string InitialReferrer { get; protected set; }

        /// <summary>
        /// Bounced indicates wether the user came to the page and left the site without doing anything else
        /// </summary>
        public bool Bounced { get; internal set; }

        /// <summary>
        /// User session is a bot
        /// </summary>
        public bool IsBot { get; internal set; }

        /// <summary>
        /// Unique ID for City information
        /// </summary>
        public Int64 CityID { get; protected set; }

        /// <summary>
        /// Country for visitor
        /// </summary>
        public string CountryCode { get; protected set; }

        /// <summary>
        /// Visitor Region
        /// </summary>
        public string Region { get; protected set; }

        /// <summary>
        /// Visitor city
        /// </summary>
        public string CityName { get; protected set; }

        /// <summary>
        /// Latitude for ip address
        /// </summary>
        public decimal Latitude { get; protected set; }

        /// <summary>
        /// Longitude for ip address
        /// </summary>
        public decimal Longitude { get; protected set; }

        /// <summary>
        /// Id of current logged on user
        /// </summary>
        public Int64 UserID { get; set; }

        /// <summary>
        /// Name of logged on user
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Email for logged on user
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// Unique guid used to identify a user
        /// </summary>
        public Guid UserGuid { get; set; }

        /// <summary>
        /// Users basket id
        /// </summary>
        public long UserBasketId { get; set; }

        /// <summary>
        /// Mobile device manufacturer
        /// </summary>
        public string MobileManufacturer { get; protected set; }

        /// <summary>
        /// Mobile device model
        /// </summary>
        public string MobileModel { get; protected set; }

        /// <summary>
        /// Width of users screen
        /// </summary>
        public int ScreenWidth { get; protected set; }

        /// <summary>
        /// Height of users screen
        /// </summary>
        public int ScreenHeight { get; protected set; }

        /// <summary>
        /// List of pages visited by user
        /// </summary>
        public List<PageViewData> Pages
        {
            get
            {
                return (_pageViews);
            }
        }

        /// <summary>
        /// Total time in seconds the user has been viewing pages
        /// </summary>
        public int TotalTime
        {
            get
            {
                int Result = 0;

                foreach (PageViewData view in _pageViews)
                    Result += Convert.ToInt32(view.TotalTime.TotalSeconds);

                return (Result);
            }
        }

        /// <summary>
        /// Current page being viewed
        /// </summary>
        public string CurrentPage { get; set; }

        /// <summary>
        /// Indicates the value of the current sale
        /// 
        /// This value should be set when the website makes a sale
        /// </summary>
        public decimal CurrentSale { get; set; }

        /// <summary>
        /// Current sale currency code
        /// </summary>
        public string CurrentSaleCurrency { get; set; }

        /// <summary>
        /// User defined object for storing other data
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Save status of data, to indicate wether the data requires saving, 
        /// is already saved or pending changes before being saved
        /// </summary>
        public SaveStatus SaveStatus { get; set; }

        /// <summary>
        /// Save Status of pages for session
        /// </summary>
        public SaveStatus PageSaveStatus { get; set; }


        public string Culture { get; set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// PageView is used whenever a user visits a page
        /// </summary>
        /// <param name="page">Current page being viewed</param>
        /// <param name="referrer">Current Referrer</param>
        /// <param name="isPostBack">Is Post Back</param>
        public void PageView(string page, string referrer, bool isPostBack)
        {
            using (TimedLock.Lock(_pageViewLockObject))
            {
                PageViewData newPageView = new PageViewData(page, referrer, isPostBack);
                _pageViews.Add(newPageView);

                if (UserSessionManager.SaveImmediately)
                {
                    newPageView.SaveStatus = Classes.SaveStatus.RequiresSave;
                    UserSessionManager.Instance.RaiseSavePage(this, newPageView);
                }
                else
                {
                    newPageView.SaveStatus = SaveStatus.Pending;
                }
            }

            int pages = _pageViews.Count -1;

            if (pages >= 1)
            {
                // calculate page time for previous page
                PageViewData previousPage = _pageViews[pages -1];
                previousPage.TotalTime = DateTime.Now - previousPage.TimeStamp;

                // not a bounce as already moved onto another page
                if (Bounced)
                    Bounced = false;

                previousPage.SaveStatus = Classes.SaveStatus.RequiresSave;
                this.PageSaveStatus = Classes.SaveStatus.RequiresSave;
            }

            CurrentPage = page;
        }

        /// <summary>
        /// Creates a copy of the object
        /// </summary>
        /// <returns>Shallow cloned copy of the session</returns>
        public UserSession Clone()
        {
            return (UserSession)MemberwiseClone();
        }

        /// <summary>
        /// Updates the user detail properties for the session
        /// </summary>
        /// <param name="userID">Unique ID for user</param>
        /// <param name="username">User's name</param>
        /// <param name="email">Email address for user</param>
        public void Login(Int64 userID, string username, string email)
        {
            UserName = username;
            UserEmail = email;
            UserID = userID;
            SaveStatus = SaveStatus.RequiresSave;

            if (UserSessionManager.SaveImmediately)
                UserSessionManager.Instance.RaiseSessionSave(this);
        }

        /// <summary>
        /// Updates the Sale figures properties for the session
        /// </summary>
        /// <param name="saleAmount">Amount sold</param>
        /// <param name="currencyCode">Currency Code</param>
        public void Sale(decimal saleAmount, string currencyCode)
        {
            CurrentSale = saleAmount;
            CurrentSaleCurrency = currencyCode;
            SaveStatus = Classes.SaveStatus.RequiresSave;

            if (UserSessionManager.SaveImmediately)
                UserSessionManager.Instance.RaiseSessionSave(this);   
        }

        /// <summary>
        /// Internally updates the IP Address details
        /// </summary>
        /// <param name="id">Unique ID for City Record</param>
        /// <param name="latitude">Latitude of IP Address</param>
        /// <param name="longitude">Longitude of IP Address</param>
        /// <param name="regionName">Region for IP Address</param>
        /// <param name="cityName">City for IP Address</param>
        /// <param name="countryCode">Country Code for IP Address</param>
        public void UpdateIPDetails(Int64 id, decimal latitude, decimal longitude, string regionName, 
            string cityName, string countryCode)
        {
            CityID = id;
            Latitude = latitude;
            Longitude = longitude;
            Region = regionName;
            CityName = cityName;
            CountryCode = countryCode;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Detects wether the user session is from a mobile device or not
        /// 
        /// Stores the result as session state
        /// </summary>
        /// <param name="userAgent">User Agent</param>
        /// <returns>bool, true if mobile device, otherwise false</returns>
        protected bool CheckIfMobileDevice(string userAgent)
        {
            if (!String.IsNullOrEmpty(userAgent) && userAgent.Length >= 4)
            {
                return (MobileCheck.IsMatch(userAgent) || MobileVersionCheck.IsMatch(userAgent.Substring(0, 4)));
            }

            return (false);
        }


        #endregion Private Methods

        #region IDisposable

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
#if DEBUG
            System.GC.SuppressFinalize(this);
#endif
            _pageViews.Clear();
        }

        #endregion IDisposable
    }

    /// <summary>
    /// PageViewData
    /// 
    /// Stores information on pages viewed by a user
    /// </summary>
    [Serializable]
    public sealed class PageViewData
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url">url of page being visited</param>
        /// <param name="referringPage">referring page</param>
        /// <param name="isPostBack">bool is post back or not</param>
        public PageViewData(string url, string referringPage, bool isPostBack)
        {
            ID = Int64.MinValue;
            URL = url;
            Referrer = referringPage;
            IsPostBack = isPostBack;
            TimeStamp = DateTime.Now;
            SaveStatus = SaveStatus.Pending;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Unique ID for record
        /// </summary>
        public Int64 ID { get; set; }

        /// <summary>
        /// Page being viewed
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        /// Time page viewed
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// Total time spent on page
        /// </summary>
        public TimeSpan TotalTime { get; internal set; }

        /// <summary>
        /// Referring web page, if any
        /// </summary>
        public string Referrer { get; private set; }

        /// <summary>
        /// Indicates wether it's a post back or not
        /// </summary>
        public bool IsPostBack { get; private set; }

        /// <summary>
        /// Current save status of the page view
        /// </summary>
        public SaveStatus SaveStatus { get; set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Called by application once the page data has been saved
        /// </summary>
        public void Saved()
        {
            SaveStatus = SaveStatus.Saved;
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Initialises a session in a thread
    /// </summary>
    public sealed class InitialiseSessionThread : ThreadManager
    {
        //private List<UserSession> _sessions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="session"></param>
        public InitialiseSessionThread(UserSessionManager manager, UserSession session)
            : base(session, new TimeSpan(0, 0, 0, 0, 300), manager)
        {

        }

        /// <summary>
        /// Thread run method
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override bool Run(object parameters)
        {
            UserSession session = (UserSession)parameters;
            UserSessionManager manager = (UserSessionManager)this.Parent;

            if (session == null || manager == null)
                return (false);

            try
            {
                manager.InitialiseSession(session);
            }
            catch (Exception err)
            {
                EventLog.Add(err);
            }

            return (false);
        }
    }

    /// <summary>
    /// Referral Types
    /// </summary>
    public enum ReferalType 
    { 
        /// <summary>
        /// Not a clue
        /// </summary>
        Unknown = 0, 
        
        /// <summary>
        /// User typed url directly into the browser
        /// </summary>
        Direct = 1, 
        
        /// <summary>
        /// Referral from a search engine
        /// </summary>
        Organic = 2, 
        
        /// <summary>
        /// From a.n. other website
        /// </summary>
        Referal = 3,

        /// <summary>
        /// Referring agent was Facebook
        /// </summary>
        Facebook = 4,

        /// <summary>
        /// Referring agent was Twitter
        /// </summary>
        Twitter = 5,

        /// <summary>
        /// Referring agent was google
        /// </summary>
        Google = 6,

        /// <summary>
        /// Referring agent was yahoo
        /// </summary>
        Yahoo = 7,

        /// <summary>
        /// Referring agent was Bing 
        /// </summary>
        Bing = 8
    }

    /// <summary>
    /// Session Status
    /// </summary>
    public enum SessionStatus 
    { 
        /// <summary>
        /// Initialising
        /// </summary>
        Initialising, 
        
        /// <summary>
        /// Updated
        /// </summary>
        Updated, 
        
        /// <summary>
        /// About to be closed
        /// </summary>
        Closing, 
        
        /// <summary>
        /// Session has been reloaded for continuing
        /// </summary>
        Continuing 
    }

    /// <summary>
    /// Save status for current item
    /// </summary>
    public enum SaveStatus 
    { 
        /// <summary>
        /// Data has already been saved
        /// </summary>
        Saved, 
        
        /// <summary>
        /// Item is ready to be saved, no further updates anticipated
        /// </summary>
        RequiresSave, 
        
        /// <summary>
        /// Awaiting more information before save can proceed
        /// </summary>
        Pending 
    }
}
