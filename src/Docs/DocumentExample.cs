/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2020 Simon Carter
 *
 *  Purpose:  Documentation Example
 *
 */
using System;

namespace Shared.Docs
{
    public sealed class DocumentExample
    {
        #region Constructors

        public DocumentExample(in string text)
        {
            if (String.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            Text = text;
        }

        #endregion Constructors

        #region Properties

        public string Text { get; set; }

        #endregion Properties
    }
}
