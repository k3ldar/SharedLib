﻿/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2012 Simon Carter
 *
 *  Purpose:  Event log and Error log cache management
 *
 */
using System;
using System.Collections.Generic;
using System.Text;

using Shared.Classes;

namespace Shared
{
    internal class LoggingErrorCache : CacheItem
    {
        #region Constructor

        internal LoggingErrorCache(string name, object value)
            : base (name, value)
        {
            NumberOfErrors = 0;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Error log file name
        /// </summary>
        internal string FileName { get; set; }

        /// <summary>
        /// Number of times error has occurred
        /// </summary>
        internal int NumberOfErrors { get; private set; }

        #endregion Properties

        #region Internal Methods

        internal void IncrementErrors()
        {
            NumberOfErrors++;
            LastUpdated = DateTime.Now;
        }

        #endregion Internal Methds
    }
}