/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  FileBackup, class to monitor changes to folders
 *  
 *  WORK IN PROGRESS - DO NOT USE
 *
 */
using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable IDE1006 // naming rule violation

namespace Shared.Classes
{
    /// <summary>
    /// Backup File Class
    /// </summary>
    public class FileBackup : ThreadManager, IDisposable
    {
        #region Private Members

        private List<string> _hookedFolders;

        private static List<FileSystemWatcher> _watchedFolders = new List<FileSystemWatcher>();

        private object _fileLockObject = new object();

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="folders">List of folders to watch/monitor</param>
        public FileBackup(List<string> folders)
            :base(null, new TimeSpan(0, 0, 1))
        {
            this.HangTimeout = 10;

            _hookedFolders = folders;

            HookFolders();
        }

        #endregion Constructors

        #region Overridden Methods

        /// <summary>
        /// Overridden Run Method
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>true if to continue, otherwise false to terminate the thread</returns>
        protected override bool Run(object parameters)
        {
            return (!HasCancelled());
        }

        #endregion Overridden Methods

        #region Public Methods


        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
#if DEBUG
            System.GC.SuppressFinalize(this);
#endif
            UnHookFolders();
        }

        #endregion Public Methods

        #region Private Methods

        private void HookFolders()
        {
            using (TimedLock.Lock (_fileLockObject))
            { 
                foreach (string path in _hookedFolders)
                {
                    FileSystemWatcher watcher = new System.IO.FileSystemWatcher(path);
                    watcher.Changed += watcher_Changed;
                    watcher.Deleted += watcher_Deleted;
                    watcher.Renamed += watcher_Renamed;
                    watcher.Created += watcher_Created;
                    watcher.EnableRaisingEvents = true;
                    watcher.IncludeSubdirectories = true;
                    _watchedFolders.Add(watcher);
                }
            }
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {
            
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            
        }

        private void watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
 	        
        }   


        private void UnHookFolders()
        {

            _hookedFolders = null;
        }

        #endregion Private Methods
    }
}
