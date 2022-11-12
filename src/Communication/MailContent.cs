/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Pop 3 client Class
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if NET5

namespace Shared.Communication
{
    public class MailContent
    {
        public MailContent(StringBuilder content)
        {
            RawContent = content.ToString();
            Headers = new();
            ParseEmailData(true);
        }

        public MailContent(StringBuilder content, ContentTypeData contentType)
        {
            RawContent = content.ToString();

            Headers = new()
            {
                contentType
            };

            ParseEmailData(false);
        }

        public string RawContent { get; }

        public string Message { get; private set; }

        public List<ContentTypeData> Headers { get; }


        private void ParseEmailData(bool containsHeaders)
        {
            string[] lines = RawContent.Split(new char[] { '\n' }, StringSplitOptions.None);
            bool isHeaders = containsHeaders;
            StringBuilder email = new();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    isHeaders = false;
                }

                if (isHeaders)
                {
                    Headers.Add(new ContentTypeData(line.Trim(), i));
                }
                else
                {

                    email.AppendLine(line);
                }
            }

            if (Headers.Any(h => h.IsUtf8))
            {
                Message = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(email.ToString().Trim()));
            }
            else if (Headers.Any(h => h.IsLatin))
            {
                Message = Encoding.Latin1.GetString(Encoding.Latin1.GetBytes(email.ToString().Trim()));
            }
            else
            {
                Message = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(email.ToString().Trim()));
            }
        }
    }
}

#endif