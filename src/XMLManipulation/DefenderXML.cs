/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2010 Simon Carter
 *
 *  Purpose:  XML Wrapper Functions
 *
 */
using Shared.Classes;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Shared
{
    /// <summary>
    /// XML Manipulation Class
    /// </summary>
    public static class XML
    {
        #region Private Static Members

        /// <summary>
        /// Dictionary for in memory xml file to speed up reading/writing
        /// </summary>
        private static readonly Dictionary<string, XMLMemoryFile> _memoryXMLFile = new Dictionary<string, XMLMemoryFile>();



        /// <summary>
        /// Lock object for asynchronous access
        /// </summary>
        private static readonly object _lockObject = new object();

        #endregion Private Static Members

        internal static string _xmlDefaultFile
        {
            get
            {
                return Utilities.CurrentPath(true) + "WebDefender.xml";
            }
        }

        private static string LoadXMLFile(ref XmlDocument doc, string xmlFile,
            string rootNode = "WebDefender", bool lockFile = true)
        {
            if (String.IsNullOrEmpty(xmlFile))
                xmlFile = _xmlDefaultFile;

            using (TimedLock.Lock(_lockObject))
            {
                if (_memoryXMLFile.ContainsKey(xmlFile))
                {
                    //has time out limit been reached?


                    doc = _memoryXMLFile[xmlFile].Document;
                    return xmlFile;
                }
            }

            if (xmlFile.StartsWith("http") | System.IO.File.Exists(xmlFile))
            {
                using (TimedLock.Lock(_lockObject))
                {
                    doc.Load(xmlFile);
                }
            }
            else
            {
                XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", null, null);
                doc.AppendChild(dec);// Create the root element
                XmlElement root = doc.CreateElement(rootNode);
                doc.AppendChild(root);
            }

            return xmlFile;
        }

        #region Public Static Methods

        #region Memory / Speed Saving

        /// <summary>
        /// Loads an XML file into memory for faster read / write
        /// </summary>
        /// <param name="fileName">File to load</param>
        /// <param name="timeOut">Time out in seconds - Default 2 minutes</param>
        /// <param name="rootNode">Root node of XML File</param>
        public static void BeginUpdate(string fileName, ulong timeOut = 120, string rootNode = "WebDefender")
        {
            // min time out 5 seconds, max timeout 20 minutes
            timeOut = Utilities.CheckMinMaxU(timeOut, 5, 20 * 60);

            using (TimedLock.Lock(_lockObject))
            {
                // is the file still in memory
                if (_memoryXMLFile.ContainsKey(fileName))
                    return;
            }

            // not found, add to cache
            XMLMemoryFile memFile = new XMLMemoryFile(fileName, timeOut);
            XmlDocument doc = memFile.Document;
            LoadXMLFile(ref doc, fileName, rootNode);

            using (TimedLock.Lock(_lockObject))
            {
                _memoryXMLFile.Add(fileName, memFile);
            }
        }

        /// <summary>
        /// Ends an update on the file and removes from cache
        /// </summary>
        /// <param name="fileName">xml file</param>
        /// <param name="save">Determines wether contents should be saved</param>
        public static void EndUpdate(string fileName, bool save)
        {
            using (TimedLock.Lock(_lockObject))
            {
                // is the file still in memory
                if (!_memoryXMLFile.ContainsKey(fileName))
                    return;

                // found, save file and remove from cache
                if (save)
                {
                    XMLMemoryFile memFile = _memoryXMLFile[fileName];
                    memFile.Document.Save(memFile.FileName);
                }

                _memoryXMLFile.Remove(fileName);
            }
        }

        #endregion Memory / Speed Saving

        #region Read / Write

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentName"></param>
        /// <param name="keyName"></param>
        /// <param name="xmlFile"></param>
        /// <param name="rootName"></param>
        public static void DeleteXMLValue(string parentName, string keyName,
            string xmlFile, string rootName)
        {
            XmlDocument xmldoc = new XmlDocument();

            xmlFile = LoadXMLFile(ref xmldoc, xmlFile);
            XmlNode Root = xmldoc.DocumentElement;
            XmlNode xmlParentNode = null;

            if (Root != null & Root.Name == rootName)
            {
                for (int i = 0; i <= Root.ChildNodes.Count - 1; i++)
                {
                    XmlNode Child = Root.ChildNodes[i];

                    if (Child.Name == parentName)
                    {
                        xmlParentNode = Child;

                        for (int j = 0; j <= Child.ChildNodes.Count - 1; j++)
                        {
                            XmlNode Item = Child.ChildNodes[j];

                            if (Item.Name == keyName)
                            {
                                Child.RemoveChild(Item);

                                using (TimedLock.Lock(_lockObject))
                                {
                                    xmldoc.Save(xmlFile);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set's an XML value in a file
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <param name="parentName"></param>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        /// <param name="rootName"></param>
        public static void SetXMLValue(string parentName, string keyName,
            string value, string xmlFile, string rootName)
        {
            XmlDocument xmldoc = new XmlDocument();

            xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

            XmlNode Root = xmldoc.DocumentElement;
            bool FoundParent = false;
            bool Found = false;
            XmlNode xmlParentNode = null;

            if (Root != null & Root.Name == rootName)
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

                                using (TimedLock.Lock(_lockObject))
                                {
                                    xmldoc.Save(xmlFile);
                                }
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

                using (TimedLock.Lock(_lockObject))
                {
                    xmldoc.Save(xmlFile);
                }
            }
        }

        /// <summary>
        /// Set's an XML value in a file
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <param name="parentName"></param>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        public static void SetXMLValue(string parentName, string keyName,
            string value, string xmlFile = "")
        {
            XmlDocument xmldoc = new XmlDocument();

            xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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

                                using (TimedLock.Lock(_lockObject))
                                {
                                    xmldoc.Save(xmlFile);
                                }

                                break;
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

                using (TimedLock.Lock(_lockObject))
                {
                    xmldoc.Save(xmlFile);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentName"></param>
        /// <param name="keyName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="xmlFile"></param>
        /// <returns></returns>
        public static Int64 GetXMLValue(string parentName, string keyName,
            Int64 defaultValue, string xmlFile = "")
        {
            Int64 Result = defaultValue;
            try
            {
                Int64.TryParse(GetXMLValue(parentName, keyName, defaultValue.ToString(), xmlFile), out Result);
            }
            catch
            {
                Result = defaultValue;
            }

            return Result;
        }

        /// <summary>
        /// returns a boolean value
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>boolean value if found, otherwise default value</returns>
        public static bool GetXMLValue(string parentName, string keyName,
            bool defaultValue, string xmlFile = "")
        {
            try
            {
                return Convert.ToBoolean(GetXMLValue(parentName, keyName, xmlFile));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Returns an integer XML Value
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>int value from xml file if found, otherwise the default value</returns>
        public static int GetXMLValue(string parentName, string keyName,
            int defaultValue, string xmlFile = "")
        {
            int Result = defaultValue;


            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }

        /// <summary>
        /// Returns a ulong value from an XML File
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>ulong value from xml file if found, otherwise default value</returns>
        public static ulong GetXMLValueU(string parentName, string keyName,
            ulong defaultValue, string xmlFile = "")
        {
            ulong Result = defaultValue;

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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
                                        Result = Convert.ToUInt64(Item.InnerText);
                                    }
                                    catch
                                    {
                                        Result = defaultValue;
                                    }

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }

        /// <summary>
        /// Returns an Int64 value from an xml file
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>Int64 value if found, otherwise default value</returns>
        public static Int64 GetXMLValue64(string parentName, string keyName,
            Int64 defaultValue, string xmlFile = "")
        {
            Int64 Result = defaultValue;

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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
                                        Result = Convert.ToInt64(Item.InnerText);
                                    }
                                    catch
                                    {
                                        Result = defaultValue;
                                    }

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }

        /// <summary>
        /// Returns a date/time value
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>Date/time value if found, otherwise default value</returns>
        public static DateTime GetXMLValue(string parentName, string keyName,
            DateTime defaultValue, string xmlFile = "")
        {
            DateTime Result = defaultValue;


            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }

        /// <summary>
        /// Returns a string from xml file
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>string value if found, otherwise default value</returns>
        public static string GetXMLValue(string parentName, string keyName,
            string defaultValue, string xmlFile = "")
        {
            string Result = defaultValue;

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }


            return Result;
        }

        /// <summary>
        /// Returns a string from xml file
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <param name="rootNode"></param>
        /// <returns>string value if found, otherwise default value</returns>
        public static string GetXMLValue(string parentName, string keyName,
            string defaultValue, string xmlFile, string rootNode)
        {
            string Result = defaultValue;

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

                XmlNode Root = xmldoc.DocumentElement;

                if (Root != null & Root.Name == rootNode)
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

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }


            return Result;
        }

        /// <summary>
        /// returns a string value from xml file
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>string value if found, otherwise empty string</returns>
        public static string GetXMLValue(string parentName, string keyName,
            string xmlFile = "")
        {
            string Result = "";

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

                XmlNode Root = xmldoc.DocumentElement;

                if (Root != null && Root.Name == "WebDefender")
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
                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }

        /// <summary>
        /// Returns an Int64 value from an xml file
        /// </summary>
        /// <param name="parentName">Parent Node</param>
        /// <param name="keyName">Key Name</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="xmlFile">XML File</param>
        /// <returns>Int64 value if found, otherwise default value</returns>
        public static uint GetXMLValue64(string parentName, string keyName,
            uint defaultValue, string xmlFile = "")
        {
            uint Result = defaultValue;

            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmlFile = LoadXMLFile(ref xmldoc, xmlFile);

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
                                        Result = Convert.ToUInt32(Item.InnerText);
                                    }
                                    catch
                                    {
                                        Result = defaultValue;
                                    }

                                    return Result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (!err.Message.Contains("remote name could not be resolved"))
                    throw;
            }
            finally
            {
                xmldoc = null;
            }

            return Result;
        }


        #endregion Read / Write

        #endregion Public Static Methods
    }

    /// <summary>
    /// XML Memory file
    /// </summary>
    internal sealed class XMLMemoryFile
    {
        #region Constructors

        internal XMLMemoryFile(string fileName, ulong timeOut)
        {
            FileName = FileName;
            TimeOut = timeOut;
            DateAdded = DateTime.Now;
            Document = new XmlDocument();
        }

        #endregion Constructors

        #region Properties

        internal XmlDocument Document { get; set; }

        internal string FileName { get; private set; }

        internal DateTime DateAdded { get; private set; }

        internal ulong TimeOut { get; private set; }

        #endregion Properties
    }
}
