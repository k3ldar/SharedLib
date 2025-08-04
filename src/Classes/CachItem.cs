/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  Cache Item, used in conjunction with CacheManager
 *
 */
using System;

namespace Shared.Classes
{
    /// <summary>
    /// Cached item
    /// 
    /// Used by Cache manager for a cached item
    /// </summary>
    internal class CacheItem : ICacheItem
    {
        #region Private Members

        internal DateTime _lastUpdated;

        internal int _resetCount = 0;

        private object _value;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of cached item</param>
        /// <param name="value">Cached Item</param>
        public CacheItem(string name, object value)
        {
            Created = DateTime.UtcNow;
            _lastUpdated = Created;
            _resetCount = 0;
            Name = name;
            Index = Utilities.Hash(name);

            _value = value;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Date/Time cache Item set
        /// </summary>
        public DateTime Created { private set; get; }

        /// <summary>
        /// Date/Time cached item last updated
        /// </summary>
        public DateTime LastUpdated
        {
            protected set
            {
                _lastUpdated = value;
            }

            get
            {
                return _lastUpdated;
            }
        }

        /// <summary>
        /// Name of cached item
        /// </summary>
        public string Name { private set; get; }

        /// <summary>
        /// Reset's the age of the item when retrieved
        /// </summary>
        internal bool ResetMaximumAge { get; set; }

        /// <summary>
        /// Cached item
        /// </summary>
        public object Value
        {
            get
            {
                if (ResetMaximumAge)
                {
                    _resetCount++;
                    LastUpdated = DateTime.UtcNow;
                }

                return _value;
            }
        }

        public bool IsNull => _value == null;

        public bool IsType<T>()
        {
            if (_value == null)
                return false;

            return _value is T;
        }

        /// <summary>
        /// Search index for fast lookup
        /// </summary>
        internal UInt64 Index { get; set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Returns the cached item
        /// </summary>
        /// <param name="ignoreReset">bool, if ResetMaximumAge is true and ignoreReset = false (default) then LastUpdated will be reset</param>
        /// <returns>CachedItem's value</returns>
        public object GetValue(bool ignoreReset = false)
        {
            if (!ignoreReset && ResetMaximumAge)
            {
                _resetCount++;
                LastUpdated = DateTime.UtcNow;
            }

            return _value;
        }

        /// <summary>
        /// Returns the cached item
        /// </summary>
        /// <param name="ignoreReset">bool, if ResetMaximumAge is true and ignoreReset = false (default) then LastUpdated will be reset</param>
        /// <returns>CachedItem's value</returns>
        public T GetValue<T>(bool ignoreReset = false)
        {
            if (!ignoreReset && ResetMaximumAge)
            {
                _resetCount++;
                LastUpdated = DateTime.UtcNow;
            }

            return (T)_value;
        }

        #endregion Public Methods
    }
}
