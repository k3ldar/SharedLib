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
using System.Diagnostics;
using System.Threading;

#pragma warning disable IDE1005 // Delegate invocation can be simplified
#pragma warning disable IDE1006 // naming rule violation
#pragma warning disable IDE0016 // simplified null checks
#pragma warning disable IDE0017 // initialization can be simplified

namespace Shared.Classes
{
    /// <summary>
    /// Thread Management class
    /// 
    /// Manages threads, includes thread queing, basic statistics for thread on Process and System % CPU usage
    /// </summary>
    public class ThreadManager
    {
#if WINDOWS_ONLY
        #region DLL Imports

        /// <summary>
        /// Get's the current thread ID (Win API)
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThreadId();

        #endregion DLL Imports
#endif

        #region Private / Internal Static Members

        /// <summary>
        /// CPU Usage object
        /// </summary>
        internal static CpuUsage _cpuUsage = new CpuUsage();

        /// <summary>
        /// List of managed threads
        /// </summary>
        internal static List<ThreadManager> _threadList = new List<ThreadManager>();

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
        internal static int _countOfThreads = 0;

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
        private static ThreadCachManager _threadCacheManager = null;

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

        #region Internal / Private Members

        /// <summary>
        /// Determines wether cpu usage is monitored for this thread
        /// </summary>
        private readonly bool _monitorCPUUsage;

        /// <summary>
        /// Previous Thread Time (Kernal/User)
        /// </summary>
        private TimeSpan _prevThreadTotal;

        /// <summary>
        /// Actual thread object
        /// </summary>
        internal Thread _thread;

        /// <summary>
        /// Indicates wether the thread should delay starting in milliseconds
        /// </summary>
        private readonly int _delayStart = 0;

        /// <summary>
        /// Indicates that the thread should cancel
        /// </summary>
        internal volatile bool _cancel = false;

        /// <summary>
        /// flag to indicate wether the thread was unresponsive or not
        /// </summary>
        internal bool _unresponsive = false;

        /// <summary>
        /// Number of milliseconds to wait for time out
        /// </summary>
        internal int _cancelTimeoutMilliseconds;

        /// <summary>
        /// Date/time cancel thread requested
        /// </summary>
        internal DateTime _cancelRequested;

        /// <summary>
        /// Thread parameters
        /// </summary>
        private readonly object _parameters;

        /// <summary>
        /// Indicates the thread is marked for removal from the collection
        /// </summary>
        private bool _markedForRemoval = false;

        /// <summary>
        /// DateTime of last communication from the thread, it's upto the thread to 
        /// update this value and it *could* be used to determine that the thread has
        /// timed out
        /// </summary>
        internal DateTime _lastCommunication;

        /// <summary>
        /// Date/Time thread last executed
        /// </summary>
        private DateTime _lastRun;

        /// <summary>
        /// Parent Thread, if set
        /// </summary>
        internal ThreadManager _parentThread = null;

        /// <summary>
        /// All child threads
        /// </summary>
        internal List<ThreadManager> _childThreads = new List<ThreadManager>();

        #endregion Internal / Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameters">Paramaters to be passed to the thread</param>
        /// <param name="runInterval">Frequency which the thread will run</param>
        /// <param name="parent">Parent ThreadManager object, if there is one</param>
        /// <param name="delayStart">Number of milliseconds to delay the starting of the thread when run</param>
        /// <param name="sleepInterval">Number of milliseconds the thread should sleep for after running (Minimum 0 maximum 2000)</param>
        /// <param name="runAtStart">Indicates wether the thread should run straight away (after delayStart value) or should wait for the first iteration of runInterval
        /// 
        /// If runInterval is quite long, i.e. 30 minutes and runAtStart is false then the first time the thread would run would be after 30 minutes</param>
        /// <param name="monitorCPUUsage">if true cpu usage for the thread will be monitored both for system and process percentage</param>
        public ThreadManager(object parameters, TimeSpan runInterval, ThreadManager parent = null, int delayStart = 0,
            int sleepInterval = 200, bool runAtStart = true, bool monitorCPUUsage = true)
        {
            if (runInterval == null)
                throw new ArgumentNullException("runInternal can not be null");

            RunInterval = runInterval;
            _delayStart = delayStart;
            _parentThread = null;
            RunAtStartup = runAtStart;
            SleepInterval = Utilities.CheckMinMax(sleepInterval, 0, 2000);
            _parameters = parameters;
            ContinueIfGlobalException = true;
            _parentThread = parent;

#if WINDOWS_ONLY
            _monitorCPUUsage = monitorCPUUsage;
#else
            _monitorCPUUsage = false;
#endif

            PreviousProcessCpuUsage = 0.0m;

            // each thread can have it's own timeout period, set as default to global value 
            HangTimeout = ThreadHangTimeout;

            if (parent != null)
                parent.ChildThreads.Add(this);

            ProcessCpuUsage = 0;
            SystemCpuUsage = 0;
        }

#endregion Constructors

#region Static Methods

#region Public Static Methods

