/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter
 *
 *  Purpose:  FTP Line Parser
 *
 */
using System;

namespace Shared.Classes
{
    /// <summary>
    /// FTP Entry
    /// </summary>
    public class FTPEntry
    {
        #region Public Enums

        /// <summary>
        /// Entry Type
        /// </summary>
        public enum EntryType
        {
            /// <summary>
            /// Unknown Entry Type
            /// </summary>
            Unknown,

            /// <summary>
            /// Type is a file
            /// </summary>
            File,

            /// <summary>
            /// Type is a folder
            /// </summary>
            Folder
        }

        #endregion Public Enums

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        public FTPEntry(EntryType type, string name)
        {
            Type = type;
            Name = name;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public FTPEntry(string name)
            : this(EntryType.Folder, name)
        {
            Size = 0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        public FTPEntry(string name, long size)
            : this(EntryType.File, name)
        {
            Size = size;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Type of entry
        /// </summary>
        public EntryType Type { get; private set; }

        /// <summary>
        /// Name (File or folder name)
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// File Size
        /// </summary>
        public long Size { get; private set; }

        #endregion Properties

        #region Static Methods

        /// <summary>
        /// Parses an FTP line and returns an entry, if vald
        /// 
        /// Only tested on FileZilla FTP Server
        /// </summary>
        /// <param name="line">Line to be parsed</param>
        /// <param name="entry">Entry to be returned if valid</param>
        /// <returns>true if successful, otherwise false</returns>
        public static bool ParseFTPLine(string line, ref FTPEntry entry)
        {
            entry = null;
            EntryType type = EntryType.Unknown;

            if (line.StartsWith("-"))
                type = EntryType.File;
            else if (line.StartsWith("d"))
                type = EntryType.Folder;

            if (type == EntryType.Unknown)
                return (false);

            string[] parts = line.Split(' ');
            string name = parts[parts.Length - 1];
            int currIndex = 7;

            while (String.IsNullOrEmpty(parts[currIndex]))
                currIndex++;

            long size = Convert.ToInt64(parts[currIndex]);

            if (type == EntryType.File)
                entry = new FTPEntry(name, size);
            else
                entry = new FTPEntry(name);

            return (true);
        }

        #endregion Static Methods
    }
}
