/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2022 Simon Carter
 *
 *  Purpose:  Received email class
 *
 */

#if NET5

using System;

namespace Shared.Communication
{
    public class ContentTypeData
    {
        public ContentTypeData(string contentType, int index)
        {
            if (contentType.StartsWith("Content-Type:", StringComparison.InvariantCultureIgnoreCase))
                contentType = contentType[13..].Trim();

            ContentType = contentType;

            Index = index;

            IsMultiPart = contentType.StartsWith("multipart", StringComparison.InvariantCultureIgnoreCase);
            string[] parts = contentType.Split(new char[] { ';' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                Content = parts[0].Trim();

                if (Content.Equals("text/plain"))
                {
                    IsPlainText = true;
                    IsHtmlText = false;
                }
                else if (Content.Equals("text/html"))
                {
                    IsPlainText = false;
                    IsHtmlText = true;
                }

                if (parts.Length > 1)
                {
                    string value = parts[1].Trim().Replace("\"", "");

                    if (value.StartsWith("boundary", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Boundary = value[9..].Trim();
                    }
                    else if (value.StartsWith("charset", StringComparison.InvariantCultureIgnoreCase))
                    {
                        CharSet = value[8..].Trim();
                    }
                }
            }
        }

        public string ContentType { get; }

        public int Index { get; }

        public bool IsMultiPart { get; } = false;

        public string Content { get; }

        public string Boundary { get; }

        public string CharSet { get; }

        public bool IsHtmlText { get; } = false;

        public bool IsPlainText { get; } = false;

        public bool IsUtf8 => !String.IsNullOrEmpty(CharSet) && CharSet.Equals("utf-8", StringComparison.InvariantCultureIgnoreCase);

        public bool IsUtf7 => !String.IsNullOrEmpty(CharSet) && CharSet.Equals("utf-7", StringComparison.InvariantCultureIgnoreCase);

        public bool IsUtf16 => !String.IsNullOrEmpty(CharSet) && CharSet.Equals("utf-16", StringComparison.InvariantCultureIgnoreCase);

        public bool IsUtf32 => !String.IsNullOrEmpty(CharSet) && CharSet.Equals("utf-32", StringComparison.InvariantCultureIgnoreCase);

        public bool IsLatin => !String.IsNullOrEmpty(CharSet) && CharSet.Equals("iso-8859-1", StringComparison.InvariantCultureIgnoreCase);
    }
}

#endif