        /// <summary>
        /// Retrieves the Nth thread in the list
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static ThreadManager Get(int Index)
        {
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
            ThreadStart(thread, name, priority, true);
        }

        /// <summary>
        /// Initialises all ThreadManager objects
        /// 
        /// Called once at startup
        /// </summary>
        public static void Initialise()
        {
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
                    _threadCacheManager = new ThreadCachManager();
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

        }

        /// <summary>
        /// Cancel's a running thread
        /// </summary>
        /// <param name="name">Name of the thread</param>
        public static void Cancel(string name)
        {
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
            _globalCancelRequested = true;

            using (TimedLock.Lock(_lockObject))
            {
                _threadPool.Clear();

                if (ThreadQueueCleared != null)
                    ThreadQueueCleared(null, EventArgs.Empty);

                foreach (ThreadManager item in _threadList)
                    item.CancelThread();
            }

            // raise an event
            if (ThreadCancellAll != null)
                ThreadCancellAll(null, EventArgs.Empty);

            // provide a certain number of seconds for everything to clean up
            DateTime cancelInitiated = DateTime.UtcNow;
            TimeSpan span = DateTime.UtcNow - cancelInitiated;

            while (span.TotalSeconds <= timeOutSeconds)
            {
                if (_threadList.Count == 0)
                    break;

                span = DateTime.UtcNow - cancelInitiated;
            }

        }

        /// <summary>
        /// Set's the priority for all threads
        /// </summary>
        /// <param name="priority">Priority to be applied</param>
        public static void UpdatePriority(ThreadPriority priority)
        {
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
        private static void ThreadStart(ThreadManager thread, string name, ThreadPriority priority, bool addToList)
        {
            if (thread == null)
                throw new ArgumentNullException("thread parameter can not be null");

            // generic properties
            thread.ThreadFinishing += thread_ThreadFinishing;
            thread._thread = new Thread(thread.ThreadRun);
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
                            if (ThreadQueueAddItem != null)
                                ThreadQueueAddItem(null, new ThreadManagerEventArgs(thread));

                            return;
                        }
                        else
                            throw new Exception("Maximum running threads exceeded.");
                    }
                }
            }

            thread._thread.Priority = priority;
            thread._thread.Start(thread._parameters);

            if (addToList)
            {
                using (TimedLock.Lock(_lockObject))
                {
                    thread._lastCommunication = DateTime.UtcNow;

                    _countOfThreads++;
                    _threadList.Add(thread);
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
            if (ThreadQueueRemoveItem != null)
                ThreadQueueRemoveItem(null, new ThreadManagerEventArgs(thread));
        }

#endregion Internal Static Methods

#endregion Static Methods

#region Public Methods

        /// <summary>
        /// Indicates that the thread should cancel
        /// </summary>
        /// <param name="timeout">Number of milliseconds to wait for the thread to close before it is forced to close, 
        /// default to 10 seconds</param>
        /// <param name="isUnResponsive">Indicates wether the thread is unresponsive or not</param>
        public virtual void CancelThread(int timeout = 10000, bool isUnResponsive = false)
        {
            // have we already been asked to cancel?
            if (_cancel)
                return;

            _cancel = true;
            _cancelTimeoutMilliseconds = timeout;
            _cancelRequested = DateTime.UtcNow;

            _unresponsive = isUnResponsive;

            for (int i = 0; i < _childThreads.Count; i++)
                _childThreads[i].CancelThread(timeout, isUnResponsive);

            RaiseThreadCancelRequested(this);
        }

        /// <summary>
        /// Indicates the thread should abort
        /// </summary>
        public virtual void Abort()
        {
            _thread.Abort();
        }

        /// <summary>
        /// Request to cancel all child threads
        /// </summary>
        public void CancelChildren()
        {
            RaiseThreadCancelChildrenRequested(this);

            for (int i = 0; i < _childThreads.Count; i++)
                _childThreads[i].CancelThread(10000, false);
        }

        /// <summary>
        /// Returns a string describing the Thread
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Usage Process/System: {5}%/{6}%; Name: {0}; ThreadID: {1}; IsCancelled: {2}; " +
                "IsUnResponsive: {3}; IsMarkedForRemoval: {4}",
                Name, ThreadID, Cancelled, _unresponsive, _markedForRemoval,
                ProcessCpuUsage.ToString("n1"), SystemCpuUsage.ToString("n1"));
        }

