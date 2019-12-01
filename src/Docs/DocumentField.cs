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
    /// <summary>
    /// Represents a class Field
    /// </summary>
    public sealed class DocumentField : BaseDocument
    {
        #region Constructors

        public DocumentField(in string assemblyName, in string namespaceName,
            in string className, in string fieldName)
            : base (assemblyName, namespaceName, DocumentType.Field)
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

        /// <summary>
        /// Name of class where field belongs
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Name of field
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Summary description of field
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Value of field
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
