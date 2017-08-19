/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2010 Simon Carter
 *
 *  Purpose:  Static Class of various functions, a complete hodgebodge collection home grown 
 *  or collected randomly from the internet
 *
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Shared
{
    /// <summary>
    /// Utilities class
    /// </summary>
    public static class Utilities
    {
        #region Windows System Users

        /// <summary>
        /// Get's account names for local users
        /// </summary>
        /// <returns>List</returns>
        public static List<string> GetLocalWindowsAccounts()
        {
            List<string> Result = new List<string>();

            ManagementObjectSearcher usersSearcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_UserAccount");
            ManagementObjectCollection users = usersSearcher.Get();

            foreach (ManagementObject user in users)
            {
                if (String.IsNullOrEmpty(user["FullName"].ToString()))
                    continue;

                Result.Add(user["FullName"].ToString());
            }

            return (Result);
        }

        #endregion Windows System Users

        #region IP Address

        /// <summary>
        /// Converts a Long to an IP Address
        /// </summary>
        /// <param name="longIP">long value</param>
        /// <returns>string - IP Address</returns>
        public static string LongToIP(long longIP)
        {
            string Result = string.Empty;

            for (int i = 0; i < 4; i++)
            {
                int num = (int)(longIP / Math.Pow(256, (3 - i)));
                longIP = longIP - (long)(num * Math.Pow(256, (3 - i)));

                if (i == 0)
                    Result = num.ToString();
                else
                    Result = Result + "." + num.ToString();
            }

            return (Result);
        }

        /// <summary>
        /// Converts an IP Address to a long
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <returns>Long value representing the ip address</returns>
        public static long IPToLong(string ip)
        {
            string[] ipBytes;
            double Result = 0;

            if (!string.IsNullOrEmpty(ip))
            {
                ipBytes = ip.Split('.');

                for (int i = ipBytes.Length - 1; i >= 0; i--)
                {
                    Result += ((int.Parse(ipBytes[i]) % 256) * Math.Pow(256, (3 - i)));
                }
            }

            return ((long)Result);
        }

        /// <summary>
        /// Returns a list of all local network addresses
        /// </summary>
        /// <returns></returns>
        public static string[] LocalIPAddresses()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString() + ";";

                }
            }

            if (localIP.EndsWith(";"))
                localIP = localIP.Substring(0, localIP.Length - 1);

            return (localIP.Split(';'));
        }

        /// <summary>
        /// Determines wether an IP address is a local ip address
        /// </summary>
        /// <returns></returns>
        public static bool LocalIPAddress(string ipAddress)
        {
            if (ipAddress == "127.0.0.1" || ipAddress == "0.0.0.0")
                return (true);

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    ip.ToString() == ipAddress)
                {
                    return (true);
                }
            }

            return (false);
        }

        #endregion IP Address

        #region Other

        private const string AVAILABLE_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWQYZ0123456789abcdefghijklmnopqrstuvwxyz";
        private static Random _random;

        /// <summary>
        /// Creates a random string
        /// </summary>
        /// <param name="length">Length of string to create</param>
        /// <returns>Random string containing A-Z, a-z, 0-9</returns>
        internal static string RandomString(int length)
        {
            string Result = String.Empty;

            if (_random == null)
                _random = new Random(DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Day);

            while (Result.Length < length)
            {
                Result += AVAILABLE_CHARACTERS.Substring(_random.Next(0, 62), 1);
            }

            return (Result);
        }

        /// <summary>
        /// Creates a random number
        /// </summary>
        /// <param name="min">Minimum Value for random number</param>
        /// <param name="max">Maximum value for random number</param>
        /// <returns>Random number between min and max</returns>
        internal static int RandomNumber(int min, int max)
        {
            if (_random == null)
                _random = new Random(DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Day);

            return (_random.Next(min, max));
        }

        /// <summary>
        /// Generates a random word
        /// </summary>
        /// <param name="length"></param>
        /// <param name="AcceptableChars"></param>
        /// <returns></returns>
        public static string GetRandomWord(int length, string AcceptableChars = 
            "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.-")
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            string Result = "";

            for (int i = 0; i < length; i++)
            {
                int ch = rnd.Next(AcceptableChars.Length - 1);
                Result += AcceptableChars.Substring(ch, 1);
            }

            return Result;
        }

        /// <summary>
        /// Returns the current path
        /// </summary>
        /// <param name="addTrailingBackSlash">Adds a trailing backslash</param>
        /// <returns>Path for current executable</returns>
        public static string CurrentPath(bool addTrailingBackSlash = false)
        {
            string Result = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            Result = Path.GetDirectoryName(Result);

            if (addTrailingBackSlash)
                Result = AddTrailingBackSlash(Result);

            return (Result.Substring(6));
        }

        /// <summary>
        /// Takes a connection string and returns a parameter value
        /// </summary>
        /// <param name="connectionString">Connection String</param>
        /// <param name="name">Name of parameter</param>
        /// <returns>Value of parameter, if found, otherwise empty string</returns>
        public static string ConnectionStringValue(string connectionString, string name)
        {
            try
            {
                string[] parts = connectionString.Split(';');

                foreach (string s in parts)
                {
                    string[] values = s.Split('=');

                    if (values[0].ToLower() == name.ToLower())
                    {
                        return (values[1]);
                    }
                }
            }
            catch
            {
                // ignore, just returns an empty string
            }

            return (String.Empty);
        }

        #endregion Other

        #region Processes

        /// <summary>
        /// Kills all processes with filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool ProcessKill(string fileName)
        {
            Process[] localByName = Process.GetProcessesByName(fileName);

            foreach (Process process in localByName)
            {
                try
                {
                    process.Kill();
                    return (true);
                }
                catch
                {
                    // nothing to do, it didn't work :-(
                }
            }

            return (false);
        }

        #endregion Processes

        #region Format

        /// <summary>
        /// Gets the currency symbol for a specific culture
        /// </summary>
        /// <param name="Culture">Culture who's currency symbol is required</param>
        /// <returns>The currency symbol for the string</returns>
        public static string GetCurrencySymbol(string Culture)
        {
            string isos;
            return (GetCurrencySymbol(Culture, out isos));
        }

        /// <summary>
        /// Gets the currency symbol for a specific culture
        /// </summary>
        /// <param name="Culture">Culture who's currency symbol is required</param>
        /// <param name="ISOSymbol">ISO Symbol</param>
        /// <returns>The currency symbol for the string</returns>
        public static string GetCurrencySymbol(string Culture, out string ISOSymbol)
        {
            CultureInfo info = CultureInfo.CreateSpecificCulture(Culture);

            return (GetCurrencySymbol(info, out ISOSymbol));
        }

        /// <summary>
        /// Gets the currency symbol for a specific culture
        /// </summary>
        /// <param name="culture">Culture who's currency symbol is required</param>
        /// <returns>The currency symbol for the string</returns>
        public static string GetCurrencySymbol(CultureInfo culture)
        {
            string isos;
            return (GetCurrencySymbol(culture, out isos));
        }

        /// <summary>
        /// Gets the currency symbol for a specific culture
        /// </summary>
        /// <param name="Culture">Culture who's currency symbol is required</param>
        /// <param name="ISOSymbol">ISO Symbol</param>
        /// <returns>The currency symbol for the string</returns>
        public static string GetCurrencySymbol(CultureInfo Culture, out string ISOSymbol)
        {
            RegionInfo region = new RegionInfo(Culture.LCID);
            ISOSymbol = region.ISOCurrencySymbol;
            return (region.CurrencySymbol);
        }


        #endregion Format

        #region Text Manipulation

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FirstCharLower(string str)
        {
            if (String.IsNullOrEmpty(str) || Char.IsLower(str, 0))
                return str;

            return Char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Ensures a string does not exceed a maximum length, if it does, cut's it to maximum length required
        /// 
        /// if value is null, returns String.Empty
        /// </summary>
        /// <param name="value">string to check</param>
        /// <param name="maxLength">Maximum Length</param>
        /// <returns>value, no longer than maxLength</returns>
        public static string TextMaxLength(string value, uint maxLength)
        {
            if (String.IsNullOrEmpty(value))
                return (String.Empty);

            if (value.Length > maxLength)
                return (value.Substring(0, (int)maxLength));
            else
                return (value);
        }

        /// <summary>
        /// Measures text
        /// 
        /// Given the font used, will return how wide/how the text will be when drawn
        /// </summary>
        /// <param name="text">Text to measure</param>
        /// <param name="font">Font used in measurements</param>
        /// <returns>Size of text</returns>
        public static Size MeasureText(string text, Font font)
        {
            return (TextRenderer.MeasureText(text, font));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="maxCharacters"></param>
        /// <returns></returns>
        public static Size MeasureText(string text, Font font, int maxCharacters)
        {
            text = WordWrap(text, maxCharacters);

            return (TextRenderer.MeasureText(text, font));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="maxCharacters"></param>
        /// <returns></returns>
        public static string WordWrap(string input, int maxCharacters)
        {
            List<string> lines = new List<string>();

            if (!input.Contains(" ") && !input.Contains("\n"))
            {
                int start = 0;
                while (start < input.Length)
                {
                    lines.Add(input.Substring(start, Math.Min(maxCharacters, input.Length - start)));
                    start += maxCharacters;
                }
            }
            else
            {
                string[] paragraphs = input.Split('\n');

                foreach (string paragraph in paragraphs)
                {
                    string[] words = paragraph.Split(' ');

                    string line = "";
                    foreach (string word in words)
                    {
                        if ((line + word).Length > maxCharacters)
                        {
                            lines.Add(line.Trim());
                            line = "";
                        }

                        line += string.Format("{0} ", word);
                    }

                    if (line.Length > 0)
                    {
                        lines.Add(line.Trim());
                    }
                }
            }

            string Result = String.Empty;

            foreach (string s in lines)
                Result += "\r\n" + s;

            return Result.Substring(2);
        }

        /// <summary>
        /// Removes double quotes surrounding a string, if they exist
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveDblQuotes(string s)
        {
            string Result = s;

            if (Result.StartsWith("\"") && Result.EndsWith("\""))
                Result = Result.Substring(1, Result.Length - 2);

            return (Result);
        }

        /// <summary>
        /// Splits a string into words using uppercase char as option to split
        /// </summary>
        /// <param name="s">String to split</param>
        /// <returns>string</returns>
        public static string SplitCamelCase(string s)
        {
            Regex r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            return (r.Replace(s, " "));
        }

        #endregion Text Manipulation

        #region Service Methods

        /// <summary>
        /// Retrieves the version information for a service application
        /// </summary>
        /// <param name="ServiceName">Name of service</param>
        /// <returns>File Version for service</returns>
        public static string ServiceFileVersion(string ServiceName)
        {
            string query = string.Format("SELECT PathName FROM Win32_Service WHERE DisplayName = '{0}'", ServiceName);

            using (ManagementObjectSearcher search = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject service in search.Get())
                {
                    try
                    {
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(service["PathName"].ToString().Trim('"'));

                        return (fvi.FileVersion);
                    }
                    catch (Exception err)
                    {
                        // not possible permissions etc, may need to run as administrator
                        EventLog.Add(err);
                        throw;
                    }
                }
            }

            return (String.Empty);
        }

        /// <summary>
        /// Retrieves the Path for a service application
        /// </summary>
        /// <param name="ServiceName">Name of service</param>
        /// <returns>Installation Path for service</returns>
        public static string ServiceFilePath(string ServiceName)
        {
            string query = string.Format("SELECT PathName FROM Win32_Service WHERE DisplayName = '{0}'", ServiceName);

            using (ManagementObjectSearcher search = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject service in search.Get())
                {
                    try
                    {
                        return (Path.GetDirectoryName(service["PathName"].ToString().Trim('"')));
                    }
                    catch (Exception err)
                    {
                        // not possible permissions etc, may need to run as administrator
                        EventLog.Add(err);
                        throw;
                    }
                }
            }

            return (String.Empty);
        }

        /// <summary>
        /// Starts a service
        /// </summary>
        /// <param name="ServiceName">Name of service to stop</param>
        public static void ServiceStart(string ServiceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    try
                    {
                        EventLog.Add("Initialise", String.Format("Starting Service: {0}", ServiceName));
                        service.Start();
                    }
                    catch (Exception err)
                    {
                        // not possible permissions etc, may need to run as administrator
                        EventLog.Add(err);
                        throw;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Stops a windows service
        /// </summary>
        /// <param name="ServiceName">Name of service to stop</param>
        public static void ServiceStop(string ServiceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    try
                    {
                        EventLog.Add("Finalise", String.Format("Stopping Service: {0}", ServiceName));
                        service.Stop();
                    }
                    catch (Exception err)
                    {
                        //not possible permissions etc may need to run as administrator
                        EventLog.Add(err);
                        throw;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Determines wether a service is installed
        /// </summary>
        /// <param name="ServiceName">Name of Service</param>
        /// <returns>true if service installed, otherwise false</returns>
        public static bool ServiceInstalled(string ServiceName)
        {
            bool Result = false;

            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    Result = true;
                    break;
                }
            }

            return (Result);
        }

        /// <summary>
        /// Determines wether a service can be stopped
        /// </summary>
        /// <param name="ServiceName">Name of Service</param>
        /// <returns>true if service can be stopped, otherwise false</returns>
        public static bool ServiceCanBeStopped(string ServiceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    return (service.Status == ServiceControllerStatus.Running);
                }
            }

            return (false);
        }

        /// <summary>
        /// Determines wether a service can be started
        /// </summary>
        /// <param name="ServiceName">Name of service</param>
        /// <returns>true if the service can be started</returns>
        public static bool ServiceCanBeStarted(string ServiceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    switch (service.Status)
                    {
                        case ServiceControllerStatus.StartPending:
                        case ServiceControllerStatus.ContinuePending:
                        case ServiceControllerStatus.PausePending:
                        case ServiceControllerStatus.StopPending:
                        case ServiceControllerStatus.Running:
                            return (false);

                        default:
                            return (true);
                    }
                }
            }

            return (false);
        }

        /// <summary>
        /// Determines whether a service is running
        /// </summary>
        /// <param name="ServiceName">Name of Service</param>
        /// <returns>true if running, otherwise false</returns>
        public static bool ServiceRunning(string ServiceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == ServiceName)
                {
                    return (service.Status == ServiceControllerStatus.Running);
                }
            }

            return (false);
        }

        /// <summary>
        /// Restarts a windows service
        /// </summary>
        /// <param name="ServiceName">Name of service to restart</param>
        /// <param name="sleepDelay">Delay in ms to sleep whilst waiting for a response</param>
        /// <param name="iteratorLoops">Maximum number of iterations whilst waiting for a response</param>
        /// <returns>true if service service is restarted, otherwise false</returns>
        public static bool ServiceRestart(string ServiceName, int sleepDelay = 50, int iteratorLoops = 20)
        {
            int iterator = 0;
            bool serviceInstalled = Utilities.ServiceInstalled(ServiceName);

            if (serviceInstalled && Utilities.ServiceCanBeStopped(ServiceName) && Utilities.ServiceRunning(ServiceName))
                Utilities.ServiceStop(ServiceName);

            while (serviceInstalled && !Utilities.ServiceCanBeStarted(ServiceName) && iterator < iteratorLoops)
            {
                System.Threading.Thread.Sleep(sleepDelay);
                iterator++;
            }

            if (serviceInstalled && Utilities.ServiceCanBeStarted(ServiceName) && !Utilities.ServiceRunning(ServiceName))
                Utilities.ServiceStart(ServiceName);

            iterator = 0;

            while (serviceInstalled && !Utilities.ServiceRunning(ServiceName) && iterator < iteratorLoops)
            {
                System.Threading.Thread.Sleep(sleepDelay);
                iterator++;
            }

            return (ServiceRunning(ServiceName));
        }

        #endregion Service Methods

        #region Conversion

        /// <summary>
        /// Converts a double value to a time value
        /// </summary>
        /// <param name="date"></param>
        /// <param name="Time"></param>
        /// <returns></returns>
        public static DateTime DoubleToDate(DateTime date, double Time)
        {
            DateTime Result = date;
            Result = Result.AddHours(-date.Hour);
            Result = Result.AddMinutes(-date.Minute);

            if (Time < 0) Time = 0; // less than zero then 0
            if (Time > 24) Time = 23.75;

            int h = (int)Time;
            int m = (int)(((Time - Math.Floor(Time)) * 100) / 1.666666666666667);

            if (m > 0)
                m++;

            Result = Result.AddHours(h);
            Result = Result.AddMinutes(m);


            return (Result);
        }

        /// <summary>
        /// Converts a double value to a time value
        /// </summary>
        /// <param name="date"></param>
        /// <param name="Time"></param>
        /// <param name="Duration"></param>
        /// <returns></returns>
        public static DateTime DoubleToDate(DateTime date, double Time, int Duration)
        {
            DateTime Result = date;

            if (Time < 0) Time = 0; // less than zero then 0
            if (Time > 24) Time = 23.75;

            int h = (int)Time;
            int m = (int)(((Time - Math.Floor(Time)) * 100) / 1.666666666666667);

            if (m > 0)
                m++;

            Result = Result.AddHours(h);
            Result = Result.AddMinutes(m);

            int MinSlots15 = Duration / 15;

            for (int i = 0; i < MinSlots15; i++)
                Result = Result.AddMinutes(15);


            return (Result);
        }

        /// <summary>
        /// Converts the time element of a datetime object to a double value
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static double DateToDouble(DateTime date)
        {
            return (TimeToDouble(date.ToString("HH:MM")));
        }

        /// <summary>
        /// Converts the time to a double
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static double TimeToDouble(DateTime time)
        {
            return (TimeToDouble(time.ToString("t")));
        }

        /// <summary>
        /// Converts a string to a double 
        /// </summary>
        /// <param name="t">string to convert</param>
        /// <returns></returns>
        public static double TimeToDouble(string t)
        {
            string[] parts = t.Split(':');
            string s = "";

            switch (parts[1])
            {
                case "15":
                    s = "25";
                    break;
                case "30":
                    s = "50";
                    break;
                case "45":
                    s = "75";
                    break;
            }

            s = parts[0] + "." + s;

            return (Convert.ToDouble(s));
        }

        /// <summary>
        /// Converts a double into a time
        /// </summary>
        /// <param name="d">Double to convert</param>
        /// <returns>string</returns>
        public static string DoubleToTime(Double d)
        {
            //validation
            if (d < 0) d = 0; // less than zero then 0
            if (d > 24) d = 23.75;

            int h = (int)d;
            int m = (int)(((d - Math.Floor(d)) * 100) / 1.666666666666667);


            if (m > 0)
                m++;

            string Result = string.Format("{0}:{1}", h, m);

            if (m == 0)
                Result += "0";

            if (Result.Length == 4)
                Result = "0" + Result;

            return (Result);
        }

        /// <summary>
        /// Converts a string to an Int64
        /// </summary>
        /// <param name="value">String to be converted</param>
        /// <param name="defaultValue">Default value if conversion fails</param>
        /// <returns></returns>
        public static Int64 StrToInt64(string value, Int64 defaultValue)
        {
            Int64 Result = defaultValue;

            try
            {
                if (!Int64.TryParse(value, out Result))
                    Result = defaultValue;
            }
            catch
            {
                Result = defaultValue;
            }

            return (Result);
        }

        /// <summary>
        /// Converts a string to a number
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int StrToInt(string value, int defaultValue)
        {
            int Result = defaultValue;

            try
            {
                if (!int.TryParse(value, out Result))
                    Result = defaultValue;
            }
            catch
            {
                Result = defaultValue;
            }

            return (Result);
        }

        /// <summary>
        /// Attempts to convert a string to an int value
        /// </summary>
        /// <param name="Value">Value to convert</param>
        /// <param name="DefaultValue">Default value to be returned if conversion fails</param>
        /// <returns>int value</returns>
        public static int StrToIntDef(string Value, int DefaultValue)
        {
            try
            {
                if (Value == null || Value.Length == 0)
                    return (DefaultValue);

                return (Convert.ToInt32(Value));
            }
            catch
            {
                return DefaultValue;
            }
        }

        /// <summary>
        /// Converts a string to an Int64, provides a default value.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public static Int64 StrToInt64Def(string Value, Int64 DefaultValue)
        {
            try
            {
                if (Value == null || Value.Length == 0)
                    return (DefaultValue);

                return (Convert.ToInt64(Value));
            }
            catch
            {
                return (DefaultValue);
            }
        }

        /// <summary>
        /// Converts a string to a boolean value
        /// </summary>
        /// <param name="value">String value to convert</param>
        /// <param name="defaultValue">Default value if conversion fails</param>
        /// <returns>bool value</returns>
        public static bool StrToBool(string value, bool defaultValue)
        {
            bool Result = defaultValue;

            try
            {
                if (!bool.TryParse(value, out Result))
                    Result = defaultValue;
            }
            catch
            {
                Result = defaultValue;
            }

            return (Result);
        }

        /// <summary>
        /// Converts a string to a bool with default value of false
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool StrToBool(string Value)
        {
            return (Value.ToUpper().CompareTo("TRUE") == 0);
        }

        /// <summary>
        /// Converts a string to a date time using specified culture
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <param name="culture">Culture used in conversion</param>
        /// <returns></returns>
        public static DateTime StrToDateTime(string value, string culture = "en-GB")
        {
            return (StrToDateTime(value, new CultureInfo(culture)));
        }

        /// <summary>
        /// Converts a string to a date time using specified culture
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <param name="culture">Culture used in conversion</param>
        /// <returns></returns>
        public static DateTime StrToDateTime(string value, CultureInfo culture)
        {
            IFormatProvider formatProvider = culture;
            return (DateTime.Parse(value, formatProvider, DateTimeStyles.AssumeLocal));
        }

        /// <summary>
        /// Converts a date/time to a string using specified culture
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string DateTimeToStr(DateTime timeStamp, string culture)
        {
            IFormatProvider formatProvider = new CultureInfo(culture, true);
            return (timeStamp.ToString(formatProvider));
        }

        /// <summary>
        /// Converts a date/time to a string using specified culture
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string DateTimeToStr(DateTime timeStamp, CultureInfo culture)
        {
            IFormatProvider formatProvider = culture;
            return (timeStamp.ToString(formatProvider));
        }

        private const string NUMERIC_NUMBERS = "0123456789";

        /// <summary>
        /// Determines if a string value is a currency or not
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="val">retruned currency amount</param>
        /// <returns>true if currency otherwise false</returns>
        public static bool StrIsCurrency(string value, ref decimal val)
        {
            try
            {
                if (value.Length > 1)
                {
                    if (!NUMERIC_NUMBERS.Contains(value[0].ToString()))
                    {
                        value = value.Substring(1);
                    }

                    return (decimal.TryParse(value, out val));
                }
                else
                    return (false);
            }
            catch
            {
                return (false);
            }
        }

        /// <summary>
        /// Determines wether the string passed is a date or not
        /// </summary>
        /// <param name="value">string value used to check if it's a date</param>
        /// <param name="val">Date value returned</param>
        /// <returns>true if it is a date format, otherwise false</returns>
        public static bool StrIsDate(string value, ref DateTime val)
        {
            return (StrIsDate(value, ref val, Thread.CurrentThread.CurrentUICulture));
        }

        /// <summary>
        /// Determines wether the string passed is a date or not
        /// </summary>
        /// <param name="value">string value used to check if it's a date</param>
        /// <param name="val">Date value returned</param>
        /// <param name="culture">Culture to use</param>
        /// <returns>true if it is a date format, otherwise false</returns>
        public static bool StrIsDate(string value, ref DateTime val, CultureInfo culture)
        {
            IFormatProvider formatProvider = culture;
            return (DateTime.TryParse(value, formatProvider, DateTimeStyles.AssumeLocal, out val));
        }

        /// <summary>
        /// Looks at a string and determines wether it's a numeric value or not
        /// </summary>
        /// <param name="value">value to be checked</param>
        /// <param name="val">Value returned</param>
        /// <returns>true if it is numeric, otherwise false</returns>
        public static bool StrIsNumeric(string value, ref Int64 val)
        {
            try
            {
                return (Int64.TryParse(value, out val));
            }
            catch
            {
                return (false);
            }
        }

        /// <summary>
        /// Converts a boolean value to a string
        /// </summary>
        /// <param name="b">boolean value to convert</param>
        /// <returns>String representation of the bool, in proper case</returns>
        public static string BoolToStr(bool b)
        {
            if (b)
                return "True";
            else
                return "False";
        }

        /// <summary>
        /// Converts a string value to a decimal
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="defaultValue">Value to be returned if conversion fails</param>
        /// <param name="cultureInfo">Culture used for converting string, if null the current thread's UI culture is used</param>
        /// <returns>Decimal representation of value if convertable, otherwise defaultValue</returns>
        public static decimal StrToDecimal(string value, decimal defaultValue, CultureInfo cultureInfo = null)
        {
            try
            {
                if (cultureInfo == null)
                    cultureInfo = Thread.CurrentThread.CurrentUICulture;

                return (Convert.ToDecimal(value, cultureInfo));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="Value">Value to convert</param>
        /// <param name="DefaultValue">Value to be returned if conversion fails</param>
        /// <returns>double</returns>
        public static Double StrToDblDef(string Value, Double DefaultValue)
        {
            try
            {
                return (Convert.ToDouble(Value));
            }
            catch
            {
                return DefaultValue;
            }
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="Value">value to be converted</param>
        /// <returns>double</returns>
        public static Double StrToDbl(string Value)
        {
            return (Convert.ToDouble(Value));
        }

        /// <summary>
        /// Converts a string to an Int64
        /// </summary>
        /// <param name="Value">Value to convert</param>
        /// <param name="DefaultValue">Value to be returned if conversion fails</param>
        /// <returns>Int64</returns>
        public static Int64 StrToIntDef(string Value, Int64 DefaultValue)
        {
            try
            {
                if (Value == null || Value.Length == 0)
                    return (DefaultValue);

                return (Convert.ToInt64(Value));
            }
            catch
            {
                return DefaultValue;
            }
        }


        /// <summary>
        /// Converts a string to an uint
        /// </summary>
        /// <param name="Value">Value to convert</param>
        /// <param name="DefaultValue">Value to be returned if conversion fails</param>
        /// <returns>uint</returns>
        public static uint StrToUInt(string Value, uint DefaultValue)
        {
            uint Result = DefaultValue;
            try
            {
                if (Value == null || Value.Length == 0)
                    return (DefaultValue);

                if (!uint.TryParse(Value, out Result))
                    Result = DefaultValue;

                return (Result);
            }
            catch
            {
                return DefaultValue;
            }
        }

        /// <summary>
        /// Converts minutes into miliseconds
        /// </summary>
        /// <param name="Minutes">time in minutes to be converted</param>
        /// <param name="Default">Default time</param>
        /// <returns>Converted value in miliseconds</returns>
        public static string ConvertMinTomSecDef(string Minutes, string Default)
        {
            return (StrToDblDef(Minutes, StrToDblDef(Default, 0.0)) * 60 * 1000).ToString();
        }

        /// <summary>
        /// Converts minutes to Milliseconds
        /// </summary>
        /// <param name="Minutes">Minutes to convert</param>
        /// <param name="Default">Default value</param>
        /// <returns>Minutes converted to milliseconds</returns>
        public static int ConvertMinTomSecDef(int Minutes, int Default)
        {
            return (Minutes * 60 * 1000);
        }

        /// <summary>
        /// Attempts to convert a string to int
        /// </summary>
        /// <param name="number">string that is attempted to be converted</param>
        /// <returns>true if the string can be converted to an int, otherwise false</returns>
        public static bool TryParse(string number)
        {
            bool Result = false;

            try
            {
                Convert.ToInt32(number);
                Result = true;
            }
            catch
            {
                Result = false;
            }

            return (Result);
        }

        /// <summary>
        /// Converts miliseconds into minutes
        /// </summary>
        /// <param name="Miliseconds">time in miliseconds to be converted</param>
        /// <param name="Default">Default time</param>
        /// <returns>Converted value in minutes</returns>
        public static string ConvertmSecToMinDef(string Miliseconds, string Default)
        {
            return (StrToDblDef(Miliseconds, StrToDblDef(Default, 0.0)) / 60 / 1000).ToString();
        }

        /// <summary>
        /// Converts a string to a shortint (Int16)
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="DefaultValue"></param>
        /// <returns></returns>
        public static Int16 StrToInt(string Value, Int16 DefaultValue)
        {
            try
            {
                if (Value == null || Value.Length == 0)
                    return (DefaultValue);

                return (Convert.ToInt16(Value));
            }
            catch
            {
                return (DefaultValue);
            }
        }

        /// <summary>
        /// Attempts to format a phone number for UK
        /// </summary>
        /// <param name="Telephone">Telephone number to format</param>
        /// <returns>Formatted telephone number</returns>
        public static string FormatPhoneNumber(string Telephone)
        {
            string Result = Telephone;

            if (Result.StartsWith("+"))
                Result.Replace("+", "");

            if (Result.StartsWith("44"))
                Result = "0" + Result.Substring(2);

            if (Result.StartsWith("07")) //mobile
                return (Regex.Replace(Result, @"(\d{5})(\d{3})(\d{3})", "$1 $2$3"));
            else //landline
                return (Regex.Replace(Result, @"(\d{5})(\d{3})(\d{3})", "$1 $2 $3"));
        }

        /// <summary>
        /// Determines wether a string contains a correctly formatted email or not
        /// 
        /// This does not check wether the email address physically exists, only that
        /// it is correctly formatted, i.e. me@domain.com
        /// </summary>
        /// <param name="inputEmail">email address to check</param>
        /// <returns>true if correctly formatted email address, otherwise false</returns>
        public static bool IsValidEmail(string inputEmail)
        {
            if (inputEmail == null)
                inputEmail = string.Empty;

            string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);

            return (re.IsMatch(inputEmail));
        }

        /// <summary>
        /// Converts Kilometres to Miles
        /// </summary>
        /// <param name="KM"></param>
        /// <returns></returns>
        public static Double ConvertKMtoMiles(Double KM)
        {
            return (KM * 0.621371192);
        }

        /// <summary>
        /// Converts a string to proper case
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public static string ProperCase(string S)
        {
            try
            {
                CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentUICulture;
                TextInfo TextInfo = cultureInfo.TextInfo;
                return (TextInfo.ToTitleCase(S.ToLower()));
            }
            catch
            {
                return (S);
            }
        }

        /// <summary>
        /// Determines wether the postcode is a valid UK post code
        /// </summary>
        /// <param name="InPostCode"></param>
        /// <param name="PostCode"></param>
        /// <returns></returns>
        public static bool IsValidUKPostcode(string InPostCode, out string PostCode)
        {
            PostCode = InPostCode.ToUpper();
            PostCode = PostCode.Trim();

            // remove the space
            PostCode = PostCode.Replace(" ", "");

            switch (PostCode.Length)
            {
                case 5:
                    PostCode = PostCode.Substring(0, 2) + " " + PostCode.Substring(2, 3);
                    break;
                case 6:
                    PostCode = PostCode.Substring(0, 3) + " " + PostCode.Substring(3, 3);
                    break;
                case 7:
                    PostCode = PostCode.Substring(0, 4) + " " + PostCode.Substring(4, 3);
                    break;
            }

            Regex postcode = new Regex(@"(GIR 0AA)|(((A[BL]|B[ABDHLNRSTX]?|C[ABFHMORTVW]|D[ADEGHLNTY]|E[HNX]?" +
                "|F[KY]|G[LUY]?|H[ADGPRSUX]|I[GMPV]|JE|K[ATWY]|L[ADELNSU]?|M[EKL]?|N[EGNPRW]?|O[LX]|P[AEHLOR]|" +
                "R[GHM]|S[AEGKLMNOPRSTY]?|T[ADFNQRSW]|UB|W[ADFNRSV]|YO|ZE)[1-9]?[0-9]|((E|N|NW|SE|SW|W)1|EC[1-4]" +
                "|WC[12])[A-HJKMNPR-Y]|(SW|W)([2-9]|[1-9][0-9])|EC[1-9][0-9]) [0-9][ABD-HJLNP-UW-Z]{2})", 
                RegexOptions.Singleline);

            Match m = postcode.Match(PostCode);

            return (m.Success);
        }

        /// <summary>
        /// Converts a time to a string value
        /// </summary>
        /// <param name="Time"></param>
        /// <returns></returns>
        public static string TimeToString(int Time)
        {
            int time = Time;
            int hours = (int)time / 60;
            int mins = (time - (hours * 60));

            if (hours == 0)
            {
                return (String.Format("{0} minutes", mins));
            }
            else
            {
                if (hours > 1)
                {
                    if (mins == 0)
                        return (String.Format("{0} hours", hours));
                    else
                        return (String.Format("{0} hours and {1} minutes", hours, mins));
                }
                else
                {
                    if (mins == 0)
                        return (String.Format("{0} hour", hours));
                    else
                        return (String.Format("{0} hour and {1} minutes", hours, mins));
                }
            }
        }

        /// <summary>
        /// Converts a date to a string using specified culture
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Culture"></param>
        /// <returns></returns>
        public static string DateToStr(DateTime Value, string Culture)
        {
            IFormatProvider culture = new CultureInfo(Culture, true);
            string Result = Value.ToString(culture);

            Result = Result.Remove(Result.Length - 8, 8);
            return (Result);
        }

        /// <summary>
        /// Converts a date to a string using specified culture
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Culture"></param>
        /// <returns></returns>
        public static string DateToStr(DateTime Value, CultureInfo Culture)
        {
            IFormatProvider culture = Culture;
            string Result = Value.ToString(culture);

            return (Result.Remove(Result.Length - 8, 8));
        }

        #endregion Conversion

        #region Passwords

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Length"></param>
        /// <returns></returns>
        public static string RandomPassword(int Length)
        {
            Random rnd = new Random();
            string Result = String.Empty;

            for (int i = 1; i < Length; i++)
            {
                char s = (char)(byte)rnd.Next(65, 91);
                Result = Result + s;
            }

            return (Result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="acceptableCharacters"></param>
        /// <returns></returns>
        public static string GetRandomPassword(int length, 
            string acceptableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            string AcceptableChars = String.IsNullOrEmpty(acceptableCharacters) ? 
                "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ." : acceptableCharacters;
            string Result = "";

            for (int i = 0; i < length; i++)
            {
                int ch = rnd.Next(AcceptableChars.Length - 1);
                Result += AcceptableChars.Substring(ch, 1);
            }

            return Result;
        }

        #endregion Passwords

        #region HTML

        /// <summary>
        /// Removes some HTML Elements
        /// </summary>
        /// <param name="s">string containing html elements</param>
        /// <returns>string without html elements</returns>
        public static string RemoveHTMLElements(string s)
        {
            string Result = s;
            string[] Replace = new string[7] { "  ", " ", "<", ">", "\"", "\r\n", "" };
            string[] Find = new string[7] { "&nbsp; ", " &nbsp;", "&lt;", "&gt;", "&quot;", "<p>", "</p>" };

            for (int i = 0; i < Find.Length; i++)
                Result = Result.Replace(Find[i], Replace[i]);

            return (Result);
        }

        #endregion HTML

        #region File Manipulation

        /// <summary>
        /// Delete's files matching searchpattern, in folder which have not been accessed for maxDays
        /// </summary>
        /// <param name="folder">Folder to search</param>
        /// <param name="searchPattern">search Pattern</param>
        /// <param name="maxDays">Days since last accessed</param>
        public static void FileDeleteOlder(string folder, string searchPattern, int maxDays)
        {
            try
            {
                string logPath = folder;

                if (!Directory.Exists(folder))
                    return;

                string[] files = Directory.GetFiles(folder, searchPattern);
                TimeSpan lastAccessed = new TimeSpan(maxDays, 0, 0);

                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);

                    if ((DateTime.Now - info.LastWriteTime) > lastAccessed)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception error)
            {
                EventLog.Add(error);
            }
        }

        /// <summary>
        /// Returns a description of the file size
        /// </summary>
        /// <param name="bytes">number of bytes</param>
        /// <param name="decimalPlaces">Number of decimal places to return</param>
        /// <returns>string</returns>
        public static string FileSize(long bytes, int decimalPlaces)
        {
            decimalPlaces = CheckMinMax(decimalPlaces, 0, 6);
            string[] suf = { " B", " KB", " MB", " GB", " TB", " PB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), decimalPlaces);
            return (num.ToString() + suf[place]);
        }

        /// <summary>
        /// Gets the size of a file
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>number of bytes</returns>
        public static long FileSize(string fileName)
        {
            if (!File.Exists(fileName))
                return (0);

            FileInfo file = new FileInfo(fileName);
            try
            {
                return (file.Length);
            }
            finally
            {
                file = null;
            }
        }

        /// <summary>
        /// Rename's all files within a folder if the filename contains specific text
        /// 
        /// e.g. FileRename("c:\\myfiles\\", " (2)", "_2");
        /// 
        /// looks for any file with  (2) in the file name ("text file (2).txt") and renames 
        /// it to ("text file_2.txt")
        /// </summary>
        /// <param name="path">Path where files reside</param>
        /// <param name="findText">text to find within the filename</param>
        /// <param name="replaceText">text that will replace findText within the filename</param>
        public static void FileRename(string path, string findText, string replaceText)
        {
            string[] files = System.IO.Directory.GetFiles(path);

            foreach (string file in files)
            {
                if (file.Contains(findText))
                {
                    File.Move(file, file.Replace(findText, replaceText));
                }
            }
        }

        /// <summary>
        /// Obtains a CRC for a given file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="exclusiveAccess">Exclusive File Access or Not</param>
        /// <returns></returns>
        public static string FileCRC(string fileName, bool exclusiveAccess)
        {
            // open the file locked
            Stream fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, 
                exclusiveAccess ? FileShare.None : FileShare.Read);
            try
            {
                int readBytes = 0;
                int offset = 0;
                byte[] fileContents = new byte[fileStream.Length];

                int bytesToRead = fileContents.Length;

                while (bytesToRead > 0)
                {
                    int currentChunk = bytesToRead > 4196 ? 4196 : bytesToRead;

                    readBytes = fileStream.Read(fileContents, offset, currentChunk);

                    bytesToRead -= readBytes;
                    offset += readBytes;
                }

                // checksum
                SHA256 myCRC = SHA256Managed.Create();
                try
                {
                    byte[] hash = myCRC.ComputeHash(fileContents);
                    return (BitConverter.ToString(hash).Replace("-", String.Empty));
                }
                finally
                {
                    myCRC.Dispose();
                    myCRC = null;
                }
            }
            finally
            {
                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;
            }
        }

        #endregion File Manipulation

        #region Date/Time

        /// <summary>
        /// Gets the current date / time format based on culture
        /// </summary>
        /// <param name="includeTime">if true, includes short time</param>
        /// <param name="shortDate">if true short date, otherwise long date</param>
        /// <returns></returns>
        public static string DateFormat(bool includeTime, bool shortDate)
        {
            string Result = String.Empty;

            if (shortDate)
                Result = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortDatePattern;
            else
                Result = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.LongDatePattern;

            if (includeTime)
            {
                Result += String.Format(" {0}", Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortTimePattern);
            }

            return (Result);
        }

        #endregion Date/Time


        /// <summary>
        /// Determines wether value is between two values
        /// </summary>
        /// <param name="value">Value to Check</param>
        /// <param name="left">Left Value</param>
        /// <param name="right">Right Value</param>
        /// <returns>Bool if value between left and right</returns>
        public static bool Between(int value, int left, int right)
        {
            return value >= left && value <= right;
        }

        /// <summary>
        /// Checks a value is at least Minimum
        /// </summary>
        /// <param name="Minimum">Minimum allowed value</param>
        /// <param name="Value">Value to be checked</param>
        /// <returns>int Value as long as it is above Minimum, otherwise Minimum</returns>
        public static int MinimumValue(int Minimum, int Value)
        {
            if (Value < Minimum)
                return (Minimum);
            else
                return (Value);
        }

        /// <summary>
        /// Rounds up an int value
        /// </summary>
        /// <param name="Total"></param>
        /// <param name="DivBy"></param>
        /// <returns></returns>
        public static int RoundUp(int Total, int DivBy)
        {
            int Result = 0;
            int rem = 0;

            Result = Math.DivRem(Total, DivBy, out rem);

            if (rem > 0)
                Result++;

            return (Result);
        }

        /// <summary>
        /// Detects if any characters are right to left within a string
        /// </summary>
        /// <param name="s">string to check</param>
        /// <returns>true if any of the characters are right to left</returns>
        public static bool IsRightToLeftCharacter(string s)
        {
            return (Regex.IsMatch(s, @"\p{IsArabic}|\p{IsHebrew}"));
        }

        /// <summary>
        /// Formats a date for a specific culture
        /// </summary>
        /// <param name="date">Date/Time to be formatted</param>
        /// <param name="culture">Culture to be used</param>
        /// <param name="dateFormat">date format to be used</param>
        /// <returns></returns>
        public static string FormatDate(DateTime date, string culture, string dateFormat = "g")
        {
            CultureInfo cultureInfo = new CultureInfo(culture);
            return (date.ToString(dateFormat, cultureInfo));
        }

        /// <summary>
        /// Format's text removing any characters not part of allowed characters param
        /// </summary>
        /// <param name="s">Text to format</param>
        /// <param name="allowedCharacters">Characters allowed</param>
        /// <returns>s Formatted</returns>
        public static string FormatText(string s, string allowedCharacters)
        {
            string Result = String.Empty;

            foreach (char ch in s)
            {
                if (allowedCharacters.Contains(ch.ToString()))
                    Result += ch;
            }

            return (Result);
        }


        /// <summary>
        /// enusres a int value is between minimum and maximum value
        /// </summary>
        /// <param name="minimum">Minimum int value</param>
        /// <param name="maximum">Maximum int value</param>
        /// <param name="value">Value being verified</param>
        /// <returns>Value between Minimum and Maximum</returns>
        public static int ValueWithin(int minimum, int maximum, int value)
        {
            if (minimum > maximum)
                throw new InvalidOperationException("Minimum can not be greater than maximum");

            if (maximum < minimum)
                throw new InvalidOperationException("Maximum can not be less than minimum");

            int Result = value;

            if (value < minimum)
                Result = minimum;

            if (value > maximum)
                Result = maximum;

            return (Result);
        }

        /// <summary>
        /// Returns s to maximum length specified
        /// </summary>
        /// <param name="s"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string MaximumLength(string s, int length)
        {
            if (s.Length > length)
                return (s.Substring(0, length));
            else
                return (s);
        }

        /// <summary>
        /// Generates a random password which is 10 characters long
        /// </summary>
        /// <returns></returns>
        public static string GetRandomPassword()
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            string AcceptableChars = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.";
            string Result = "";

            for (int i = 0; i < 9; i++)
            {
                int ch = rnd.Next(AcceptableChars.Length - 1);
                Result += AcceptableChars.Substring(ch, 1);
            }

            return Result;
        }

        /// <summary>
        /// Generates a random key value in the form LLL-NNNNNN
        /// 
        /// Where L is a letter and N is a number
        /// </summary>
        /// <returns></returns>
        public static string GetRandomKey()
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            string Result = "";

            for (int i = 0; i < 3; i++)
            {
                char s = (char)(byte)rnd.Next(65, 91);
                Result = Result + s;
            }

            Result = Result + "-";

            for (int i = 0; i < 6; i++)
            {
                Result = Result + Convert.ToString(rnd.Next(9));
            }

            return Result;
        }

        /// <summary>
        /// Creates a hash of a string
        /// 
        /// From: https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static UInt64 Hash(string Value)
        {
            string s1 = Value.Substring(0, Value.Length / 2);
            string s2 = Value.Substring(Value.Length / 2);

            Byte[] MS4B = BitConverter.GetBytes(s1.GetHashCode());
            Byte[] LS4B = BitConverter.GetBytes(s2.GetHashCode());
            return ((UInt64)MS4B[0] << 56 | (UInt64)MS4B[1] << 48 | 
                          (UInt64)MS4B[2] << 40 | (UInt64)MS4B[3] << 32 |
                          (UInt64)LS4B[0] << 24 | (UInt64)LS4B[1] << 16 | 
                          (UInt64)LS4B[2] << 8  | (UInt64)LS4B[3]);
        }

        /// <summary>
        /// Converts a string to an MD5 value
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string HashStringMD5(string Value)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            try
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(Value);
                data = x.ComputeHash(data);
                string ret = "";
                for (int i = 0; i < data.Length; i++)
                    ret += data[i].ToString("x2").ToLower();
                return ret;
            }
            finally
            {
                x.Dispose();
                x = null;
            }
        }

        /// <summary>
        /// Buffers text ensuring it's a specified length
        /// 
        /// Appends spaces to the string
        /// </summary>
        /// <param name="Text">Text to buffer</param>
        /// <param name="Length">Required length</param>
        /// <returns></returns>
        public static string BufferText(string Text, int Length)
        {
            string Result = Text;

            while (Result.Length < Length)
                Result += " ";

            return (Result);
        }

        /// <summary>
        /// Calculates a percentage
        /// </summary>
        /// <param name="total">Total number available</param>
        /// <param name="current">current value</param>
        /// <returns>Percentage</returns>
        public static double Percentage(double total, double current)
        {
            return (Math.Round((double)(100 * current) / total));
        }

        /// <summary>
        /// Calculates a percentage
        /// </summary>
        /// <param name="total">Total number available</param>
        /// <param name="current">current value</param>
        /// <returns>Percentage</returns>
        public static int Percentage(int total, int current)
        {
            return ((int)Math.Round((double)(100 * current) / total));
        }

        /// <summary>
        /// Returns the week number for today's date
        /// </summary>
        /// <returns>Week number</returns>
        public static int GetWeek()
        {
            return (GetWeek(DateTime.Now));
        }

        /// <summary>
        /// Returns the week number for the date
        /// </summary>
        /// <param name="date">Date which the week number is sought</param>
        /// <returns>Week number</returns>
        public static int GetWeek(DateTime date)
        {
            CultureInfo cult = CultureInfo.CurrentCulture;
            return (cult.Calendar.GetWeekOfYear(date,
                cult.DateTimeFormat.CalendarWeekRule,
                cult.DateTimeFormat.FirstDayOfWeek));
        }

        #region Simple Encryption / Decryption

        /// <summary>
        /// Encrypts a string using key provided
        /// </summary>
        /// <param name="textToEncrypt">Text to encrypt</param>
        /// <param name="key">Key used to encrypt</param>
        /// <returns>Encrypted String</returns>
        public static string Encrypt(string textToEncrypt, string key)
        {
            return (StringCipher.Encrypt(textToEncrypt, key));
        }

        /// <summary>
        /// Decrypts an encrypted string 
        /// </summary>
        /// <param name="textToDecrypt">Text to decrypt</param>
        /// <param name="key">Key used to decrypt</param>
        /// <returns>Decrypted String</returns>
        public static string Decrypt(string textToDecrypt, string key)
        {
            return (StringCipher.Decrypt(textToDecrypt, key));
        }

        /// <summary>
        /// Simple encryption/decryption
        /// </summary>
        /// <param name="textToEncrypt">String to Encrypt/Decrypt</param>
        /// <param name="key">Key Value</param>
        /// <returns>If string is encrypted then decrypted string, othewise encrypted string</returns>
        public static string EncryptDecrypt(string textToEncrypt, int key = 19)
        {
            System.Text.StringBuilder inSb = new System.Text.StringBuilder(textToEncrypt);
            System.Text.StringBuilder outSb = new System.Text.StringBuilder(textToEncrypt.Length);
            char c;
            for (int i = 0; i < textToEncrypt.Length; i++)
            {
                c = inSb[i];
                c = (char)(c ^ key);
                outSb.Append(c);
            }
            return outSb.ToString();
        }

        /// <summary>
        /// Simple string encryption
        /// </summary>
        /// <param name="InStr"></param>
        /// <returns></returns>
        public static string Encrypt(string InStr)
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);

            if (InStr.Length == 0)
                return "";

            Char C = (Char)((InStr.Length + 50));
            string Result = Convert.ToString(C);
            int Offset = rnd.Next(1, 30);
            Result = Result + Convert.ToString((Char)(Offset + 30));


            //buffer out to 50 char string
            while (InStr.Length < 50)
            {
                InStr = InStr + (Char)rnd.Next(20, 120);
            }

            for (int I = 0; I <= InStr.Length - 1; I++)
            {
                Result = Result + (Char)rnd.Next(35, 125);
                Result = Result + (Char)rnd.Next(35, 125);
                Result = Result + (Char)(Byte)(InStr[I] + Offset);
            }

            return Result;
        }

        /// <summary>
        /// Simple string decryption
        /// </summary>
        /// <param name="InStr"></param>
        /// <returns></returns>
        public static string Decrypt(string InStr)
        {
            try
            {
                int Len = (Char)((InStr[0]) - 50);
                int Offset = (Char)(InStr[1] - 30);

                string Result = "";
                int J = 1;

                for (int I = 2; I <= InStr.Length - 1; I++)
                {
                    if ((J % 3) == 0)
                    {
                        Result = Result + (Char)(Byte)(InStr[I] - Offset);

                        if ((Result.Length) == Len)
                        {
                            return Result;
                        }
                    }

                    J = J + 1;
                }

                return Result;
            }
            catch
            {
                return "";
            }
        }

        #endregion Simple Encryption / Decryption

        #region Validation

        /// <summary>
        /// Determines wether the date falls within a number of days from current date
        /// </summary>
        /// <param name="date">date to check</param>
        /// <param name="Range">Number of days allowed</param>
        /// <returns>true if date is within Range number of days, otherwise false</returns>
        public static bool DateWithin(DateTime date, int Range)
        {
            DateTime check = new DateTime(DateTime.Now.Year, date.Month, date.Day);
            return (check >= DateTime.Now.AddDays(-Range) && check <= DateTime.Now.AddDays(Range));
        }

        /// <summary>
        /// Checks to see if a date falls between 2 other dates
        /// </summary>
        /// <param name="dateStart">Start Date</param>
        /// <param name="finishDate">Finish Date</param>
        /// <param name="checkDate">Date to be checked</param>
        /// <param name="ignoreYears">If true, the check will not include the years element, only the day/month</param>
        /// <returns>bool, true if falls within, otherwise false</returns>
        public static bool DateWithin(DateTime dateStart, DateTime finishDate, 
            DateTime checkDate, bool ignoreYears = false)
        {
            if (ignoreYears)
            {
                bool result = (checkDate.Date.Month >= dateStart.Date.Month && 
                               checkDate.Date.Day >= dateStart.Date.Day &&
                               checkDate.Date.Month <= finishDate.Date.Month && 
                               checkDate.Date.Day <= finishDate.Date.Day);
                return (result);
            }
            else
                return (checkDate.Date >= dateStart.Date && checkDate.Date <= finishDate.Date);
        }

        /// <summary>
        /// Checks to see if a date falls within two dates
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="finishDate"></param>
        /// <param name="checkDateStart"></param>
        /// <param name="checkDateFinish"></param>
        /// <returns></returns>
        public static bool DateWithin(DateTime dateStart, DateTime finishDate, DateTime checkDateStart, DateTime checkDateFinish)
        {
            bool Result = checkDateStart >= dateStart && checkDateStart <= finishDate;

            if (!Result)
                Result = checkDateFinish <= finishDate && checkDateFinish >= dateStart;

            return (Result);
        }

        /// <summary>
        /// Checks a value ensuring it is between an upper/lower limit
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="min">Minimum Value Allowed</param>
        /// <param name="max">Maximum Value Allowed</param>
        /// <returns>Validated Value</returns>
        public static int CheckMinMax(int value, int min, int max)
        {
            int Result = value;

            if (Result < min)
                Result = min;

            if (Result > max)
                Result = max;

            return (Result);
        }

        /// <summary>
        /// Checks a value ensuring it is between an upper/lower limit
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="min">Minimum Value Allowed</param>
        /// <param name="max">Maximum Value Allowed</param>
        /// <returns>Validated Value</returns>
        public static uint CheckMinMax(uint value, uint min, uint max)
        {
            uint Result = value;

            if (Result < min)
                Result = min;

            if (Result > max)
                Result = max;

            return (Result);
        }

        /// <summary>
        /// Checks a value ensuring it is between an upper/lower limit
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="min">Minimum Value Allowed</param>
        /// <param name="max">Maximum Value Allowed</param>
        /// <returns>Validated Value</returns>
        public static ulong CheckMinMaxU(ulong value, ulong min, ulong max)
        {
            ulong Result = value;

            if (Result < min)
                Result = min;

            if (Result > max)
                Result = max;

            return (Result);
        }

        /// <summary>
        /// Checks a value ensuring it is between an upper/lower limit
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="min">Minimum Value Allowed</param>
        /// <param name="max">Maximum Value Allowed</param>
        /// <returns>Validated Value</returns>
        public static decimal CheckMinMax(decimal value, decimal min, decimal max)
        {
            decimal Result = value;

            if (Result < min)
                Result = min;

            if (Result > max)
                Result = max;

            return (Result);
        }

        /// <summary>
        /// Checks a value ensuring it is between an upper/lower limit
        /// </summary>
        /// <param name="value">Value to be checked</param>
        /// <param name="min">Minimum Value Allowed</param>
        /// <param name="max">Maximum Value Allowed</param>
        /// <returns>Validated Value</returns>
        public static double CheckMinMax(double value, double min, double max)
        {
            double Result = value;

            if (Result < min)
                Result = min;

            if (Result > max)
                Result = max;

            return (Result);
        }

        /// <summary>
        /// Ensures the string s has a trailing backslash
        /// </summary>
        /// <param name="s">parameter s usually a path</param>
        /// <returns>s, ensuring contains a back slash as last character</returns>
        public static string AddTrailingBackSlash(string s)
        {
            if (String.IsNullOrEmpty(s))
                return (s);

            string Result = s;

            if (!Result.EndsWith("\\"))
                Result += "\\";

            return (Result);
        }

        /// <summary>
        /// Validates a string exists and meets min/max length requirements
        /// </summary>
        /// <param name="s">String to validate</param>
        /// <param name="error">Error message</param>
        /// <param name="minLength">Minimum Length</param>
        /// <param name="maxLength">Maximum Length</param>
        public static void ValidateTextExists(string s, string error, int minLength = 0, int maxLength = 32000)
        {
            if (String.IsNullOrEmpty(s))
                throw new Exception(error);

            if (s.Length < minLength)
                throw new Exception(String.Format("{0}\r\nMinimum length must be {1)", error, minLength));

            if (s.Length > maxLength)
                throw new Exception(String.Format("{0}\r\nMaximun length must be {1)", error, maxLength));
        }

        /// <summary>
        /// Validates that the string representation of an IP Address is valid
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>true if properly formatted ip address, otherwise false</returns>
        public static bool ValidateIPAddress(string ipAddress)
        {
            bool Result = false;
            IPAddress address;
            Result = IPAddress.TryParse(ipAddress, out address);

            return (Result);
        }

        /// <summary>
        /// Validates a domain name ensuring it is valid
        /// </summary>
        /// <param name="domain">Domain name to validate</param>
        /// <param name="extendedTest">Indicates wether an extended test is performed
        /// 
        /// This will physically connect to the website to ensure it exists</param>
        /// <param name="timeout">Time out in milliseconds if an extended test is performed</param>
        /// <returns>True if valid domain name, otherwise false</returns>
        public static bool ValidateDomainName(string domain, bool extendedTest = false, int timeout = 10000)
        {
            if (string.IsNullOrEmpty(domain))
                return (false);

            Regex TempReg = new Regex(@"^(http|https|ftp)://([a-zA-Z0-9_-]*(?:\.[a-zA-Z0-9_-]*)+):?([0-9]+)?/?");
            bool Result = TempReg.IsMatch(domain);

            if (Result)
            {
                string domainCheck = domain.Replace("https://", "").Replace("http://", "");

                if (domainCheck.Contains("/"))
                {
                    domainCheck = domainCheck.Substring(0, domainCheck.IndexOf("/"));
                }

                Result = Uri.CheckHostName(domainCheck) != UriHostNameType.Unknown;
            }

            if (Result && extendedTest)
            {
                try
                {
                    WebRequest request = WebRequest.Create(domain);
                    try
                    {
                        request.Timeout = timeout;

                        WebResponse response = request.GetResponse();
                        try
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {

                                string data = reader.ReadToEnd();

                                return (true);
                            }
                        }
                        catch
                        {
                            return (false);
                        }
                        finally
                        {
                            if (response != null)
                            {
                                response.Close();
                                response = null;
                            }
                        }
                    }
                    finally
                    {
                        request = null;
                    }
                }
                catch (WebException err)
                {
                    if (err.Status != WebExceptionStatus.Success)
                        return (false);
                }
            }

            return (false);
        }

        private static bool invalid = false;

        /// <summary>
        /// Validates an Email Address ensuring it is valid
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if valid email address, otherwise false</returns>
        public static bool ValidateEmail(string email)
        {
            invalid = false;

            if (String.IsNullOrEmpty(email))
                return (false);

            try
            {
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper);
            }
            catch (Exception)
            {
                return (false);
            }

            if (invalid)
                return (false);

            // Return true if strIn is in valid e-mail format. 
            try
            {
                return (Regex.IsMatch(email,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase));
            }
            catch (Exception)
            {
                return (false);
            }
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;

            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }

            return (match.Groups[1].Value + domainName);
        }

        #endregion Validation

        #region Random Strings

        internal static string GetRandomWord(int length)
        {
            Random rnd = new Random(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
            string AcceptableChars = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.-";
            string Result = "";

            for (int i = 0; i < length; i++)
            {
                int ch = rnd.Next(AcceptableChars.Length - 1);
                Result += AcceptableChars.Substring(ch, 1);
            }

            return Result;
        }

        #endregion Random Strings

        #region XML

        /// <summary>
        /// Set's an XML value in a file
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <param name="parentName"></param>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        internal static void XMLSetValue(string xmlFile, string parentName, string keyName, string value)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile);
            XmlNode Root = xmldoc.DocumentElement;
            bool FoundParent = false;
            bool Found = false;
            XmlNode xmlParentNode = null;

            if (Root != null & Root.Name == "WebDefender")
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        FoundParent = true;
                        xmlParentNode = Child;

                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                Item.InnerText = value;
                                Found = true;
                                xmldoc.Save(xmlFile);
                                return;
                            }
                        }
                    }
                }
            }

            if (!Found)
            {
                if (!FoundParent)
                {
                    xmlParentNode = xmldoc.CreateNode(XmlNodeType.Element, "", parentName, null);
                    //XmlElement appendedParentName = xmldoc.CreateElement(ParentName);
                    Root.AppendChild(xmlParentNode);
                }

                XmlElement appendedKeyName = xmldoc.CreateElement(keyName);
                XmlText xmlKeyName = xmldoc.CreateTextNode(value);
                appendedKeyName.AppendChild(xmlKeyName);
                xmlParentNode.AppendChild(appendedKeyName);

                xmldoc.Save(xmlFile);
            }
        }

        internal static bool XMLGetValue(string xmlFile, string parentName, string keyName, bool defaultValue)
        {
            try
            {
                return (Convert.ToBoolean(XMLGetValue(xmlFile, parentName, keyName)));
            }
            catch
            {
                return (defaultValue);
            }
        }

        internal static int XMLGetValue(string xmlFile, string parentName, string keyName, int defaultValue)
        {
            int Result = defaultValue;


            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile);
            XmlNode Root = xmldoc.DocumentElement;

            if (Root != null & Root.Name == "WebDefender")
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                try
                                {
                                    Result = Convert.ToInt32(Item.InnerText);
                                }
                                catch
                                {
                                    Result = defaultValue;
                                }

                                return (Result);
                            }
                        }
                    }
                }
            }

            return (Result);
        }

        internal static DateTime XMLGetValue(string xmlFile, string parentName, string keyName, DateTime defaultValue)
        {
            DateTime Result = defaultValue;


            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile);
            XmlNode Root = xmldoc.DocumentElement;

            if (Root != null & Root.Name == "WebDefender")
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                try
                                {
                                    Result = Convert.ToDateTime(Item.InnerText);
                                }
                                catch
                                {
                                    Result = defaultValue;
                                }

                                return (Result);
                            }
                        }
                    }
                }
            }

            return (Result);
        }

        internal static string XMLGetValue(string xmlFile, string parentName, string keyName, string defaultValue)
        {
            string Result = defaultValue;


            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile);
            XmlNode Root = xmldoc.DocumentElement;

            if (Root != null & Root.Name == "WebDefender")
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                try
                                {
                                    Result = Item.InnerText;
                                }
                                catch
                                {
                                    Result = defaultValue;
                                }

                                return (Result);
                            }
                        }
                    }
                }
            }

            return (Result);
        }

        internal static string XMLGetValue(string xmlFile, string parentName, string keyName)
        {
            string Result = "";


            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile);
            XmlNode Root = xmldoc.DocumentElement;

            if (Root != null & Root.Name == "WebDefender")
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                Result = Item.InnerText;
                                return (Result);
                            }
                        }
                    }
                }
            }

            return (Result);
        }

        #endregion XML

        #region Settings

        /// <summary>
        /// Saves Email Credentials to file
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="host"></param>
        /// <param name="sender"></param>
        /// <param name="port"></param>
        /// <param name="ssl"></param>
        public static void SaveEmailCredentials(string userName, string password, string host, string sender,
            int port, bool ssl)
        {
            XML.SetXMLValue("Email", "SSL", StringCipher.Encrypt(ssl.ToString(), StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Port", StringCipher.Encrypt(port.ToString(), StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "User", StringCipher.Encrypt(userName, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Password", StringCipher.Encrypt(password, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Host", StringCipher.Encrypt(host, StringCipher.DefaultPassword));
            XML.SetXMLValue("Email", "Sender", StringCipher.Encrypt(sender, StringCipher.DefaultPassword));
        }

        #endregion Settings

        #region Non Encrypted Files

        /// <summary>
        /// saves a string to a file
        /// 
        /// Overwrites if file already exists
        /// </summary>
        /// <param name="fileName">Filename and path</param>
        /// <param name="contents">String data to be encryped and saved</param>
        public static void FileWrite(string fileName, string contents)
        {
            string folder = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (File.Exists(fileName))
                File.Delete(fileName);

            if (String.IsNullOrEmpty(contents))
                throw new InvalidDataException("contents can not be empty");

            StreamWriter w = File.AppendText(fileName);
            try
            {
                w.Write(contents);
            }
            finally
            {
                // Update the underlying file.
                w.Flush();
                w.Close();
                w.Dispose();
                w = null;
            }

        }

        /// <summary>
        /// Opens a file, returns the contents
        /// </summary>
        /// <param name="fileName">Filename and path</param>
        /// <param name="notFoundException">Determines wether an exception is raised if the file does not exist</param>
        /// <param name="iteration"></param>
        /// <returns>Returns contents of the file</returns>
        public static string FileRead(string fileName, bool notFoundException, int iteration = 0)
        {
            string Result = String.Empty;
            try
            {
                if (!File.Exists(fileName))
                {
                    if (notFoundException)
                        throw new FileNotFoundException();
                    else
                        return (Result);
                }

                StreamReader rdr = new StreamReader(fileName);
                try
                {
                    Result = rdr.ReadToEnd();
                }
                finally
                {
                    rdr.Close();
                    rdr.Dispose();
                    rdr = null;
                }
            }
            catch (Exception err)
            {
                if (err.Message.Contains("because it is being used by another process") && iteration < 10)
                {
                    System.Threading.Thread.Sleep(500);
                    return (FileRead(fileName, notFoundException, iteration + 1));
                }
            }

            return (Result);
        }

        #endregion Non Encrypted Files

        #region Encrypted Files

        /// <summary>
        /// Encrypts a string and saves it to a file
        /// 
        /// Overwrites if file already exists
        /// </summary>
        /// <param name="fileName">Filename and path</param>
        /// <param name="contents">String data to be encryped and saved</param>
        /// <param name="key">key used to encrypt the contents</param>
        public static void FileEncryptedWrite(string fileName, string contents, string key)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            if (String.IsNullOrEmpty(contents))
                throw new InvalidDataException("contents can not be empty");

            if (String.IsNullOrEmpty(key) || key == "default")
                key = StringCipher.DefaultPassword;

            StreamWriter w = File.AppendText(fileName);
            try
            {
                w.Write(StringCipher.Encrypt(contents, key));
            }
            finally
            {
                // Update the underlying file.
                w.Flush();
                w.Close();
                w.Dispose();
                w = null;
            }

        }

        /// <summary>
        /// Opens a file, decrypts the contents
        /// </summary>
        /// <param name="fileName">Filename and path</param>
        /// <param name="key">key used to decrypt the contents</param>
        /// <returns>Decrypted contents of the file</returns>
        public static string FileEncryptedRead(string fileName, string key)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException("File does not exist", fileName);

            if (String.IsNullOrEmpty(key) || key == "default")
                key = StringCipher.DefaultPassword;

            string Result = String.Empty;

            StreamReader rdr = new StreamReader(fileName);
            try
            {
                Result = StringCipher.Decrypt(rdr.ReadToEnd(), key);
            }
            finally
            {
                rdr.Close();
                rdr.Dispose();
                rdr = null;
            }

            return (Result);
        }

        #endregion Encrypted Files

        #region Database Connection String

        /// <summary>
        /// Retrieves part of a database connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetDatabasePart(string connectionString, string name)
        {
            string Result = String.Empty;

            string[] parts = connectionString.Split(';');

            foreach (string s in parts)
            {
                string[] values = s.Split('=');

                if (values[0].ToLower() == name.ToLower())
                {
                    Result = values[1];
                    break;
                }
            }

            return (Result);
        }

        #endregion Database Connection String
    }


    /// <summary>
    /// Encrypt/Decrypt strings
    /// </summary>
    internal static class StringCipher
    {
        // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string initVector = "l4uf17G0S5hpo9tS";

        // This constant is used to determine the keysize of the encryption algorithm.
        private const int keysize = 256;

        internal const string DefaultPassword = ";lkjass,df04rl.nkfasdf658sd";

        internal static string Encrypt(string plainText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            try
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);

                RijndaelManaged symmetricKey = new RijndaelManaged();
                try
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                    try
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        try
                        {
                            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                            try
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                return Convert.ToBase64String(memoryStream.ToArray());
                            }
                            finally
                            {
                                cryptoStream.Close();
                                cryptoStream.Dispose();
                                cryptoStream = null;
                            }
                        }
                        finally
                        {
                            memoryStream.Close();
                            memoryStream.Dispose();
                            memoryStream = null;
                        }
                    }
                    finally
                    {
                        encryptor.Dispose();
                        encryptor = null;
                    }
                }
                finally
                {
                    symmetricKey.Dispose();
                    symmetricKey = null;
                }
            }
            finally
            {
                password.Dispose();
                password = null;
            }
        }

        internal static string Decrypt(string cipherText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            try
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);

                RijndaelManaged symmetricKey = new RijndaelManaged();
                try
                {
                    symmetricKey.Mode = CipherMode.CBC;

                    ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                    try
                    {
                        MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
                        try
                        {
                            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                            try
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                            finally
                            {
                                cryptoStream.Close();
                                cryptoStream.Dispose();
                                cryptoStream = null;
                            }
                        }
                        finally
                        {
                            memoryStream.Close();
                            memoryStream.Dispose();
                            memoryStream = null;
                        }
                    }
                    finally
                    {
                        decryptor.Dispose();
                        decryptor = null;
                    }
                }
                finally
                {
                    symmetricKey.Dispose();
                    symmetricKey = null;
                }
            }
            finally
            {
                password.Dispose();
                password = null;
            }
        }
    }

}
