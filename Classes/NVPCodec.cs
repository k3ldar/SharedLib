/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by unknown
 *
 *  Copyright (c) unknown
 *
 *  Purpose:  Extended Name / Value pair
 *
 */
using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Shared.Classes
{
    /// <summary>
    /// Name / Value Pair Collection
    /// </summary>
    public sealed class NVPCodec : NameValueCollection
    {
        #region Private / Protected Members

        private const string AMPERSAND = "&";
        private const string EQUALS = "=";
        private static readonly char[] AMPERSAND_CHAR_ARRAY = AMPERSAND.ToCharArray();
        private static readonly char[] EQUALS_CHAR_ARRAY = EQUALS.ToCharArray();

        #endregion Private / Protected Members

        #region Public Methods

        /// <summary>
        /// Returns the built NVP string of all name/value pairs in the Name Value Collection
        /// </summary>
        /// <returns></returns>
        public string Encode()
        {
            StringBuilder sb = new StringBuilder();
            bool firstPair = true;
            foreach (string kv in AllKeys)
            {
                string name = UrlEncode(kv);
                string value = UrlEncode(this[kv]);
                if (!firstPair)
                {
                    sb.Append(AMPERSAND);
                }
                sb.Append(name).Append(EQUALS).Append(value);
                firstPair = false;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Decoding the string
        /// </summary>
        /// <param name="nvpstring"></param>
        public void Decode(string nvpstring)
        {
            Clear();
            foreach (string nvp in nvpstring.Split(AMPERSAND_CHAR_ARRAY))
            {
                string[] tokens = nvp.Split(EQUALS_CHAR_ARRAY);
                if (tokens.Length >= 2)
                {
                    string name = UrlDecode(tokens[0]);
                    string value = UrlDecode(tokens[1]);
                    Add(name, value);
                }
            }
        }


        #endregion Public Methods

        #region Private Methods

        private static string UrlDecode(string s) { return HttpUtility.UrlDecode(s); }
        private static string UrlEncode(string s) { return HttpUtility.UrlEncode(s); }

        #endregion Private Methods

        #region Array methods

        /// <summary>
        /// Adds a new Name/Value pair at specific index
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        public void Add(string name, string value, int index)
        {
            this.Add(GetArrayName(index, name), value);
        }

        /// <summary>
        /// Removes a name value pair
        /// </summary>
        /// <param name="arrayName"></param>
        /// <param name="index"></param>
        public void Remove(string arrayName, int index)
        {
            this.Remove(GetArrayName(index, arrayName));
        }

        /// <summary>
        /// 
        /// </summary>
        public string this[string name, int index]
        {
            get
            {
                return this[GetArrayName(index, name)];
            }
            set
            {
                this[GetArrayName(index, name)] = value;
            }
        }

        private string GetArrayName(int index, string name)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return (name + index);
        }
        #endregion
    }
}
