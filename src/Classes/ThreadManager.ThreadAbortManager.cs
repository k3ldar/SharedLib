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
    /// Thread to abort other threads
    /// 
    /// Threads are given an opportunity to abort nicely, if they don't then
    /// they will be forced to abort
    /// </summary>
    internal sealed class ThreadAbortManager : ThreadManager
    {
        #region Constructors

        /// <summary>
        /// Constructor, initialises to run thread every 1 second with a delay of 2 seconds between runs
        /// </summary>
        public ThreadAbortManager()
            : base(null, new TimeSpan(0, 0, 0, 1), null, 2000, 100)
        {
            ContinueIfGlobalException = true;
        }

        #endregion Constructors

        protected override bool Run(object parameters)
        {
            // kill the thread
            for (int i = _abortPool.Count - 1; i >= 0; i--)
            {
                ThreadManager item = _abortPool[i];

                _abortPool.Remove(item);

                item.Abort();

                // play niceley
                if (HasCancelled())
                    return false;
            }

            return !HasCancelled();
        }
    }
}
