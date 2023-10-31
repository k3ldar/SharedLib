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
using System.Runtime.InteropServices;

using ct = System.Runtime.InteropServices.ComTypes;

namespace SharedLib.Win
{
    public static class WinDLLImports
    {
        /// <summary>
        /// Get's the current thread ID (Win API)
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(
                    out ct.FILETIME lpIdleTime,
                    out ct.FILETIME lpKernelTime,
                    out ct.FILETIME lpUserTime);
    }
}
