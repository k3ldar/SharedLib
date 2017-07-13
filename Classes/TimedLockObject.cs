using System;
using System.Threading;

namespace Shared.Classes
{
    // Thanks to Eric Gunnerson for recommending this be a struct rather
    // than a class - avoids a heap allocation.
    // (In Debug mode, we make it a class so that we can add a finalizer
    // in order to detect when the object is not freed.)
    // Thanks to Chance Gillespie and Jocelyn Coulmance for pointing out
    // the bugs that then crept in when I changed it to use struct...
    // https://www.interact-sw.co.uk/iangblog/2004/03/23/locking

    /// <summary>
    /// Timed lock 
    /// </summary>
    [Serializable]
    public struct TimedLock : IDisposable
    {
        /// <summary>
        /// Maximum Attempts at obtaining a lock
        /// </summary>
        private static sbyte _maxAttempts = 3;

        /// <summary>
        /// Maximum number of attempts to obtain lock
        /// </summary>
        public static sbyte MaximumAttempts
        {
            get
            {
                return (_maxAttempts);
            }

            set
            {
                _maxAttempts = value;
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="o">lock object</param>
        /// <returns>TimedLock instance</returns>
        public static TimedLock Lock (object o)
        {
            return Lock (o, TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="o">lock object</param>
        /// <param name="timeout">timeout in seconds</param>
        /// <returns>TimedLock instance</returns>
        public static TimedLock Lock (object o, TimeSpan timeout)
        {
            TimedLock tl = new TimedLock (o);
            sbyte attempt = 0;

            while (!Monitor.TryEnter (o, timeout))
            {
                attempt++;

                if (attempt <= _maxAttempts)
                    continue;

                throw new LockTimeoutException();
            }

            return (tl);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="o"></param>
        private TimedLock (object o)
        {
            target = o;
        }

        private object target;

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose ()
        {
#if DEBUG
        GC.SuppressFinalize(this);
#endif
            Monitor.Exit(target);
        }
    }

    /// <summary>
    /// Lock Timeout exception class
    /// </summary>
    public class LockTimeoutException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LockTimeoutException () 
            : base("Timeout waiting for lock")
        {
        }
    }
}