#endregion Public Methods

#region Protected Methods

        /// <summary>
        /// Thread execution method
        /// </summary>
        /// <param name="parameters"></param>
        protected void ThreadRun(object parameters)
        {
            ThreadID = _thread.ManagedThreadId;

#if WINDOWS_ONLY

            ID = (int)GetCurrentThreadId();

            if (_monitorCPUUsage)
                _cpuUsage.ThreadAdd(this);
#else
            ID = ThreadID;
#endif

            TimeStart = DateTime.UtcNow;

            // is the start being delayed
            if (_delayStart > 0)
            {
                // need to ensure that if the app is closed prior to the thread being
                // run then we provide a mechanism for the thread to close
                DateTime continueTime = DateTime.UtcNow.AddMilliseconds(_delayStart);

                while (continueTime > DateTime.UtcNow)
                {
                    Thread.Sleep(100);

                    if (HasCancelled())
                        break;
                }
            }

            _lastRun = DateTime.UtcNow.AddDays(RunAtStartup ? -1 : 0);
            DateTime lastPing = DateTime.UtcNow;

            RaiseThreadStart(this);
            try
            {
                while (true) // always loop
                {
                    try
                    {
                        //have we been asked to cancel the thread?
                        if (_cancel)
                            return;

                        TimeSpan span = DateTime.UtcNow - _lastRun;

                        // run the thread
                        if (span.TotalMilliseconds > RunInterval.TotalMilliseconds)
                        {
                            if (!Run(_parameters))
                                return;

                            _lastRun = DateTime.UtcNow;
                        }

                        span = DateTime.UtcNow - lastPing;

                        if (span.TotalSeconds > 30)
                        {
                            lastPing = DateTime.UtcNow;
                            Ping();
                        }

                        // play niceley with everyone else
                        Thread.Sleep(SleepInterval);
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception error)
                    {
                        RaiseOnException(error);

                        if (!ContinueIfGlobalException)
                            throw;
                    }
                }
            }
            finally
            {
                TimeFinish = DateTime.UtcNow;

                if (_monitorCPUUsage)
                    _cpuUsage.ThreadRemove(this);

                RaiseThreadFinished(this);
            }
        }

        /// <summary>
        /// Method overridden in descendant class which will execute within the thread
        /// </summary>
        /// <param name="parameters">An object that contains data for the thread procedure</param>
        /// <returns>true if the thread is to continue, false will casue the thread to terminate</returns>
        protected virtual bool Run(object parameters)
        {
            return false;
        }

        /// <summary>
        /// Determines wether the thread has been asked to cancel
        /// 
        /// Descendant objects should check this and if set to true should quit doing whatever they are doing
        /// </summary>
        /// <returns>true if cancel has been requested, otherwise false</returns>
        protected bool HasCancelled()
        {
            if (_globalCancelRequested)
                return true;

            // still alive as we the thread has called this directly
            _lastCommunication = DateTime.UtcNow;

            return _cancel;
        }

        /// <summary>
        /// Descendant threads can call this method to show they are still "active"
        /// </summary>
        protected void IndicateNotHanging()
        {
            _lastCommunication = DateTime.UtcNow;
        }

#endregion Protected Methods

