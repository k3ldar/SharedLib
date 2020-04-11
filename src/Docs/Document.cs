/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Static File Download class
 *
 */
using System;
using System.Collections.Generic;

namespace Shared.Docs
{
    /// <summary>
    /// Contains documentation for a class/type or custom documentation
    /// </summary>
    public sealed class Document : BaseDocument
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// 
        /// Other document types (Custom etc)
        /// </summary>
        /// <param name="documentType">DocumentType</param>
        /// <param name="fullMemberName">Full member name of assembly</param>
        public Document(DocumentType documentType, in string fullMemberName)
            : base(null, null, documentType, fullMemberName)
        {
            Constructors = new List<DocumentMethod>();
            Methods = new List<DocumentMethod>();
            Properties = new List<DocumentProperty>();
            Fields = new List<DocumentField>();
            ExampleUseage = String.Empty;
            Title = String.Empty;

            SortOrder = 5000;
        }

        /// <summary>
        /// Constructor
        /// 
        /// Used when documenting an assembly
        /// </summary>
        /// <param name="assemblyName">Name of assembly</param>
        /// <param name="fullMemberName">Full member name of assembly</param>
        public Document(in string assemblyName, in string fullMemberName)
            : base(assemblyName, null, DocumentType.Assembly, fullMemberName)
        {
            Constructors = new List<DocumentMethod>();
            Methods = new List<DocumentMethod>();
            Properties = new List<DocumentProperty>();
            Fields = new List<DocumentField>();
            ExampleUseage = String.Empty;
            Title = String.Empty;

            SortOrder = 5000;
        }

        /// <summary>
        /// Constructor
        /// 
        /// Used when documenting a class/type
        /// </summary>
        /// <param name="documentType">DocumentType</param>
        /// <param name="assemblyName">Name of assembly containing the class</param>
        /// <param name="namespaceName">Namespace where class can be found</param>
        /// <param name="className">Name of class</param>
        /// <param name="fullMemberName">Full member name of assembly</param>
        public Document(in DocumentType documentType, in string assemblyName, 
            in string namespaceName, in string className, in string fullMemberName)
            : this(documentType, fullMemberName)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            Title = className;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Document Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// How class can be acquired (DI etc)
        /// </summary>
        public string AcquisitionMethod { get; set; }

        /// <summary>
        /// Name of class
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Summary for class
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Any remarkable information for class
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Return Value if required
        /// </summary>
        public string Returns { get; set; }

        /// <summary>
        /// Value if required
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Example 
        /// </summary>
        public string Example { get; set; }

        /// <summary>
        /// Documented Construcors
        /// </summary>
        public List<DocumentMethod> Constructors { get; private set; }

        /// <summary>
        /// Documented Methods
        /// </summary>
        public List<DocumentMethod> Methods { get; private set; }

        /// <summary>
        /// Documented Properties
        /// </summary>
        public List<DocumentProperty> Properties { get; private set; }

        /// <summary>
        /// Documented Fields
        /// </summary>
        public List<DocumentField> Fields { get; private set; }

        /// <summary>
        /// Sort order to be used for this item
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Text for example useage
        /// </summary>
        public string ExampleUseage { get; set; }

        /// <summary>
        /// Custom object for use by implementing class to hold any data.
        /// </summary>
        public object Tag { get; set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess()
        {
            Constructors.ForEach(c => c.PostProcess(this));
            Methods.ForEach(m => m.PostProcess(this));
            Properties.ForEach(p => p.PostProcess(this));
        }

        internal string StandardReplace(string s)
        {
            return s.Replace("System.", String.Empty);
        }

        internal string PostReplace(string s)
        {
            return s.Replace("@,", ",").Replace("@)", ")");
        }

        #endregion Internal Methods
    }
}
