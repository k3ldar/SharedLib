/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Static Zip File Wrapper class
 *
 */
using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

#pragma warning disable IDE1005 // Delegate invocation can be simplified
#pragma warning disable IDE1006 // naming rule violation
#pragma warning disable IDE0017 // initialization can be simplified

namespace Shared
{
    /// <summary>
    /// Static class for manipulating zip files
    /// </summary>
    public static class ZipFiles
    {
        #region Public Static Methods

        /// <summary>
        /// Creates a zip file and adds all folders/sub folders into the zip file.
        /// </summary>
        /// <param name="zipFile">Zip File to Create</param>
        /// <param name="folderToZip">Folder to be zipped</param>
        public static void ZipFolder(string zipFile, string folderToZip)
        {
            ZipOutputStream zip = new ZipOutputStream(File.Create(zipFile));
            try
            {
                zip.SetLevel(9);
                string folder = folderToZip;
                zipFolder(folder, folder, zip, zipFile);
                zip.Finish();
            }
            finally
            {
                zip.Close();
                zip.Dispose();
                zip = null;
            }
        }

        /// <summary>
        /// Unzips a zip file to a specific folder
        /// </summary>
        /// <param name="fileName">zip file to be unzipped</param>
        /// <param name="unpackFolder">folder where zip file is to be unzipped</param>
        public static void UnzipToFolder(string fileName, string unpackFolder)
        {
            FastZip zip = new FastZip();
            zip.ExtractZip(fileName, unpackFolder, "");
        }

        /// <summary>
        /// Unpacks a zip file
        /// </summary>
        /// <param name="ZipFile"></param>
        /// <param name="ZipExtractPath"></param>
        public static void Unpack(string ZipFile, string ZipExtractPath)
        {
            try
            {
                if (!File.Exists(ZipFile))
                {
                    return;
                }

                ZipInputStream s = new ZipInputStream(File.OpenRead(ZipFile));
                try
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string directoryName = Path.GetDirectoryName(theEntry.Name);
                        string fileName = Path.GetFileName(theEntry.Name);

                        // create directory
                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(ZipExtractPath + directoryName);
                        }

                        if (fileName != String.Empty)
                        {
                            string destination = ZipExtractPath + theEntry.Name;

                            FileStream streamWriter = File.Create(destination);
                            try
                            {

                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                streamWriter.Flush();
                            }
                            finally
                            {
                                streamWriter.Close();
                                streamWriter.Dispose();
                                streamWriter = null;
                            }
                        }
                    }
                }
                finally
                {
                    s.Close();
                    s.Dispose();
                    s = null;
                }
            }
            finally
            {
                //Update Status
            }
        }

        /// <summary>
        /// Creates a zip file and add's a single file
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="fileToZip"></param>
        /// <param name="deleteOriginal"></param>
        public static void CompressFile(string zipFile, string fileToZip, bool deleteOriginal = true)
        {
            ZipOutputStream zip = new ZipOutputStream(File.Create(zipFile));
            try
            {
                zip.SetLevel(9);
                addFileToZip(zip, String.Empty, fileToZip);
                zip.Finish();
            }
            finally
            {
                zip.Close();
                zip.Dispose();
                zip = null;
            }

            if (deleteOriginal)
            {
                //delete backup file
                File.Delete(fileToZip);
            }
        }

        /// <summary>
        /// Compresses a file and returns the contents to base 64 encoded
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="fileToCompress"></param>
        /// <returns></returns>
        public static string CompressFileBase64(byte[] fileContents, string fileToCompress)
        {
            byte[] zippedContents = CompressFileContents(fileContents, fileToCompress);
            return (Convert.ToBase64String(zippedContents));
        }

        #endregion Public Static Methods

        #region Private Static Methods

        private static byte[] CompressFileContents(byte[] fileContents, string fileName)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                ZipOutputStream zs = new ZipOutputStream(ms);
                try
                {
                    zs.SetLevel(9); //0-9, 9 being the highest level of compression

                    ZipEntry newEntry = new ZipEntry(Path.GetFileName(fileName)); // Call it what you will here
                    newEntry.DateTime = DateTime.Now;
                    zs.UseZip64 = UseZip64.Off;
                    zs.PutNextEntry(newEntry);
                    zs.Write(fileContents, 0, fileContents.Length);
                    zs.CloseEntry();
                    zs.IsStreamOwner = false;    // True makes the Close also Close the underlying stream
                }
                finally
                {
                    zs.Close();
                    zs.Dispose();
                    zs = null;
                }

                return (ms.ToArray());
            }
            finally
            {
                ms.Close();
                ms.Dispose();
                ms = null;
            }
        }

        private static void zipFolder(string RootFolder, string CurrentFolder, ZipOutputStream zStream, string zipFile)
        {

            string[] SubFolders = Directory.GetDirectories(CurrentFolder);
            foreach (string Folder in SubFolders)
                zipFolder(RootFolder, Folder, zStream, zipFile);

            string relativePath = CurrentFolder.Substring(RootFolder.Length) + "/";

            if (relativePath.Length > 1)
            {
                ZipEntry dirEntry;
                dirEntry = new ZipEntry(relativePath);
                dirEntry.DateTime = DateTime.Now;
            }

            foreach (string file in Directory.GetFiles(CurrentFolder))
            {
                RaiseFileAddedToZip(file, zipFile);
                addFileToZip(zStream, relativePath, file);
            }
        }

        private static void addFileToZip(ZipOutputStream zStream, string relativePath, string file)
        {
            byte[] buffer = new byte[4096];
            string fileRelativePath = (relativePath.Length > 1 ? relativePath : string.Empty) + Path.GetFileName(file);
            ZipEntry entry = new ZipEntry(fileRelativePath);
            entry.DateTime = DateTime.Now;
            zStream.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(file))
            {
                int sourceBytes;
                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                    zStream.Write(buffer, 0, sourceBytes);

                } while (sourceBytes > 0);
            }
        }

        private static void RaiseFileAddedToZip(string fileName, string archive)
        {
            if (ZipFileAddedToArchive != null)
                ZipFileAddedToArchive(null, new ZipFileAddEventArgs(fileName, archive));
        }

        #endregion Private Static Methods

        /// <summary>
        /// Event raised when a file is added to the archive
        /// </summary>
        public static event ZipFileAddEventHandler ZipFileAddedToArchive;
    }

    /// <summary>
    /// File add event arguments
    /// </summary>
    public class ZipFileAddEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="archive"></param>
        public ZipFileAddEventArgs(string fileName, string archive)
        {
            FileName = fileName;
            Archive = archive;
        }

        /// <summary>
        /// Name of file to add to archive
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Zip archive file is to be added to
        /// </summary>
        public string Archive { get; private set; }
    }

    /// <summary>
    /// Delegate for zip file add
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ZipFileAddEventHandler(object sender, ZipFileAddEventArgs e);
}
