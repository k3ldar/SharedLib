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

using Shared.Classes;

namespace Shared.Abstractions
{
    /// <summary>
    /// Cache manager factory for retrieving individual caches
    /// </summary>
    public interface ICacheManagerFactory
    {
        /// <summary>
        /// Retreives a cache by name if it exists.
        /// </summary>
        /// <param name="cacheName">Name of cache to retrieve</param>
        /// <returns>CacheManager instance if found, otherwise null</returns>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        CacheManager GetCacheIfExists(string cacheName);

        /// <summary>
        /// Retreives a cache by name if it exists.
        /// </summary>
        /// <param name="cacheName">Name of cache to retrieve</param>
        /// <returns>CacheManager instance if found, otherwise InvalidOperationException</returns>
        /// <exception cref="InvalidOperationException">Thrown if the cache does not exist</exception>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        CacheManager GetCache(string cacheName);

        /// <summary>
        /// Determines whether a cache exists or not
        /// </summary>
        /// <param name="cacheName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        bool CacheExists(string cacheName);

        /// <summary>
        /// Creates an instance of CacheManager
        /// 
        /// Maximum age will not be reset, Items can be cleared.
        /// </summary>
        /// <param name="cacheName">Name of cache, must be unique</param>
        /// <param name="maximumAge">Maximum age of items within the cache</param>
        /// <returns>CacheManager instance</returns>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown if a cache by cacheName already exists</exception>
        /// 
        CacheManager CreateCache(string cacheName, TimeSpan maximumAge);

        /// <summary>
        /// Creates an instance of CacheManager
        /// </summary>
        /// <param name="cacheName">Name of cache, must be unique</param>
        /// <param name="maximumAge">Maximum age of items within the cache</param>
        /// <param name="resetMaximumAge">Reset age of item when retrieved</param>
        /// <param name="allowClearAll">Allows the cache to be cleared automatically</param>
        /// <returns>CacheManager instance</returns>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown if a cache by cacheName already exists</exception>
        /// 
        CacheManager CreateCache(string cacheName, TimeSpan maximumAge,
            bool resetMaximumAge, bool allowClearAll);

        /// <summary>
        /// Removes a cache, by name if it exists.
        /// </summary>
        /// <param name="cacheName">Name of cache, must be unique</param>
        /// <exception cref="ArgumentNullException">Thrown if cacheName is null or empty</exception>
        void RemoveCache(string cacheName);

        /// <summary>
        /// Forces a clean up of all caches, removing all items.  This will not clear any cache manager instance where the AllowClearAll flag is set to false.
        /// </summary>
        void ClearAllCaches();

        /// <summary>
        /// Forces a clean up of all caches, removing older items that have expired
        /// </summary>
        void CleanAllCaches();
    }
}
