/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 - 2021 Simon Carter
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
    public partial class ThreadManager
    {
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
            RunInterval = runInterval;
            _delayStart = delayStart;
            _parentThread = null;
            RunAtStartup = runAtStart;
            SleepInterval = Utilities.CheckMinMax(sleepInterval, 0, 2000);
            _parameters = parameters;
            ContinueIfGlobalException = true;
            _parentThread = parent;

            _monitorCPUUsage = monitorCPUUsage;

            PreviousProcessCpuUsage = 0.0m;

            // each thread can have it's own timeout period, set as default to global value 
            HangTimeoutSpan = ThreadHangTimeout;

            if (parent != null)
                parent.ChildThreads.Add(this);

            ProcessCpuUsage = 0;
            SystemCpuUsage = 0;
        }

        #endregion Constructors

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
#if !NET_CORE
            _thread.Abort();
#endif
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
            ThreadID = _cpuUsage.GetCurrentThreadId();


            ID = Thread.CurrentThread.ManagedThreadId;

            if (_monitorCPUUsage)
                _cpuUsage.ThreadAdd(this);

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

            LastRun = DateTime.UtcNow.AddDays(RunAtStartup ? -1 : 0);
            DateTime lastPing = DateTime.UtcNow;

            using (TimedLock.Lock(_lockObject))
            {
                _threadList.Add(this);
                _countOfThreads++;
            }


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

                        TimeSpan span = DateTime.UtcNow - LastRun;

                        // run the thread
                        if (span.TotalMilliseconds > RunInterval.TotalMilliseconds)
                        {
                            if (!Run(_parameters))
                                return;

                            LastRun = DateTime.UtcNow;
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

                if (_monitorCPUUsage && _cpuUsage != null)
                    _cpuUsage.ThreadRemove(this);

                using (TimedLock.Lock(_lockObject))
                {
                    _threadList.Remove(this);
                    _countOfThreads--;
                }

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
        public void UpdateThreadUsage(Int64 processTotal, Int64 systemTotal, TimeSpan threadTotal)
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

        public bool IsBackGround { get; internal set; }

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
        /// Actual Managed ID for thread being watched
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
        [Obsolete("Replaced with HangTimeoutSpan, will be removed in future version", true)]
        public int HangTimeout { get => Convert.ToInt32(HangTimeoutSpan.TotalMinutes); set => HangTimeoutSpan = TimeSpan.FromMinutes(value); }

        public TimeSpan HangTimeoutSpan { get; set; }

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
        protected DateTime LastRun { get; private set; }

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
        public bool CPUUsageChanged { get; set; }

        #endregion Class Properties

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
    }
}