#region Internal Methods

        /// <summary>
        /// Long running threads will be pinged every 30 seconds to ensure they are not hanging
        /// 
        /// Descendants should override and call IndicateNotHanging() method
        /// </summary>
        /// <returns></returns>
        protected virtual bool Ping()
        {
            return true;
        }

        /// <summary>
        /// Updates the thread usage, called internally
        /// </summary>
        /// <param name="processTotal">Total Process Time</param>
        /// <param name="systemTotal">Total System Time</param>
        /// <param name="threadTotal">Total Thread Time</param>
        internal void UpdateThreadUsage(Int64 processTotal, Int64 systemTotal, TimeSpan threadTotal)
        {
            Int64 threadProcessTotalTicks = threadTotal.Ticks - _prevThreadTotal.Ticks;

            if (threadProcessTotalTicks > processTotal)
                threadProcessTotalTicks = processTotal;

            if (processTotal > 0.00m && threadProcessTotalTicks > 0)
                ProcessCpuUsage = 100.0m * threadProcessTotalTicks / processTotal;
            else
                ProcessCpuUsage = 0.00m;

            if (Math.Abs(PreviousProcessCpuUsage - ProcessCpuUsage) >= ThreadManager.ThreadCpuChangeNotification)
            {
                CPUUsageChanged = true;
            }

            PreviousProcessCpuUsage = ProcessCpuUsage;

            Int64 threadSystemTotalTicks = threadTotal.Ticks - _prevThreadTotal.Ticks;

            if (systemTotal > 0.00m && threadSystemTotalTicks > 0)
                SystemCpuUsage = 100.0m * threadSystemTotalTicks / systemTotal;
            else
                SystemCpuUsage = 0.00m;

            PreviousSystemCpuUsage = SystemCpuUsage;

            if (Math.Abs(PreviousSystemCpuUsage - SystemCpuUsage) >= ThreadManager.ThreadCpuChangeNotification)
            {
                CPUUsageChanged = true;
            }

            _prevThreadTotal = threadTotal;
        }

#endregion Internal Methods

#region Private Methods

#region Event Wrappers

        /// <summary>
        /// Raises an exception event
        /// </summary>
        /// <param name="error">Exception being raised</param>
        private void RaiseOnException(Exception error)
        {
            if (ExceptionRaised != null)
                ExceptionRaised(this, new ThreadManagerExceptionEventArgs(this, error));

            if (ThreadExceptionRaised != null)
                ThreadExceptionRaised(null, new ThreadManagerExceptionEventArgs(this, error));
        }

        /// <summary>
        /// Raises a thread start event
        /// </summary>
        /// <param name="thread"></param>
        private void RaiseThreadStart(ThreadManager thread)
        {
            ThreadManagerEventArgs args = new ThreadManagerEventArgs(thread);

            if (ThreadStarting != null)
                ThreadStarting(this, args);

            if (ThreadStarted != null)
                ThreadStarted(null, args);
        }

        /// <summary>
        /// Raises a thread finish event
        /// </summary>
        /// <param name="thread"></param>
        internal void RaiseThreadFinished(ThreadManager thread)
        {
            ThreadManagerEventArgs args = new ThreadManagerEventArgs(thread);

            if (ThreadFinishing != null)
                ThreadFinishing(this, args);

            if (ThreadStopped != null)
                ThreadStopped(null, args);


            if (thread.Name == "Cache Management Thread" && !thread.Cancelled && thread.UnResponsive)
            {
                Classes.ThreadManager.ThreadStart(_threadCacheManager,
                    "Cache Management Thread", ThreadPriority.Lowest);
            }

        }

        /// <summary>
        /// Event raised to indicate Thread has been requested to cancel
        /// </summary>
        /// <param name="thread"></param>
        private void RaiseThreadCancelRequested(ThreadManager thread)
        {
            if (ThreadCancelRequested != null)
                ThreadCancelRequested(this, new ThreadManagerEventArgs(thread));
        }

        /// <summary>
        /// Event raised to indicate all child threads have been requested to cancel
        /// </summary>
        /// <param name="thread"></param>
        private void RaiseThreadCancelChildrenRequested(ThreadManager thread)
        {
            if (ThreadCancelChildrenRequested != null)
                ThreadCancelChildrenRequested(this, new ThreadManagerEventArgs(thread));
        }

        /// <summary>
        /// Event raised when a thread is forced to close
        /// </summary>
        /// <param name="thread"></param>
        internal static void RaiseThreadForcedToClose(ThreadManager thread)
        {
            if (ThreadForcedToClose != null)
                ThreadForcedToClose(null, new ThreadManagerEventArgs(thread));

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
        internal static void RaiseThreadAbortForced(ThreadManager thread)
        {
            if (ThreadAbortForced != null)
                ThreadAbortForced(null, new ThreadManagerEventArgs(thread));
        }

        /// <summary>
        /// Raises an event to indicate the % of cpu usage for a thread has chenged by a specific amount
        /// </summary>
        internal static void RaiseThreadCpuChanged()
        {
            if (ThreadCpuChanged != null)
            {
                ThreadCpuChanged(null, EventArgs.Empty);
            }
        }

#endregion Event Wrappers

        /// <summary>
        /// Get's the memory usage for the thread
        /// </summary>
        /// <returns></returns>
        private double GetThreadMemoryUsage()
        {
            double Result = 0.00;

            //foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            //{
            //    if (thread.Id == ID)
            //    {
            //        thread.;
            //        return (true);
            //    }
            //}

            return Result;
        }

        /// <summary>
        /// Retrieves the Total Thread Time
        /// </summary>
        /// <param name="threadTotal">TimeSpan of total time</param>
        /// <returns>True if retrieved, otherwise false</returns>
        private bool GetThreadTimes(out TimeSpan threadTotal)
        {
            threadTotal = new TimeSpan();

            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                if (thread.Id == ID)
                {
                    threadTotal = thread.TotalProcessorTime;
                    return true;
                }
            }

            return false;
        }

