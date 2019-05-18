/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2015 Simon Carter
 *
 *  Purpose:  HTTP Post Methods
 *
 */
using System;
using System.IO;
using System.Net;
using System.Text;

#if NET461
using System.Web;
#endif

using Shared.Classes;

namespace Shared.Communication
{
    /// <summary>
    /// Class to perform http post
    /// </summary>
    public static class HttpPost
    {
        /// <summary>
        /// Performs a post to a webpage
        /// </summary>
        /// <param name="url">url where data will be posted</param>
        /// <param name="parameters">Name value parameters
        /// 
        /// Must be in the form NAME0=VALUE0 seperated by ampersand </param>
        /// <param name="timeout">Timeout in seconds for the request</param>
        /// <param name="additionalHeaders">Additional header values</param>
        /// <param name="userAgent">User agent to be sent with the request</param>
        /// <param name="contentType">Content type</param>
        /// <returns>string data returned by webpage</returns>
        public static string Post(string url, string parameters, uint timeout = 30, 
            NVPCodec additionalHeaders = null,
            string userAgent = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)",
            string contentType = "application/x-www-form-urlencoded")
        {
            string Result = String.Empty;

            byte[] data = Encoding.ASCII.GetBytes(parameters);

            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(url);
            objRequest.Method = "POST";
            objRequest.Timeout = (int)timeout * 1000;
            objRequest.UserAgent = userAgent;
            objRequest.ContentType = contentType;
            objRequest.ContentLength = data.Length;            

            if (additionalHeaders != null)
            {
                foreach (string s in additionalHeaders)
                {
                    objRequest.Headers.Add(s, additionalHeaders[s]);
                }
            }
            try
            {
                using (Stream myWriter = objRequest.GetRequestStream())
                {
                    myWriter.Write(data, 0, data.Length);
                }
            }
            catch (Exception e)
            {
                return (e.Message);
            }

            //Retrieve the Response returned from the NVP API call to PayPal
            HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();

            using (StreamReader sr = new StreamReader(objResponse.GetResponseStream()))
            {
                Result = sr.ReadToEnd();
            }

            return (Result);
        }

        /// <summary>
        /// Performs a post to a webpage
        /// </summary>
        /// <param name="url">url where data will be posted</param>
        /// <param name="parameters">Name value parameters</param>
        /// <param name="timeout">Timeout in seconds for the request</param>
        /// <param name="additionalHeaders">Additonal header values to be sent</param>
        /// <returns>text received from web page</returns>
        public static string Post(string url, NVPCodec parameters, uint timeout = 30, NVPCodec additionalHeaders = null)
        {
            return (Post(url, parameters.Encode(), timeout, additionalHeaders));
        }

        /// <summary>
        /// Performs a post to a webpage
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <param name="timeout"></param>
        /// <param name="userAgent"></param>
        /// <param name="additionalHeaders"></param>
        /// <returns></returns>
        public static string Post(string url, NVPCodec parameters, uint timeout, string userAgent,
            NVPCodec additionalHeaders = null)
        {
            return (Post(url, parameters.Encode(), timeout, additionalHeaders, userAgent));
        }

#if NET461
        /// <summary>
        /// Performs a post to a web page and redirects to the webpage
        /// </summary>
        /// <param name="response"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        public static void PostRedirect(HttpResponse response, string url, NVPCodec parameters)
        {
            response.Clear();

            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.AppendFormat(@"<body onload='document.forms[""form""].submit()'>");
            sb.AppendFormat("<form name='form' action='{0}' method='post'>", url);

            foreach (string s in parameters)
            {
                sb.AppendFormat("<input type='hidden' name='{0}' value='{1}'>", s, parameters[s]);
            }
            // Other params go here
            sb.Append("</form>");
            sb.Append("</body>");
            sb.Append("</html>");

            response.Write(sb.ToString());

            response.End();
        }
#endif
    }
}
