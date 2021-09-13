/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  Managed cached items
 *
 */
using System;
using System.Collections.Generic;

namespace Shared.Classes
{
    public partial class CacheManager
    {
        #region Private Static Members


        /// <summary>
        /// List of all cache's created by application
        /// </summary>
        internal static readonly Dictionary<string, CacheManager> _allCaches = new Dictionary<string, CacheManager>();

        private static readonly object _dictionaryLockObject = new object();

        #endregion Private Static Members

        #region Static Public Methods

        public static void AddCache(string name, CacheManager cacheManager)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                if (CacheExists(name))
                    throw new InvalidOperationException("cache already exists");

                _allCaches[name] = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            }
        }

        public static bool RemoveCacheManager(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                if (_allCaches.ContainsKey(name))
                    _allCaches.Remove(name);

                return _allCaches.ContainsKey(name);
            }
        }

        /// <summary>
        /// Forces a clean up of all caches, removing older items that have expired
        /// </summary>
        internal static void CleanAllCaches()
        {
            foreach (KeyValuePair<string, CacheManager> cManager in _allCaches)
            {
                try
                {
                    cManager.Value.CleanCachedItems();
                }
                catch (Exception err)
                {
                    EventLog.Add(err);
                }
            }
        }

        /// <summary>
        /// Forces a clean up of all caches, removing all items
        /// </summary>
        internal static void ClearAllCaches()
        {
            foreach (KeyValuePair<string, CacheManager> cManager in _allCaches)
            {
                try
                {
                    if (cManager.Value.AllowClearAll)
                        cManager.Value.Clear();
                }
                catch (Exception err)
                {
                    EventLog.Add(err);
                }
            }
        }

        /// <summary>
        /// Returns the numer of caches
        /// </summary>
        /// <returns></returns>
        internal static bool CacheExists(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                return _allCaches.ContainsKey(cacheName);
            }
        }

        /// <summary>
        /// Returns the numer of caches
        /// </summary>
        /// <returns></returns>
        internal static CacheManager GetCache(string cacheName)
        {
            if (String.IsNullOrEmpty(cacheName))
                throw new ArgumentNullException(nameof(cacheName));

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                if (_allCaches.ContainsKey(cacheName))
                    return _allCaches[cacheName];
            }

            return null;
        }

        /// <summary>
        /// Returns the numer of caches
        /// </summary>
        /// <returns></returns>
        public static int GetCount()
        {
            using (TimedLock.Lock(_dictionaryLockObject))
            {
                return _allCaches.Count;
            }
        }

        /// <summary>
        /// Get's the name of the cache
        /// </summary>
        /// <param name="index">Index of the caches</param>
        /// <returns>Cache name</returns>
        public static string GetCacheName(int index)
        {
            int i = 0;

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                foreach (KeyValuePair<string, CacheManager> cManager in _allCaches)
                {
                    if (i == index)
                        return cManager.Value.Name;

                    i++;
                }
            }

            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Get's the maximum age of a cache
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TimeSpan GetCacheAge(int index)
        {
            int i = 0;

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                foreach (KeyValuePair<string, CacheManager> cManager in _allCaches)
                {
                    if (i == index)
                        return cManager.Value.MaximumAge;

                    i++;
                }
            }

            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Returns the number of items in the cache
        /// </summary>
        /// <param name="index">Index of cache</param>
        /// <returns>integer</returns>
        public static int GetCacheCount(int index)
        {
            int i = 0;

            using (TimedLock.Lock(_dictionaryLockObject))
            {
                foreach (KeyValuePair<string, CacheManager> cManager in _allCaches)
                {
                    if (i == index)
                        return cManager.Value.Count;

                    i++;
                }
            }

            throw new IndexOutOfRangeException();
        }

        #endregion Static Public Methods

    }
}