#endregion Private Methods

#region Properties

#region Class Properties

        /// <summary>
        /// Indicates wether the thread has been cancelled or not
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancel;
            }
        }

        /// <summary>
        /// Date/time thread started
        /// </summary>
        public DateTime TimeStart { get; private set; }

        /// <summary>
        /// Date/time thread finished running
        /// </summary>
        public DateTime TimeFinish { get; private set; }

        /// <summary>
        /// if true and an unhandled error occurs then the thread will 
        /// still continue to run
        /// </summary>
        public bool ContinueIfGlobalException { get; set; }

        /// <summary>
        /// If true then the thread will run straight away, if false then thread will wait for RunInterval
        /// </summary>
        public bool RunAtStartup { get; set; }

        /// <summary>
        /// How often the thread should run
        /// </summary>
        public TimeSpan RunInterval { get; private set; }

        /// <summary>
        /// Number of milliseconds the thread should sleep after running
        /// </summary>
        public int SleepInterval { get; private set; }

        /// <summary>
        /// Name of the thread
        /// </summary>
        public string Name
        {
            get
            {
                return _thread.Name;
            }
        }

        /// <summary>
        /// Indicates the thread is finished and should be marked for removal from the collection
        /// </summary>
        public bool MarkedForRemoval
        {
            get
            {
                return _markedForRemoval;
            }
        }

        /// <summary>
        /// Retrieves the thread ID
        /// </summary>
        public int ThreadID { get; private set; }

        /// <summary>
        /// Actual ID for thread being watched
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Indicates wether the thread was unresponsive or not
        /// </summary>
        public bool UnResponsive { get { return _unresponsive; } }

        /// <summary>
        /// Hang Timeout for running thread
        /// 
        /// A value of 0 (zero) indicates the thread will not be checked for hanging
        /// </summary>
        public int HangTimeout { get; set; }

        /// <summary>
        /// Collectin of child threads
        /// </summary>
        public List<ThreadManager> ChildThreads { get { return _childThreads; } }

        /// <summary>
        /// Parent thread object
        /// </summary>
        public ThreadManager Parent { get { return _parentThread; } }

        /// <summary>
        /// Date/Time the thread Run method was executed
        /// </summary>
        protected DateTime LastRun
        {
            get
            {
                return _lastRun;
            }

            set
            {
                _lastRun = value;
            }
        }

        /// <summary>
        /// Threads usage within the process
        /// </summary>
        public decimal ProcessCpuUsage { get; private set; }

        /// <summary>
        /// Threads usage within the system
        /// </summary>
        public decimal SystemCpuUsage { get; private set; }

        /// <summary>
        /// Previous System CPU Usage
        /// </summary>
        public decimal PreviousSystemCpuUsage { get; private set; }

        /// <summary>
        /// Previous CPU Usage
        /// </summary>
        public decimal PreviousProcessCpuUsage { get; private set; }

        /// <summary>
        /// CPU Usage has changed
        /// </summary>
        public bool CPUUsageChanged { get; internal set; }

