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
    /// Class that manages the threads within the thread manager
    /// </summary>
    internal sealed class ThreadManagerManager : ThreadManager
    {
        #region Constructors

        /// <summary>
        /// Constructor, initialises to run thread every 1 second with a delay of 0 seconds between runs
        /// </summary>
        public ThreadManagerManager()
            : base(null, new TimeSpan(0, 0, 0, 1), null, 0, 100)
        {
            ContinueIfGlobalException = true;
        }

        #endregion Constructors

        protected override bool Run(object parameters)
        {
            // check to see if timeout value of cancel has expired, if so, kill the thread
            using (TimedLock.Lock(_lockObject))
            {
                _cpuUsage.GetProcessUsage();

                for (int i = _threadList.Count - 1; i >= 0; i--)
                {
                    ThreadManager item = _threadList[i];

                    if (_checkForHangingThreads)
                    {
                        TimeSpan hangingSpan = DateTime.UtcNow - item._lastCommunication;

                        if (item.HangTimeoutSpan.TotalMilliseconds > 0 && !item._cancel && hangingSpan.TotalMilliseconds > item.HangTimeoutSpan.TotalMilliseconds)
                        {
                            //set time out long enough for the thread to clear itself out
                            // if it doesn't then we will force the closure
                            item.CancelThread(10000, true);
                        }
                    }

                    if (item.MarkedForRemoval)
                    {
                        item.ThreadFinishing -= thread_ThreadFinishing;
                    }

                    if (item._cancel)
                    {
                        TimeSpan span = DateTime.UtcNow - item._cancelRequested;

                        if (span.TotalMilliseconds > item._cancelTimeoutMilliseconds)
                        {
                            if (!item.MarkedForRemoval)
                            {
                                _countOfThreads--;
                                item.ThreadFinishing -= thread_ThreadFinishing;
                                _threadList.Remove(item);
                            }

                            RaiseThreadForcedToClose(item);
                            _abortPool.Add(item);
                        }
                    }

                    // if there is enough space, can we run one of the threads in the pool?
                    if (AllowThreadPool && _threadPool.Count > 0)
                    {
                        while (_threadList.Count < MaximumRunningThreads)
                        {
                            ThreadManager nextRunItem = _threadPool[0];

                            _threadPool.RemoveAt(0);
                            RaiseThreadQueueRemoveItem(nextRunItem);

                            ThreadStart(nextRunItem, nextRunItem.Name, nextRunItem._thread.Priority, _thread.IsBackground);
                        }
                    }
                }
            }

            return !HasCancelled();
        }
    }
}
