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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Shared.Classes
{
    public partial class ThreadManager
    {

        #region Private / Internal Static Members

        /// <summary>
        /// CPU Usage object
        /// </summary>
        internal static ICpuUsage _cpuUsage;

        /// <summary>
        /// List of managed threads
        /// </summary>
        internal static volatile List<ThreadManager> _threadList = new List<ThreadManager>();

        /// <summary>
        /// Thread pool to hold threads if MaximumRunningThreads is exceeded
        /// </summary>
        internal static List<ThreadManager> _threadPool = new List<ThreadManager>();

        /// <summary>
        /// List of threads to abort
        /// </summary>
        internal static List<ThreadManager> _abortPool = new List<ThreadManager>();

        /// <summary>
        /// Number of managed threads
        /// </summary>
        internal static volatile int _countOfThreads = 0;

        /// <summary>
        /// Object used for exclusive locking
        /// </summary>
        internal static object _lockObject = new object();

        /// <summary>
        /// Thread which manages the other threads in the list
        /// </summary>
        private static ThreadManagerManager _threadManager = null;

        /// <summary>
        /// Thread to abort other threads
        /// </summary>
        private static ThreadAbortManager _threadAbortManager = null;

        /// <summary>
        /// Cache Manager for all Threads
        /// </summary>
        private static ThreadCacheManager _threadCacheManager = null;

        /// <summary>
        /// Indicates all threadshave been requested to cancel
        /// </summary>
        private static bool _globalCancelRequested = false;

        /// <summary>
        /// Maximum number of threads that can be run
        /// </summary>
        private static int _maximumRunningThreads = 20;

        /// <summary>
        /// Determines wether a thread pool is available or not
        /// </summary>
        private static bool _allowThreadsToPool = true;

        /// <summary>
        /// Maximum number of waiting threads in the thread pool
        /// </summary>
        private static int _maximumThreadPoolSize = 300;

        /// <summary>
        /// Total number of minutes to wait before determining a thread is hanging 
        /// if it fails to communicate
        /// </summary>
        private static int _threadHangTimeoutMinutes = 3;

        /// <summary>
        /// Indicates wether checks for hanging threads (those that do not
        /// communicate in a timely fashion) are made, or not
        /// </summary>
        internal static bool _checkForHangingThreads = true;

        /// <summary>
        /// Event raised if thread cpu usage changes by this amount
        /// </summary>
        private static int _threadCPUChangeNotification = 10;

        #endregion Private / Internal Static Members

        #region Static Events

        /// <summary>
        /// Event raised when an exception occurs
        /// </summary>
        public static event ThreadManagerExceptionEventDelegate ThreadExceptionRaised;

        /// <summary>
        /// Event raised when a thread is forced to close
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadForcedToClose;

        /// <summary>
        /// Event raised when a thread Abort is forced
        /// 
        /// Indicates the threads internal Abort method was called but it failed to work so thread.abort() was being called
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadAbortForced;

        /// <summary>
        /// Thread has started
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadStarted;

        /// <summary>
        /// Thread has stopped running
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadStopped;

        /// <summary>
        /// Thread has been added to the queue
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadQueueAddItem;

        /// <summary>
        /// Thread has been removed from the queue
        /// </summary>
        public static event ThreadManagerEventDelegate ThreadQueueRemoveItem;

        /// <summary>
        /// Event raised when the thread queue is cleared
        /// </summary>
        public static event EventHandler ThreadQueueCleared;

        /// <summary>
        /// Event raised when Cancel all threads is called
        /// </summary>
        public static event EventHandler ThreadCancellAll;

        /// <summary>
        /// Event raised when cpu usage for a thread changes by designated amount
        /// </summary>
        public static event EventHandler ThreadCpuChanged;

        #endregion Static Events

        #region Public Static Methods

        /// <summary>
        /// Retrieves the Nth thread in the list
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static ThreadManager Get(int Index)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            return _threadList[Index];
        }

        /// <summary>
        /// Checks wether a thread with a specific name already exists within the managed threads
        /// </summary>
        /// <param name="name">Name of Thread
        /// 
        /// Exact match which is case sensistive</param>
        /// <returns>true if thread exists, otherwise false</returns>
        public static bool Exists(string name)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            using (TimedLock.Lock(_lockObject))
            {
                foreach (ThreadManager item in _threadList)
                {
                    if (item.Name == name && !item.MarkedForRemoval)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks wether a thread with a specific name exists and if so returns it
        /// </summary>
        /// <param name="name">Name of Thread
        /// 
        /// Exact match which is case sensistive</param>
        /// <returns>ThreadManager object if thread exists, otherwise null</returns>
        public static ThreadManager Find(string name)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            using (TimedLock.Lock(_lockObject))
            {
                foreach (ThreadManager item in _threadList)
                {
                    if (item.Name == name && !item.MarkedForRemoval)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Initiates a thread and adds it to a thread pool
        /// </summary>
        /// <param name="thread">ThreadManager descendant which will be run in a thread</param>
        /// <param name="name">Name of the thread</param>
        /// <param name="priority">Priority of thread</param>
        public static void ThreadStart(ThreadManager thread, string name, ThreadPriority priority)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            ThreadStart(thread, name, priority, true, true);
        }

        /// <summary>
        /// Initiates a thread and adds it to a thread pool
        /// </summary>
        /// <param name="thread">ThreadManager descendant which will be run in a thread</param>
        /// <param name="name">Name of the thread</param>
        /// <param name="priority">Priority of thread</param>
        public static void ThreadStart(ThreadManager thread, string name, ThreadPriority priority, bool isBackGround)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            ThreadStart(thread, name, priority, true, isBackGround);
        }

        /// <summary>
        /// Initialises all ThreadManager objects
        /// 
        /// Called once at startup
        /// </summary>
        public static void Initialise()
        {
            Initialise(new CpuUsage());
        }

        public static void Initialise(ICpuUsage cpuUsage)
        {
            _cpuUsage = cpuUsage ?? throw new ArgumentNullException(nameof(cpuUsage));

            using (TimedLock.Lock(_lockObject))
            {
                _globalCancelRequested = false;

                if (_threadManager == null)
                {
                    _threadManager = new ThreadManagerManager();
                    ThreadStart(_threadManager, "Thread Manager - Management Thread", ThreadPriority.BelowNormal, false);
                }


                if (_threadAbortManager == null)
                {
                    _threadAbortManager = new ThreadAbortManager();
                    ThreadStart(_threadAbortManager, "Thread Abort - Management Thread", ThreadPriority.Lowest, false);
                }


                if (_threadCacheManager == null)
                {
                    _threadCacheManager = new ThreadCacheManager();
                    Classes.ThreadManager.ThreadStart(_threadCacheManager,
                        "Cache Management Thread", ThreadPriority.Lowest);
                }
            }
        }

        /// <summary>
        /// Finalises all ThreadManager objects
        /// 
        /// Called once at finish
        /// </summary>
        public static void Finalise()
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            CancelAll();

            if (_threadManager != null)
            {
                _threadManager.CancelThread();
                _threadManager = null;
            }

            if (_threadAbortManager != null)
            {
                _threadAbortManager.CancelThread();
                _threadAbortManager = null;
            }

            if (_threadCacheManager != null)
            {
                _threadCacheManager.CancelThread();
                _threadCacheManager = null;
            }

            _cpuUsage = null;
        }

        /// <summary>
        /// Cancel's a running thread
        /// </summary>
        /// <param name="name">Name of the thread</param>
        public static void Cancel(string name)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            using (TimedLock.Lock(_lockObject, new TimeSpan(0, 0, 30)))
            {
                foreach (ThreadManager item in _threadList)
                {
                    if (item.Name == name && !item.MarkedForRemoval)
                    {
                        item.CancelThread();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Cancel's all threads, requesting that they close
        /// </summary>
        /// <param name="timeOutSeconds">Number of seconds to wait for all threads to finish</param>
        public static void CancelAll(int timeOutSeconds = 15)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            _globalCancelRequested = true;

            using (TimedLock.Lock(_lockObject))
            {
                _threadPool.Clear();

                ThreadQueueCleared?.Invoke(null, EventArgs.Empty);

                foreach (ThreadManager item in _threadList)
                {
                    item?.CancelThread();
                }
            }

            // raise an event
            ThreadCancellAll?.Invoke(null, EventArgs.Empty);

            // provide a certain number of seconds for everything to clean up
            DateTime cancelInitiated = DateTime.UtcNow;
            TimeSpan span = DateTime.UtcNow - cancelInitiated;

            while (span.TotalSeconds <= timeOutSeconds)
            {
                using (TimedLock.Lock(_lockObject))
                {
                    if (_threadList.Count == 0)
                        break;

                    for (int i = _threadList.Count -1; i >= 0; i--)
                    {
                        if (_threadList[i] == null)
                        {
                            _threadList.RemoveAt(i);
                        }
                    }
                }

                Thread.Sleep(0);
                span = DateTime.UtcNow - cancelInitiated;
            }

        }

        /// <summary>
        /// Set's the priority for all threads
        /// </summary>
        /// <param name="priority">Priority to be applied</param>
        public static void UpdatePriority(ThreadPriority priority)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            using (TimedLock.Lock(_lockObject))
            {
                foreach (ThreadManager item in _threadList)
                    item._thread.Priority = priority;
            }
        }

        #endregion Public Static Methods

        #region Private Static Methods

        /// <summary>
        /// Starts a new thread
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="addToList"></param>
        private static void ThreadStart(ThreadManager thread, string name, ThreadPriority priority, bool addToList, bool isBackgroundThread)
        {
            if (_cpuUsage == null)
                throw new InvalidOperationException("ThreadManager must be initialised");

            if (thread == null)
                throw new ArgumentNullException("thread parameter can not be null");

            // generic properties
            thread.ThreadFinishing += thread_ThreadFinishing;
            thread.IsBackGround = isBackgroundThread;
            thread._thread = new Thread(thread.ThreadRun);
            thread._thread.IsBackground = isBackgroundThread;
            thread._thread.Name = name;

            if (addToList)
            {
                lock (_lockObject)
                {
                    if (_threadList.Count >= _maximumRunningThreads)
                    {
                        if (_allowThreadsToPool)
                        {
                            if (_threadPool.Count > _maximumThreadPoolSize)
                                throw new Exception("Thread Pool Count exceeded");

                            _threadPool.Add(thread);

                            // notify listners that a thread has been added to the queue
                            ThreadQueueAddItem?.Invoke(null, new ThreadManagerEventArgs(thread));

                            return;
                        }
                        else
                            throw new Exception("Maximum running threads exceeded.");
                    }
                }
            }

            thread._thread.Priority = priority;
            thread._thread.IsBackground = isBackgroundThread;
            thread._thread.Start(thread._parameters);

            if (addToList)
            {
                using (TimedLock.Lock(_lockObject))
                {
                    thread._lastCommunication = DateTime.UtcNow;

                    //_countOfThreads++;
                    //_threadList.Add(thread);
                }
            }
        }

        #endregion Private Static Methods

        #region Internal Static Methods

        /// <summary>
        /// Event method to remove from list of threads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void thread_ThreadFinishing(object sender, ThreadManagerEventArgs e)
        {
            e.Thread._markedForRemoval = true;
            e.Thread.ThreadFinishing -= thread_ThreadFinishing;
            e.Thread.CancelThread(30000, false);
        }

        /// <summary>
        /// Raises the thread queue remove item event
        /// </summary>
        /// <param name="thread"></param>
        internal static void RaiseThreadQueueRemoveItem(ThreadManager thread)
        {
            ThreadQueueRemoveItem?.Invoke(null, new ThreadManagerEventArgs(thread));
        }

        #endregion Internal Static Methods

        #region Static Properties

        /// <summary>
        /// Returns the number of active threads
        /// </summary>
        public static int ThreadCount
        {
            get
            {
                return _countOfThreads;
            }
        }

        /// <summary>
        /// Indicates the size of the thread pool
        /// </summary>
        public static int ThreadPoolCount
        {
            get
            {
                return _threadPool.Count;
            }
        }

        /// <summary>
        /// Indicates that a request to cancel all threads has been made
        /// </summary>
        public static bool CancelRequested
        {
            get
            {
                return _globalCancelRequested;
            }
        }

        /// <summary>
        /// Sets the maximum number of threads that can be run 
        /// </summary>
        public static int MaximumRunningThreads
        {
            get
            {
                return _maximumRunningThreads;
            }

            set
            {
                _maximumRunningThreads = Utilities.CheckMinMax(value, 1, 200000);
            }
        }

        /// <summary>
        /// Determines wether checks are made for hanging threads, those that
        /// do not play nicely with others or appear incommunacative
        /// </summary>
        public static bool CheckForHangingThreads
        {
            get
            {
                return _checkForHangingThreads;
            }

            set
            {
                _checkForHangingThreads = value;
            }
        }

        /// <summary>
        /// Determines wether a thread pool is in operation
        /// </summary>
        public static bool AllowThreadPool
        {
            get
            {
                return _allowThreadsToPool;
            }

            set
            {
                _allowThreadsToPool = value;

                // clear existing pool if not allowed to pool
                if (!_allowThreadsToPool)
                {
                    using (TimedLock.Lock(_lockObject))
                    {
                        _threadPool.Clear();

                        ThreadQueueCleared?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Maximum size of the thread pool
        /// </summary>
        public static int MaximumPoolSize
        {
            get
            {
                return _maximumThreadPoolSize;
            }

            set
            {
                _maximumThreadPoolSize = Utilities.CheckMinMax(value, 10, 100000);
            }
        }

        /// <summary>
        /// Number of minutes a thread will timeout if it does not communicate and is deemed to have hanged
        /// </summary>
        public static int ThreadHangTimeout
        {
            get
            {
                return _threadHangTimeoutMinutes;
            }

            set
            {
                _threadHangTimeoutMinutes = Utilities.CheckMinMax(value, 1, 30);
            }
        }

        /// <summary>
        /// Retrieves the CPU Usage for the process
        /// </summary>
        public static decimal CpuUsage
        {
            get
            {
                return _cpuUsage.GetProcessUsage();
            }
        }

        /// <summary>
        /// Process usage for other threads, including Main Process thread and unmanaged threads
        /// </summary>
        public static decimal ProcessCpuOther
        {
            get
            {
                return _cpuUsage.OtherProcessCPUUsage;
            }
        }

        /// <summary>
        /// Raises an event if Thread CPU usage changes by given percentage
        /// 
        /// 0 = no notification
        /// 50 is maximum value
        /// </summary>
        public static int ThreadCpuChangeNotification
        {
            get
            {
                return _threadCPUChangeNotification;
            }

            set
            {
                _threadCPUChangeNotification = Utilities.CheckMinMax(value, 0, 50);
            }
        }

        #endregion Static Properties

        #region Event Wrappers


        /// <summary>
        /// Event raised when a thread is forced to close
        /// </summary>
        /// <param name="thread"></param>
        public static void RaiseThreadForcedToClose(ThreadManager thread)
        {
            ThreadForcedToClose?.Invoke(null, new ThreadManagerEventArgs(thread));

            if (thread.Name == "Cache Management Thread" && !thread.Cancelled && thread.UnResponsive)
            {
                Classes.ThreadManager.ThreadStart(_threadCacheManager,
                    "Cache Management Thread", ThreadPriority.Lowest);
            }
        }

        /// <summary>
        /// Indicates a thread was being forced to Abort
        /// </summary>
        /// <param name="thread"></param>
        public static void RaiseThreadAbortForced(ThreadManager thread)
        {
            ThreadAbortForced?.Invoke(null, new ThreadManagerEventArgs(thread));
        }

        /// <summary>
        /// Raises an event to indicate the % of cpu usage for a thread has chenged by a specific amount
        /// </summary>
        public static void RaiseThreadCpuChanged()
        {
            ThreadCpuChanged?.Invoke(null, EventArgs.Empty);
        }

        #endregion Event Wrappers
    }
}
