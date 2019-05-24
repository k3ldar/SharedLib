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
    public sealed class DocumentMethodException : BaseDocument
    {
        #region Constructors

        public DocumentMethodException(in string assemblyName, in string namespaceName,
            in string className, in string exceptionName)
            : base (assemblyName, namespaceName, DocumentType.Exception)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(exceptionName))
                throw new ArgumentNullException(nameof(exceptionName));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            ExceptionName = exceptionName;
        }

        #endregion Constructors

        #region Properties

        public string ClassName { get; set; }

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
