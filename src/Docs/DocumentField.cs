/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Field
 *
 */
using System;

namespace Shared.Docs
{
    public sealed class DocumentField
    {
        #region Constructors

        public DocumentField(in string assemblyName, in string namespaceName,
            in string className, in string fieldName)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException(nameof(fieldName));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            FieldName = fieldName;
        }

        #endregion Constructors

        #region Properties

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ClassName { get; set; }

        public string FieldName { get; set; }

        public string Summary { get; set; }

        public string Value { get; set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess(in Document document)
        {
            
        }

        #endregion Internal Methods
    }
}
