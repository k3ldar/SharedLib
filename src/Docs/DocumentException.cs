/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Method Exception
 *
 */
using System;

namespace Shared.Docs
{
    public sealed class DocumentException
    {
        #region Constructors

        public DocumentException(in string exceptionName, in string summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            if (String.IsNullOrEmpty(exceptionName))
                throw new ArgumentNullException(nameof(exceptionName));

            ExceptionName = exceptionName;
            Summary = String.IsNullOrEmpty(summary) ? "Summary is missing" : summary;
        }

        #endregion Constructors

        #region Properties

        public string ExceptionName { get; set; }

        public string Summary { get; set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess(in Document document, in DocumentMethod method)
        {

        }

        #endregion Internal Methods
    }
}
