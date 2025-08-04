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
    public interface ICacheManager
    {
        bool AllowClearAll { get; set; }

        int Count { get; }

        List<ICacheItem> Items { get; }

        TimeSpan MaximumAge { get; set; }

        string Name { get; }

        bool ResetMaximumAge { get; set; }

        event CacheItemDelegate ItemAdd;
        event CacheItemNotFoundDelegate ItemNotFound;
        event CacheItemDelegate ItemRemoved;

        ICacheItem Add(string name, object value, bool deleteIfExists = false);

        void Clear();
        
        ICacheItem Get(string name);
        
        void Remove(ICacheItem item);
    }
}