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
using Shared.Classes;

namespace Shared
{
    public interface ICpuUsage
    {
        decimal OtherProcessCPUUsage { get; }

        bool ThreadCPUChanged { get; }

        void ThreadAdd(ThreadManager thread);

        void ThreadRemove(ThreadManager thread);

        int ThreadUsageCount();

        ThreadManager ThreadUsageGet(int index);

        decimal GetProcessUsage();

        int GetCurrentThreadId();
    }
}
