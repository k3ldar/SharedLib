/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Property
 *
 */
using System;

namespace Shared.Docs
{
    public sealed class DocumentProperty
    {
        #region Constructors

        public DocumentProperty(in string assemblyName, in string namespaceName,
            in string className, in string propertyName)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            PropertyName = propertyName;
        }

        #endregion Constructors

        #region Properties

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ClassName { get; set; }

        public string PropertyName { get; set; }

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
