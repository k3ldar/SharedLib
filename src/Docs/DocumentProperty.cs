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
    /// <summary>
    /// Documentation for a property
    /// </summary>
    public sealed class DocumentProperty : BaseDocument
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assemblyName">Asembly name</param>
        /// <param name="namespaceName">Namespace where property belongs</param>
        /// <param name="className">Name of class</param>
        /// <param name="propertyName">Name of property</param>
        /// <param name="fullMemberName">Full member name as supplied by C# compiler</param>
        public DocumentProperty(in string assemblyName, in string namespaceName,
            in string className, in string propertyName, in string fullMemberName)
            : base(assemblyName, namespaceName, DocumentType.Property, fullMemberName)
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

        /// <summary>
        /// Name of class where property belongs
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Name of property
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Summary Description
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Value of property
        /// </summary>
        public string Value { get; set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess(in Document document)
        {


        }

        #endregion Internal Methods
    }
}
