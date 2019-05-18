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
using System.Text;

namespace Shared.Docs
{
    public sealed class Document
    {
        #region Constructors

        public Document()
        {
            Constructors = new List<DocumentMethod>();
            Methods = new List<DocumentMethod>();
            Properties = new List<DocumentProperty>();
            Fields = new List<DocumentField>();
            ExampleUseage = String.Empty;
            Title = String.Empty;

            SortOrder = 5000;
        }

        public Document(in string assemblyName, in string namespaceName, in string className)
            : this()
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

        public string Title { get; set; }

        public string AcquisitionMethod { get; set; }

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ClassName { get; set; }

        public string Summary { get; set; }

        public string Remarks { get; set; }

        public string Returns { get; set; }

        public string Value { get; set; }

        public string Example { get; set; }

        public List<DocumentMethod> Constructors { get; private set; }

        public List<DocumentMethod> Methods { get; private set; }

        public List<DocumentProperty> Properties { get; private set; }

        public List<DocumentField> Fields { get; private set; }

        public int SortOrder { get; set; }

        public string ExampleUseage { get; set; }

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
