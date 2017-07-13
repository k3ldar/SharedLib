/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Class for managing log files
 *
 */
using System;
using System.Collections.Generic;
using System.Text;

using Shared.Classes;

namespace Shared.Logging
{
    /// <summary>
    /// Class which manages the error memory cache and the age of log files
    /// 
    /// runs every 5 minutes
    /// </summary>
    internal class LoggingThread : ThreadManager
    {
        #region Constructors

        internal LoggingThread(int maximumLogAge)
            : base(maximumLogAge, new TimeSpan(0, 5, 0))
        {
            this.HangTimeout = 60;
        }

        #endregion Constructors

        #region Overridden Methods

        protected override bool Run(object parameters)
        {
            EventLog.ClearCache();
            EventLog.ArchiveOldLogFiles((int)parameters);

            return (!HasCancelled());
        }

        #endregion Overridden Methods
    }
}
