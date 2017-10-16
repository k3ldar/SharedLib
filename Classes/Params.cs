/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2015 Simon Carter
 *
 *  Purpose:  Static class for managing application parameters
 *
 */
using System;
using System.Collections.Generic;

namespace Shared.Classes
{
    /// <summary>
    /// Static class for managing Application Params
    /// </summary>
    public static class Parameters
    {
        #region Private Members

        /// <summary>
        /// Dictionary of parameters
        /// </summary>
        private static Dictionary<string, string> _parameters;

        /// <summary>
        /// Case sensitive argument names
        /// </summary>
        private static bool _caseSensitive = false;

        /// <summary>
        /// Lock object
        /// </summary>
        private static object _lockObject = new object();

        #endregion Private Members

        #region Public Methods

        /// <summary>
        /// Initialises the parameters
        /// </summary>
        /// <param name="args">Appliction arguments
        /// 
        /// i.e. /t  /o:option -i iOption</param>
        /// <param name="paramSeperators">Parameter seperators
        /// 
        /// i.e. / or -
        /// </param>
        /// <param name="optionSeperators">Parameters that seperate options after an argument
        /// 
        /// i.e. : or space</param>
        /// <param name="caseSensitiveArgName">Indicates whether argument names are case sensitive</param>
        public static void Initialise(string[] args, char[] paramSeperators, 
            char[] optionSeperators, bool caseSensitiveArgName = false)
        {
            using (TimedLock.Lock(_lockObject))
            {
                if (_parameters == null)
                {
                    _parameters = new Dictionary<string, string>();
                    _caseSensitive = caseSensitiveArgName;
                    ProcessArgs(args, paramSeperators, optionSeperators);
                }
            }
        }

        /// <summary>
        /// Initialises the parameters
        /// </summary>
        /// <param name="args">Appliction arguments
        /// 
        /// i.e. /t  /o:option -i iOption</param>
        /// <param name="paramSeperator">Parameter seperators
        /// 
        /// i.e. / or -
        /// </param>
        /// <param name="optionSeperator">Parameters that seperate options after an argument
        /// 
        /// i.e. : or space</param>
        /// <param name="caseSensitiveArgName">Indicates wether argument names are case sensitive</param>
        public static void Initialise(string[] args, char paramSeperator, 
            char optionSeperator, bool caseSensitiveArgName = false)
        {
            using (TimedLock.Lock(_lockObject))
            {
                if (_parameters == null)
                {
                    _parameters = new Dictionary<string, string>();
                    _caseSensitive = caseSensitiveArgName;
                    ProcessArgs(args, paramSeperator, optionSeperator);
                }
            }
        }

        /// <summary>
        /// Finalises the dictionary
        /// </summary>
        public static void Finalise()
        {
            if (_parameters != null)
                _parameters = null;
        }

