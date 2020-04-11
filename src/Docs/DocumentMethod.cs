/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Method
 *
 */
using System;
using System.Collections.Generic;

namespace Shared.Docs
{
    /// <summary>
    /// Class Method
    /// </summary>
    public sealed class DocumentMethod : BaseDocument
    {
        #region Constructors

        public DocumentMethod(in DocumentType documentType, in string assemblyName, in string namespaceName,
            in string className, in string methodName, in string fullMemberName)
            : base (assemblyName, namespaceName, documentType, fullMemberName)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            MethodName = methodName;
            IsConstructor = methodName.StartsWith("#ctor");
            Parameters = new List<DocumentMethodParameter>();
            Exceptions = new List<DocumentMethodException>();
            ExampleUseage = String.Empty;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of class
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Name of method
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Summary
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Return type
        /// </summary>
        public string Returns { get; set; }

        /// <summary>
        /// Parameters for method
        /// </summary>
        public List<DocumentMethodParameter> Parameters { get; private set; }

        /// <summary>
        /// Exceptions that can be raised within the method
        /// </summary>
        public List<DocumentMethodException> Exceptions { get; private set; }

        /// <summary>
        /// Determines if the method is a constructor
        /// </summary>
        public bool IsConstructor { get; private set; }

        /// <summary>
        /// Example useage for method
        /// </summary>
        public string ExampleUseage { get; set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess(Document document)
        {
            Parameters.ForEach(p => p.PostProcess(document, this));
            Exceptions.ForEach(e => e.PostProcess(document, this));

            MethodName = document.StandardReplace(MethodName);

            if (MethodName.Contains("("))
            {
                string paramNames = MethodName.Substring(MethodName.IndexOf("(") + 1).Replace(")", String.Empty);
                List<string> paramTypes = new List<string>(paramNames.Split(','));

                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (paramTypes[i].EndsWith("@"))
                        Parameters[i].IsByRef = true;

                    Parameters[i].Value = paramTypes[i].Replace("@", String.Empty);
                }
            }

            MethodName = document.PostReplace(MethodName);
        }

        #endregion Internal Methods
    }
}
