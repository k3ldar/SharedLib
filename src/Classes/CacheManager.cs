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

#pragma warning disable IDE1005 // Delegate invocation can be simplified

namespace Shared.Classes
{
    /// <summary>
    /// Cache Manager
    /// </summary>
    public class CacheManager
    {
        #region Private Static Members

        /// <summary>
        /// cache lock object
        /// </summary>
        private readonly object _cacheLockObject = new object();

        /// <summary>
        /// List of all cache's created by application
        /// </summary>
        internal static readonly List<CacheManager> _allCaches = new List<CacheManager>();

        #endregion Private Static Members

        #region Private Members

        private readonly Dictionary<string, CacheItem> _cachedItems = null;

        #endregion Private Members

        #region Constructors / Destructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cacheName">Name of cache, must be unique</param>
        /// <param name="maximumAge">Maximum age of items within the cache</param>
        /// <param name="resetMaximumAge">Reset age of item when retrieved</param>
        /// <param name="allowClearAll">Allows the cache to be cleared automatically</param>
        public CacheManager (string cacheName, TimeSpan maximumAge, 
            bool resetMaximumAge = false, bool allowClearAll = true)
        {
            _cachedItems = new Dictionary<string, CacheItem>();
            Name = cacheName;
            ResetMaximumAge = resetMaximumAge;
            AllowClearAll = allowClearAll;

            if (maximumAge.TotalSeconds == 0.00d)
                maximumAge = new TimeSpan(2, 0, 0);

            MaximumAge = maximumAge;
            _allCaches.Add(this);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~CacheManager()
        {
            _allCaches.Remove(this);
        }

        #endregion Constructors / Destructors

        #region Properties

        /// <summary>
        /// Cache Manager Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// If false, cache will not be cleared when ClearAllCaches is called
        /// </summary>
        public bool AllowClearAll { get; set; }

        /// <summary>
        /// Maximum age of cached item
        /// 
        /// once an item reaches this age it will be automatically removed from the list
        /// </summary>
        public TimeSpan MaximumAge { get; set; }

        /// <summary>
        /// If true, whenever the item is retrieved from the cache then the age of the item is reset 
        /// </summary>
        public bool ResetMaximumAge { get; set; }

        /// <summary>
        /// Returns number of cached items
        /// </summary>
        public int Count
        {
            get
            {
                return _cachedItems.Count;
            }
        }

        /// <summary>
        /// Returns a list of all cached items
        /// </summary>
        /// <returns>List of CacheItem's if found, otherwise null</returns>
        public List<CacheItem> Items
        {
            get
            {
                using (TimedLock.Lock(_cacheLockObject))
                {
                    List<CacheItem> Result = new List<CacheItem>();

                    foreach (KeyValuePair<string, CacheItem> item in _cachedItems)
                    {
                        Result.Add(item.Value);
                    }

                    return Result;
                }
            }
        }

        #endregion Properties

        #region Static Public Methods

        /// <summary>
        /// Forces a clean up of all caches, removing older items that have expired
        /// </summary>
        public static void CleanAllCaches()
        {
            foreach (CacheManager cManager in _allCaches)
            {
                try
                {
                    cManager.CleanCachedItems();
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
        public static void ClearAllCaches()
        {
            foreach (CacheManager cManager in _allCaches)
            {
                try
                {
                    if (cManager.AllowClearAll)
                        cManager.Clear();
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
        public static int GetCount()
        {
            return _allCaches.Count;
        }

        /// <summary>
        /// Get's the name of the cache
        /// </summary>
        /// <param name="index">Index of the caches</param>
        /// <returns>Cache name</returns>
        public static string GetCacheName(int index)
        {
            return _allCaches[index].Name;
        }

        /// <summary>
        /// Get's the maximum age of a cache
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TimeSpan GetCacheAge(int index)
        {
            return _allCaches[index].MaximumAge;
        }

        /// <summary>
        /// Returns the number of items in the cache
        /// </summary>
        /// <param name="index">Index of cache</param>
        /// <returns>integer</returns>
        public static int GetCacheCount(int index)
        {
            return _allCaches[index].Count;
        }

        #endregion Static Public Methods

        #region Public Methods

        /// <summary>
        /// Add's an item to the cached list
        /// </summary>
        /// <param name="name">Name of cached item</param>
        /// <param name="value">Cached item value</param>
        /// <param name="deleteIfExists">if true and the list contains a value with the same name, then the existing item is deleted</param>
        public bool Add(string name, CacheItem value, bool deleteIfExists = false)
        {
            if (value == null)
                throw new Exception("invalid value parameter");

            // is the item is already cached and we are not renewing it
            if (_cachedItems.ContainsKey(name) && !deleteIfExists)
                    return false;

            using (TimedLock.Lock(_cacheLockObject))
            {
                if (_cachedItems.ContainsKey(name))
                {
                    RaiseRemoveItem(_cachedItems[name]);
                    _cachedItems.Remove(name);
                }

                value.ResetMaximumAge = ResetMaximumAge;
                _cachedItems.Add(name, value);
                RaiseAddItem(value);

                return true;
            }
        }

        /// <summary>
        /// Returns a cached item
        /// 
        /// If the item isn't found, an event is raised to retrieve the item, subsequently adding it to the list of items
        /// </summary>
        /// <param name="name">Name of cached item</param>
        /// <returns>CacheItem if found, otherwise null</returns>
        public CacheItem Get (string name)
        {
            CacheItem Result = null;

            using (TimedLock.Lock(_cacheLockObject))
            {
                if (_cachedItems.ContainsKey(name))
                {
                    Result = _cachedItems[name];
                }
                else
                {
                    // raise event to get the cached item
                    Result = RaiseItemNotFound(name);

                    // if we have a new cached item, add to list
                    if (Result != null)
                        _cachedItems.Add(name, Result);
                }
            }

            return Result;
        }

        /// <summary>
        /// Clears all cache items
        /// </summary>
        public void Clear()
        {
            using (TimedLock.Lock(_cacheLockObject))
            {
                List<string> removeItems = new List<string>();
                try
                {
                    foreach (KeyValuePair<string, CacheItem> item in _cachedItems)
                    {
                        removeItems.Add(item.Key);
                    }

                    foreach (string item in removeItems)
                    {
                        if (_cachedItems.ContainsKey(item))
                            _cachedItems.Remove(item);
                    }
                }
                finally
                {
                    removeItems.Clear();
                }
            }
        }

        /// <summary>
        /// Removes a specific item from the cache
        /// </summary>
        /// <param name="item">Item to be removed</param>
        public void Remove(CacheItem item)
        {
            using (TimedLock.Lock(_cacheLockObject))
            {
                if (_cachedItems.ContainsKey(item.Name))
                    _cachedItems.Remove(item.Name);
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Called to clean cached items, remove those that have expired etc
        /// </summary>
        internal void CleanCachedItems()
        {
            using (TimedLock.Lock(_cacheLockObject))
            {
                List<string> removeItems = new List<string>();
                try
                {
                    foreach (KeyValuePair<string, CacheItem> item in _cachedItems)
                    {
                        TimeSpan age = DateTime.UtcNow - item.Value.LastUpdated;

                        if (age.TotalSeconds > MaximumAge.TotalSeconds)
                            removeItems.Add(item.Key);
                    }

                    foreach (string item in removeItems)
                    {
                        if (_cachedItems.ContainsKey(item))
                        {
                            RaiseRemoveItem(_cachedItems[item]);
                            _cachedItems.Remove(item);
                        }
                    }
                }
                finally
                {
                    removeItems.Clear();
                }
            }
        }

        #endregion Internal Methods

        #region Event Wrappers

        private void RaiseAddItem(CacheItem item)
        {
            if (ItemAdd != null)
                ItemAdd(this, new CacheItemArgs(item));
        }

        private void RaiseRemoveItem(CacheItem item)
        {
            if (ItemRemoved != null)
                ItemRemoved(this, new CacheItemArgs(item));
        }

        private CacheItem RaiseItemNotFound(string name)
        {
            CacheItem Result = null;

            if (ItemNotFound != null)
            {
                CacheItemNotFoundArgs args = new CacheItemNotFoundArgs(name);

                ItemNotFound(this, args);
                
                Result = args.CachedItem;
            }

            return Result;
        }

        #endregion Event Wrappers

        #region Events

        /// <summary>
        /// Event raised when a cached item can not be found
        /// </summary>
        public event CacheItemNotFoundDelegate ItemNotFound;

        /// <summary>
        /// Event raised when item added to the cache
        /// </summary>
        public event CacheItemDelegate ItemAdd;

        /// <summary>
        /// Event raised when item removed from the cache
        /// </summary>
        public event CacheItemDelegate ItemRemoved;

        #endregion Events
    }
}
