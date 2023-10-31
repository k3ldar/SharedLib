/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2021 Simon Carter
 *
 *  Purpose:  Managed cached items
 *
 */
using System;
using System.Collections.Generic;

using Shared.Abstractions;

namespace Shared.Classes
{
    public sealed class CacheManagerFactory : ICacheManagerFactory
    {
        #region Private Members

        internal static readonly Dictionary<string, CacheManager> _allCaches = new Dictionary<string, CacheManager>();


        /// <summary>
        /// List of all cache's created by application
        /// </summary>
        private static readonly object _dictionaryLockObject = new object();

        #endregion Private Members

        public bool CacheExists(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            return CacheManager._allCaches.ContainsKey(cacheName);
        }

        public void CleanAllCaches()
        {
            CacheManager.CleanAllCaches();
        }

        public void ClearAllCaches()
        {
            CacheManager.ClearAllCaches();
        }

        public CacheManager CreateCache(string cacheName, TimeSpan maximumAge)
        {
            return CreateCache(cacheName, maximumAge, false, true);
        }

        public CacheManager CreateCache(string cacheName, TimeSpan maximumAge, bool resetMaximumAge, bool allowClearAll)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            if (CacheManager.CacheExists(cacheName))
                throw new InvalidOperationException("Cache already exists");

            return new CacheManager(cacheName, maximumAge, resetMaximumAge, allowClearAll);
        }

        public CacheManager GetCache(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            if (CacheManager.CacheExists(cacheName))
                return CacheManager.GetCache(cacheName);

            throw new InvalidOperationException();
        }

        public CacheManager GetCacheIfExists(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            if (CacheManager.CacheExists(cacheName))
                return CacheManager.GetCache(cacheName);

            return null;
        }

        public void RemoveCache(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            CacheManager.RemoveCacheManager(cacheName);
        }
    }
}
