/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2014 Simon Carter
 *
 *  Purpose:  TextMagic SMS class
 *
 */

#pragma warning disable IDE0028 // collection intialization can be simplified

namespace Shared.Communication
{
    /// <summary>
    /// Wrapper for Text Magic SMS Sending
    /// 
    /// https://www.textmagic.com
    /// </summary>
    public sealed class SendSMSTextMagic
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="username"></param>
        /// <param name="key"></param>
        public SendSMSTextMagic(string username, string key)
        {
            Username = username;
            Key = key;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Text Magic username
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// TextMagic Key
        /// </summary>
        public string Key { get; private set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Sends an SMS message
        /// </summary>
        /// <param name="from">Sender account detail</param>
        /// <param name="telephone">Telephone to send to</param>
        /// <param name="message">Message to send</param>
        /// <returns></returns>
        public bool SMSSend(string from, string telephone, string message)
        {
            Classes.NVPCodec headers = new Classes.NVPCodec();
            headers.Add("X-TM-Username", Username);
            headers.Add("X-TM-Key", Key);

            Classes.NVPCodec codec = new Classes.NVPCodec();
            codec.Add("from", from);
            codec.Add("phones", telephone);
            codec.Add("text", message);
            HttpPost.Post("https://rest.textmagic.com/api/v2/messages", codec, 30, headers);

            return (true);
        }

        #endregion Public Methods
    }
}
