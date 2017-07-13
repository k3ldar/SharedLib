/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Extended WebClient class with timeout, user defined UserAgent and Cookies
 *
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace Shared.Classes
{
    /// <summary>
    /// Extended WebClient object
    /// </summary>
    public sealed class WebClientEx : WebClient
    {
        #region Private Members

        private System.Net.CookieContainer _cookieContainer;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userAgent">User Agent</param>
        public WebClientEx(string userAgent)
            :this()
        {
            UserAgent = userAgent;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public WebClientEx()
        {
            Timeout = -1;
            UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";
            _cookieContainer = new CookieContainer();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Cookie Container
        /// </summary>
        public System.Net.CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
            set { _cookieContainer = value; }
        }

        /// <summary>
        /// Cookies
        /// </summary>
        /// <param name="uri">Uri for cookies</param>
        /// <returns>CookiesCollection</returns>
        public System.Net.CookieCollection Cookies(Uri uri)
        {
            return (_cookieContainer.GetCookies(uri));
        }

        /// <summary>
        /// User Agent
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Response Time for GetWebRequest
        /// </summary>
        public int ResponseTime { get; private set; }

        #endregion Properties

        #region Overridden Methods

        /// <summary>
        /// Get's a web request
        /// </summary>
        /// <param name="address"></param>
        /// <returns>Uri for request</returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            DateTime StartRead = DateTime.Now;

            WebRequest request = base.GetWebRequest(address);

            if (request.GetType() == typeof(HttpWebRequest))
            {
                ((HttpWebRequest)request).CookieContainer = _cookieContainer;
                ((HttpWebRequest)request).UserAgent = UserAgent;
                ((HttpWebRequest)request).Timeout = Timeout;
            }

            TimeSpan span = DateTime.Now.Subtract(StartRead);
            ResponseTime = span.Milliseconds;

            return (request);
        }

        #endregion Overridden Methods

        #region Private Methods

        /// <summary>
        /// Randomly selects a new user agent from predetermined list
        /// </summary>
        private void RefreshUserAgent()
        {
            List<string> UserAgents = new List<string>();
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; AOL 9.5; AOLBuild 4337.43; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; AOL 9.5; AOLBuild 4337.34; Windows NT 6.0; WOW64; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
            UserAgents.Add("Mozilla/5.0 (X11; U; Linux i686; pl-PL; rv:1.9.0.2) Gecko/20121223 Ubuntu/9.25 (jaunty) Firefox/3.8");
            UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; ja; rv:1.9.2a1pre) Gecko/20090402 Firefox/3.6a1pre (.NET CLR 3.5.30729)");
            UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1b4) Gecko/20090423 Firefox/3.5b4 GTB5 (.NET CLR 3.5.30729)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Avant Browser; .NET CLR 2.0.50727; MAXTHON 2.0)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; Media Center PC 6.0; InfoPath.2; MS-RTC LM 8)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; WOW64; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; InfoPath.2; .NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 6.0)");
            UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 5.1; Media Center PC 3.0; .NET CLR 1.0.3705; .NET CLR 1.1.4322; .NET CLR 2.0.50727; InfoPath.1)");
            UserAgents.Add("Opera/9.70 (Linux i686 ; U; zh-cn) Presto/2.2.0");
            UserAgents.Add("Opera 9.7 (Windows NT 5.2; U; en)");
            UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.8.1.8pre) Gecko/20070928 Firefox/2.0.0.7 Navigator/9.0RC1");
            UserAgents.Add("Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1.7pre) Gecko/20070815 Firefox/2.0.0.6 Navigator/9.0b3");
            UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; en) AppleWebKit/526.9 (KHTML, like Gecko) Version/4.0dp1 Safari/526.8");
            UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; ru-RU) AppleWebKit/528.16 (KHTML, like Gecko) Version/4.0 Safari/528.16");
            UserAgents.Add("Opera/9.64 (X11; Linux x86_64; U; en) Presto/2.1.1");

            Random r = new Random();
            this.UserAgent = UserAgents[r.Next(0, UserAgents.Count)];

            UserAgents = null;
        }

        #endregion Private Methods
    }
}
