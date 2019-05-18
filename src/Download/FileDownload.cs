/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  Static File Download class
 *
 */
using System;
using System.ComponentModel;

using Shared.Classes;

#pragma warning disable IDE1006 // naming rule violation

namespace Shared
{
    /// <summary>
    /// Class for downloading files from internet
    /// </summary>
    public static class FileDownload
    {
        private static bool _downloading = false;
        private static object _lockObject = new object();

        /// <summary>
        /// Indicates wether a download is in progress or not
        /// </summary>
        public static bool Downloading
        {
            get
            {
                return (_downloading);
            }
        }

        /// <summary>
        /// Initiates a file download
        /// </summary>
        /// <param name="SourceFile">Remote File from internet</param>
        /// <param name="DestinationFile">Local path/file name for downloaded file</param>
        /// <param name="sleepTime">Number of milliseconds to sleep whilst waiting for download</param>
        /// <param name="iterations">Number of sleep iterations to wait for file download</param>
        public static void Download(string SourceFile, string DestinationFile, int sleepTime = 100, int iterations = 60)
        {
            try
            {
                using (TimedLock.Lock(_lockObject))
                {
                    _downloading = true;
                    WebClientEx client = new WebClientEx();
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);

                    Uri uri = new Uri(SourceFile);
                    client.DownloadFileAsync(uri, DestinationFile);

                    int i = 1;

                    while (_downloading && i < iterations)
                    {
                        System.Threading.Thread.Sleep(sleepTime);
                        i++;

                        if (i >= iterations)
                            throw new TimeoutException();
                    }

                    System.Threading.Thread.Sleep(400);
                }
            }
            catch (Exception err)
            {
                _downloading = false;
                EventLog.Add(err, SourceFile + "\r" + DestinationFile);
            }
        }

        private static void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _downloading = false;
        }

    }
}
