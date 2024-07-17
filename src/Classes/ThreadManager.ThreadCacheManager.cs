/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Managed Threads and thread manager class
 *
 */
using System;

namespace Shared.Classes
{
    /// <summary>
    /// thread that clears cached values
    /// </summary>
    internal class ThreadCacheManager : ThreadManager
    {
        internal ThreadCacheManager()
            : base(null, new TimeSpan(0, 0, 15))
        {
            RunAtStartup = false;
            HangTimeoutSpan = TimeSpan.FromMinutes(30); // no response for 30 minutes then kill
            ContinueIfGlobalException = true;
        }

        protected override bool Run(object parameters)
        {
            CacheManager.CleanAllCaches();
            return !HasCancelled();
        }
    }
}
