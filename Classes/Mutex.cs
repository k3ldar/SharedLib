/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Microsoft
 *  https://msdn.microsoft.com/en-us/library/system.threading.mutex(v=vs.110).aspx
 *
 *  Copyright (c) Microsoft
 *
 *  Purpose:  Extende Mutex class
 *
 */
using System;
using System.Security.AccessControl;
using System.Threading;

namespace Shared.Classes
{
    /// <summary>
    /// MutexEx control
    /// </summary>
    public sealed class MutexEx : IDisposable
    {
        #region Private Members

        private Mutex _mutex = null;

        private bool _initialOwner = true;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public MutexEx()
            : this("Mutex")
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public MutexEx(string name)
        {
            _mutex = null;
            Name = name;
            MutexCreated = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="initialOwner"></param>
        public MutexEx(string name, bool initialOwner)
            : this (name)
        {
            _initialOwner = initialOwner;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates the Mutex
        /// </summary>
        public void CreateMutex()
        {
            bool doesNotExist = false;
            bool unauthorized = false;
            bool mutexWasCreated = false;

            // Attempt to open the named mutex.
            try
            {
                // Open the mutex with (MutexRights.Synchronize |
                // MutexRights.Modify), to enter and release the
                // named mutex.
                //
                Mutex m = Mutex.OpenExisting(Name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                doesNotExist = true;
            }
            catch (UnauthorizedAccessException)
            {
                unauthorized = true;
            }

            // There are three cases: (1) The mutex does not exist.
            // (2) The mutex exists, but the current user doesn't 
            // have access. (3) The mutex exists and the user has
            // access.
            //
            if (doesNotExist)
            {
                // The mutex does not exist, so create it.

                // Create an access control list (ACL) that denies the
                // current user the right to enter or release the 
                // mutex, but allows the right to read and change
                // security information for the mutex.
                //
                string user = Environment.UserDomainName + "\\"
                    + Environment.UserName;
                MutexSecurity mSec = new MutexSecurity();

                MutexAccessRule rule = new MutexAccessRule(user,
                    MutexRights.Synchronize | MutexRights.Modify,
                    AccessControlType.Deny);
                mSec.AddAccessRule(rule);

                rule = new MutexAccessRule(user,
                    MutexRights.ReadPermissions | MutexRights.ChangePermissions,
                    AccessControlType.Allow);
                mSec.AddAccessRule(rule);

                // Create a Mutex object that represents the system
                // mutex named by the constant 'mutexName', with
                // initial ownership for this thread, and with the
                // specified security access. The Boolean value that 
                // indicates creation of the underlying system object
                // is placed in mutexWasCreated.
                //
                _mutex = new Mutex(_initialOwner, Name, out mutexWasCreated, mSec);

                // If the named system mutex was created, it can be
                // used by the current instance of this program, even 
                // though the current user is denied access. The current
                // program owns the mutex. Otherwise, exit the program.
                // 
                if (!mutexWasCreated)
                {
#if TRACE
                    Console.WriteLine("Unable to create the mutex.");
#endif
                    return;
                }
            }
            else if (unauthorized)
            {
                // Open the mutex to read and change the access control
                // security. The access control security defined above
                // allows the current user to do this.
                //
                try
                {
                    _mutex = Mutex.OpenExisting(Name,
                        MutexRights.ReadPermissions | MutexRights.ChangePermissions);

                    // Get the current ACL. This requires 
                    // MutexRights.ReadPermissions.
                    MutexSecurity mSec = _mutex.GetAccessControl();

                    string user = Environment.UserDomainName + "\\"
                        + Environment.UserName;

                    // First, the rule that denied the current user 
                    // the right to enter and release the mutex must
                    // be removed.
                    MutexAccessRule rule = new MutexAccessRule(user,
                         MutexRights.Synchronize | MutexRights.Modify,
                         AccessControlType.Deny);
                    mSec.RemoveAccessRule(rule);

                    // Now grant the user the correct rights.
                    // 
                    rule = new MutexAccessRule(user,
                        MutexRights.Synchronize | MutexRights.Modify,
                        AccessControlType.Allow);
                    mSec.AddAccessRule(rule);

                    // Update the ACL. This requires
                    // MutexRights.ChangePermissions.
                    _mutex.SetAccessControl(mSec);

                    Console.WriteLine("Updated mutex security.");

                    // Open the mutex with (MutexRights.Synchronize 
                    // | MutexRights.Modify), the rights required to
                    // enter and release the mutex.
                    //
                    _mutex = Mutex.OpenExisting(Name);

                }
#if TRACE
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine("Unable to change permissions: {0}",  ex.Message);
                }
#else
                catch
#endif
                {
                    return;
                }
            }

            MutexCreated = mutexWasCreated;

            if (!mutexWasCreated)
                _mutex.WaitOne();
        }

        #endregion Public Methods

        #region Private Methods

        private bool MutexExists()
        {
            try
            {
                // Open the mutex with (MutexRights.Synchronize |
                // MutexRights.Modify), to enter and release the
                // named mutex.
                //
                _mutex = Mutex.OpenExisting(Name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return (false);
            }
            catch (UnauthorizedAccessException)
            {
                return (true);
            }

            return (false);
        }

        #endregion Private Methods

        #region Properties

        /// <summary>
        /// Name of the Mutex
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Indicates wether it was created (true) or previously created (false)
        /// </summary>
        public bool MutexCreated { get; private set; }

        /// <summary>
        /// Determines wether the mutex already exists or not
        /// </summary>
        public bool Exists
        {
            get
            {
                return (MutexExists());
            }
        }

        #endregion Properties

        #region Disposable

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            if (_mutex == null)
                return;

            if (!MutexCreated)
                _mutex.ReleaseMutex();

            _mutex.Dispose();
            _mutex = null;
        }

        #endregion Disposable
    }
}
