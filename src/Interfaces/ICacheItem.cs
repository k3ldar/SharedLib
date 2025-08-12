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
    public interface ICacheItem
    {
        DateTime Created { get; }

        DateTime LastUpdated { get; }
        
        string Name { get; }

        [Obsolete("deprecated, use GetValue<T> instead")]
        object Value { get; }

        object GetValue(bool ignoreReset = false);

        T GetValue<T>(bool ignoreReset = false);

        bool IsNull { get; }

        bool IsType<T>();
    }
}