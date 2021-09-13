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

using Shared;
using Shared.Classes;

using ct = System.Runtime.InteropServices.ComTypes;

namespace SharedLib.Win
{
    /// <summary>
    /// Windows specific implementation of ICpuUsage, allows the collection of thread cpu usage
    /// </summary>
    public class WindowsCpuUsage : ICpuUsage
    {
        #region Private Members

        private readonly object _lockObject = new object();

        private ct.FILETIME _prevSysKernel;
        private ct.FILETIME _prevSysUser;
        private TimeSpan _prevProcTotal;

        private decimal _cpuUsage;
        private DateTime _lastRun;

        private readonly List<ThreadManager> _watchedThreads = new List<ThreadManager>();

        private readonly Dictionary<int, TimeSpan> _threadTimes = new Dictionary<int, TimeSpan>();

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public WindowsCpuUsage()
        {
            _cpuUsage = 0.0m;
            _lastRun = DateTime.MinValue;

            _prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
            _prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;

            InitialiseTimes();
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Percentage of cpu usage within the process from unmanaged threads (including Main Thread)
        /// </summary>
        public decimal OtherProcessCPUUsage { get; private set; }

        /// <summary>
        /// One of the threads CPU Usage has changed when getting the stats
        /// </summary>
        public bool ThreadCPUChanged { get; private set; }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// Determines wether 500ms has passed or not
        /// </summary>
        private bool EnoughTimePassed
        {
            get
            {
                const int minimumElapsedMS = 500;
                TimeSpan sinceLast = DateTime.UtcNow - _lastRun;
                return sinceLast.TotalMilliseconds > minimumElapsedMS;
            }
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Adds a thread to the list of threads being watched
        /// </summary>
        /// <param name="thread">Thread to start watching</param>
        public void ThreadAdd(ThreadManager thread)
        {
            using (TimedLock.Lock(_lockObject))
            {
                foreach (ThreadManager watchedThread in _watchedThreads)
                {
                    if (thread.ThreadID == watchedThread.ThreadID)
                        return;
                }

                _watchedThreads.Add(thread);

                if (!_threadTimes.ContainsKey(thread.ID))
                    _threadTimes.Add(thread.ID, new TimeSpan());
            }
        }

        /// <summary>
        /// Removes a thread from list of threads being watched
        /// </summary>
        /// <param name="thread">Thread to stop watching</param>
        public void ThreadRemove(ThreadManager thread)
        {
            try
            {
                int attempt = 0;
                ThreadRemoveInternal(thread, attempt);

                if (_threadTimes.ContainsKey(thread.ID))
                    _threadTimes.Remove(thread.ID);
            }
            catch (Exception err)
            {
                EventLog.Add(err, String.Format("Watched Threads: {0}", _watchedThreads.Count));
            }
        }

        /// <summary>
        /// Returns the number of threads being watched
        /// </summary>
        /// <returns></returns>
        public int ThreadUsageCount()
        {
            return _watchedThreads.Count;
        }

        /// <summary>
        /// Retrieves the n'th watched thread
        /// </summary>
        /// <param name="index">Index of thread being watched</param>
        /// <returns>ThreadUsage object</returns>
        public ThreadManager ThreadUsageGet(int index)
        {
            return _watchedThreads[index];
        }

        /// <summary>
        /// Retrieves the total process usage for the current process
        /// </summary>
        /// <returns></returns>
        public decimal GetProcessUsage()
        {
            decimal Result = _cpuUsage;
            Debug.Print(nameof(GetProcessUsage));
            if (!EnoughTimePassed)
            {
                Debug.Print("not enough time has passed");
                return Result;
            }

            using (TimedLock.Lock(_lockObject))
            {
                ThreadCPUChanged = false;

                Process process = Process.GetCurrentProcess();
                TimeSpan procTime = process.TotalProcessorTime;


                if (!WinDLLImports.GetSystemTimes(out ct.FILETIME sysIdle, out ct.FILETIME sysKernel, out ct.FILETIME sysUser))
                {
                    return (Result);
                }

                // get thread times
                foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                {
                    if (_threadTimes.ContainsKey(thread.Id))
                    {
                        _threadTimes[thread.Id] = thread.TotalProcessorTime;
                    }
                }


                Int64 sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
                Int64 sysUserDiff = SubtractTimes(sysUser, _prevSysUser);

                Int64 sysTotal = sysKernelDiff + sysUserDiff;

                Int64 processTotal = procTime.Ticks - _prevProcTotal.Ticks;

                if (sysTotal > 0 && processTotal > 0)
                    _cpuUsage = 100.0m * processTotal / sysTotal;
                else
                    _cpuUsage = 0;

                _prevProcTotal = procTime;
                _prevSysKernel = sysKernel;
                _prevSysUser = sysUser;
                decimal otherUsage = 0.0m;

                foreach (ThreadManager watchedThread in _watchedThreads)
                {
                    TimeSpan span;

                    if (_threadTimes.ContainsKey(watchedThread.ID))
                        span = _threadTimes[watchedThread.ID];
                    else
                        span = new TimeSpan();

                    watchedThread.CPUUsageChanged = false;
                    watchedThread.UpdateThreadUsage(processTotal, sysTotal, span);

                    if (!ThreadCPUChanged && watchedThread.CPUUsageChanged)
                        ThreadCPUChanged = true;

                    otherUsage += watchedThread.ProcessCpuUsage;
                }

                OtherProcessCPUUsage = Utilities.CheckMinMax(100 - otherUsage, 0.0m, 100.00m);
                _lastRun = DateTime.UtcNow;

                Result = _cpuUsage;
            }

            Debug.Print("Processing cpu usage");

            if (ThreadCPUChanged)
            {
                Debug.Print("Thread CPU Changed");
                ThreadManager.RaiseThreadCpuChanged();
            }

            return Result;
        }

        public int GetCurrentThreadId()
        {
            return (int)WinDLLImports.GetCurrentThreadId();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Removes a thread from list of threads being watched
        /// </summary>
        /// <param name="thread">Thread to stop watching</param>
        /// <param name="attempt">current attempt</param>
        private void ThreadRemoveInternal(ThreadManager thread, int attempt)
        {
            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    foreach (ThreadManager watchedThread in _watchedThreads)
                    {
                        if (watchedThread.ThreadID == thread.ThreadID)
                        {
                            _watchedThreads.Remove(watchedThread);
                            return;
                        }
                    }
                }
            }
            catch (LockTimeoutException err)
            {
                if (attempt < 5)
                {
                    EventLog.Add(err, String.Format("Watched Threads: {0}", _watchedThreads.Count));
                    attempt++;
                    ThreadRemoveInternal(thread, attempt);
                }
                else
                {
                    EventLog.Add(err, String.Format("Watched Threads: {0}", _watchedThreads.Count));
                    throw;
                }
            }
        }

        private void InitialiseTimes()
        {
            Process process = Process.GetCurrentProcess();
            //TimeSpan procTime = process.TotalProcessorTime;


            if (WinDLLImports.GetSystemTimes(out ct.FILETIME sysIdle, out ct.FILETIME sysKernel, out ct.FILETIME sysUser))
            {
                _prevSysKernel = sysKernel;
                _prevSysUser = sysUser;
            }
            else
                throw new Exception("Failed to get System Times");
        }

        private Int64 SubtractTimes(ct.FILETIME a, ct.FILETIME b)
        {
            Int64 aInt = ((Int64)(a.dwHighDateTime << 32)) | (Int64)a.dwLowDateTime;
            Int64 bInt = ((Int64)(b.dwHighDateTime << 32)) | (Int64)b.dwLowDateTime;

            return aInt - bInt;
        }

        #endregion Private Methods
    }
}