#endregion Class Properties

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

                        if (ThreadQueueCleared != null)
                            ThreadQueueCleared(null, EventArgs.Empty);
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

#endregion Properties

#region Class Events

        /// <summary>
        /// Event raised when the thread starts
        /// </summary>
        public event ThreadManagerEventDelegate ThreadStarting;

        /// <summary>
        /// Event raised when the thread finishes
        /// </summary>
        public event ThreadManagerEventDelegate ThreadFinishing;

        /// <summary>
        /// Event raised when an exception occurs
        /// </summary>
        public event ThreadManagerExceptionEventDelegate ExceptionRaised;

        /// <summary>
        /// Event raised when child threads have been requested to cancel
        /// </summary>
        public event ThreadManagerEventDelegate ThreadCancelChildrenRequested;

        /// <summary>
        /// Event raised when a Thread has been requested to cancel
        /// </summary>
        public event ThreadManagerEventDelegate ThreadCancelRequested;

#endregion Class Events

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
    }

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
            using (TimedLock.Lock(ThreadManager._lockObject))
            {
                ThreadManager._cpuUsage.GetProcessUsage();

                for (int i = ThreadManager._threadList.Count - 1; i >= 0; i--)
                {
                    ThreadManager item = ThreadManager._threadList[i];

                    if (ThreadManager._checkForHangingThreads)
                    {
                        TimeSpan hangingSpan = DateTime.UtcNow - item._lastCommunication;

                        if (item.HangTimeout > 0 && !item._cancel && hangingSpan.TotalMinutes > item.HangTimeout)
                        {
                            //set time out long enough for the thread to clear itself out
                            // if it doesn't then we will force the closure
                            item.CancelThread(10000, true);
                        }
                    }

                    if (item.MarkedForRemoval)
                    {
                        item.ThreadFinishing -= ThreadManager.thread_ThreadFinishing;
                        _countOfThreads--;
                        _threadList.Remove(item);
                    }

                    if (item._cancel)
                    {
                        TimeSpan span = DateTime.UtcNow - item._cancelRequested;

                        if (span.TotalMilliseconds > item._cancelTimeoutMilliseconds)
                        {
                            if (!item.MarkedForRemoval)
                            {
                                _countOfThreads--;
                                item.ThreadFinishing -= ThreadManager.thread_ThreadFinishing;
                                _threadList.Remove(item);
                            }

                            ThreadManager.RaiseThreadForcedToClose(item);
                            ThreadManager._abortPool.Add(item);
                        }
                    }

                    // if there is enought space, can we run one of the threads in the pool?
                    if (ThreadManager.AllowThreadPool && ThreadManager._threadPool.Count > 0)
                    {
                        while (ThreadManager._threadList.Count < ThreadManager.MaximumRunningThreads)
                        {
                            ThreadManager nextRunItem = ThreadManager._threadPool[0];

                            ThreadManager._threadPool.RemoveAt(0);
                            ThreadManager.RaiseThreadQueueRemoveItem(nextRunItem);

                            ThreadManager.ThreadStart(nextRunItem, nextRunItem.Name, nextRunItem._thread.Priority);
                        }
                    }
                }
            }

            return !HasCancelled();
        }
    }

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
            for (int i = ThreadManager._abortPool.Count - 1; i >= 0; i--)
            {
                ThreadManager item = ThreadManager._abortPool[i];

                ThreadManager._abortPool.Remove(item);

                item.Abort();

                // play niceley
                if (HasCancelled())
                    return false;
            }

            return true;
        }
    }


    /// <summary>
    /// thread that clears cached values
    /// </summary>
    internal class ThreadCachManager : Classes.ThreadManager
    {
        internal ThreadCachManager()
            : base(null, new TimeSpan(0, 0, 15))
        {
            RunAtStartup = false;
            HangTimeout = 30; // no response for 30 minutes then kill
            ContinueIfGlobalException = true;
        }

        protected override bool Run(object parameters)
        {
            Shared.Classes.CacheManager.CleanAllCaches();
            return !HasCancelled();
        }
    }
}