        /// <summary>
        /// Determines wether a parameter exists
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static bool OptionExists(string paramName)
        {
            if (_parameters == null)
                throw new Exception("Parameters not initialised");

            using (TimedLock.Lock(_lockObject))
            {
                return (_parameters.ContainsKey(_caseSensitive ? paramName : paramName.ToLower()));
            }
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static string GetOption(string paramName, string defaultValue = "")
        {
            if (_parameters == null)
                throw new Exception("Parameters not initialised");

            using (TimedLock.Lock(_lockObject))
            {
                if (_parameters.ContainsKey(_caseSensitive ? paramName : paramName.ToLower()))
                {
                    return (_parameters[_caseSensitive ? paramName : paramName.ToLower()]);
                }
            }

            return (defaultValue);
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static bool GetOption(string paramName, bool defaultValue = false)
        {
            return (Utilities.StrToBool(GetOption(paramName, defaultValue.ToString())));
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static double GetOption(string paramName, double defaultValue = 0.0)
        {
            return (Utilities.StrToDbl(GetOption(paramName, defaultValue.ToString())));
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static Int64 GetOption(string paramName, Int64 defaultValue = 0)
        {
            return (Utilities.StrToInt64(GetOption(paramName, defaultValue.ToString()), defaultValue));
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static int GetOption(string paramName, int defaultValue = 0)
        {
            return (Utilities.StrToIntDef(GetOption(paramName, defaultValue.ToString()), defaultValue));
        }

        /// <summary>
        /// Retrieves a parameter value
        /// </summary>
        /// <param name="paramName">parameter name
        /// 
        /// does not include the parameter seperator i.e. - or /</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value if found, otherwise defaultValue</returns>
        public static uint GetOption(string paramName, uint defaultValue = 0)
        {
            return (Utilities.StrToUInt(GetOption(paramName, defaultValue.ToString()), defaultValue));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// converts string[] to dictionary
        /// </summary>
        /// <param name="args">Appliction arguments
        /// 
        /// i.e. /t  /o:option -i iOption</param>
        /// <param name="paramSeperator">Parameter seperators
        /// 
        /// i.e. / or -
        /// </param>
        /// <param name="optionSeperator">Parameters that seperate options after an argument
        /// 
        /// i.e. : or space (' ')</param>
        private static void ProcessArgs(string[] args, char paramSeperator, char optionSeperator)
        {
            bool newEntry = false;
            string newName = String.Empty;
            string currentEntry = String.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];

                if (s.StartsWith(paramSeperator.ToString()))
                {
                    newEntry = true;

                    if (newEntry && !String.IsNullOrEmpty(newName) || !String.IsNullOrEmpty(currentEntry))
                    {
                        if (!String.IsNullOrEmpty(newName))
                            _parameters.Add(newName, currentEntry);

                        newName = String.Empty;
                        currentEntry = String.Empty;
                    }


                    if (s.Contains(optionSeperator.ToString()))
                    {
                        int posSep = s.IndexOf(optionSeperator.ToString());
                        string beginString = s.Substring(1, posSep);
                        string endString = s.Substring(posSep + 1);
                        newEntry = false;
                        _parameters.Add(beginString, endString);
                        newName = String.Empty;
                    }
                    else
                    {
                        newName = _caseSensitive ? s.Substring(1) : s.Substring(1).ToLower();
                        currentEntry = String.Empty;
                    }
                }
                else
                {
                    currentEntry += String.Format("{0}{1}", String.IsNullOrEmpty(currentEntry) ? String.Empty : " ", s);
                }

                // don't forget last entry in the array
                if (i == args.Length - 1)
                {
                    if (!String.IsNullOrEmpty(newName))
                        _parameters.Add(newName, currentEntry);
                }
            }
        }

        /// <summary>
        /// converts string[] to dictionary
        /// </summary>
        /// <param name="args">Appliction arguments
        /// 
        /// i.e. /t  /o:option -i iOption</param>
        /// <param name="paramSeperators">Parameter seperators
        /// 
        /// i.e. / or -
        /// </param>
        /// <param name="optionSeperators">Parameters that seperate options after an argument
        /// 
        /// i.e. : or space (' ')</param>
        private static void ProcessArgs(string[] args, char[] paramSeperators, char[] optionSeperators)
        {
            bool newEntry = false;
            string newName = String.Empty;
            string currentEntry = String.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];

                try
                {
                    if (StartsWithSeperator(s, paramSeperators))
                    {
                        newEntry = true;

                        if (newEntry && !String.IsNullOrEmpty(newName) || !String.IsNullOrEmpty(currentEntry))
                        {
                            _parameters.Add(newName, currentEntry);

                            newName = String.Empty;
                            currentEntry = String.Empty;
                        }

                        string beginString = String.Empty;
                        string endString = String.Empty;

                        if (ContainsSeperator(s, optionSeperators, ref beginString, ref endString))
                        {
                            newEntry = false;
                            _parameters.Add(beginString.Substring(1), endString);
                            newName = String.Empty;
                        }
                        else
                        {
                            newName = _caseSensitive ? s.Substring(1) : s.Substring(1).ToLower();
                            currentEntry = String.Empty;
                        }
                    }
                    else
                    {
                        currentEntry += String.Format("{0}{1}", String.IsNullOrEmpty(currentEntry) ? String.Empty : " ", s);
                    }
                }
                catch (Exception err)
                {
                    if (err.Message.Contains("An item with the same key has already been added"))
                        continue;
                }

                // don't forget last entry in the array
                if (i == args.Length -1)
                {
                    if (!String.IsNullOrEmpty(newName))
                        _parameters.Add(newName, currentEntry);
                }
            }
        }

        /// <summary>
        /// Determines wether a string starts with a seperator
        /// </summary>
        /// <param name="s">string to check</param>
        /// <param name="seperators">array of seperators</param>
        /// <returns>true if it starts with otherwise false</returns>
        private static bool StartsWithSeperator(string s, char[] seperators)
        {
            foreach (char sep in seperators)
            {
                if (s.StartsWith(sep.ToString()))
                {
                    return (true);
                }
            }

            return (false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="seperators"></param>
        /// <param name="beginString"></param>
        /// <param name="endString"></param>
        /// <returns></returns>
        private static bool ContainsSeperator(string s, char[] seperators, ref string beginString, ref string endString)
        {
            foreach (char sep in seperators)
            {
                if (s.Contains(sep.ToString()))
                {
                    int posSep = s.IndexOf(sep.ToString());
                    beginString = s.Substring(0, posSep);

                    if (!_caseSensitive)
                        beginString = beginString.ToLower();

                    endString = s.Substring(posSep + 1);
                    return (true);
                }
            }

            return (false);
        }

        #endregion Private Methods
    }
}
