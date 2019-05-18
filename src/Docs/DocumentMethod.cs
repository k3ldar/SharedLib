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
    public sealed class DocumentMethod
    {
        #region Constructors

        public DocumentMethod(in string assemblyName, in string namespaceName,
            in string className, in string methodName)
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

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ClassName { get; set; }

        public string MethodName { get; set; }

        public string Summary { get; set; }

        public string Returns { get; set; }

        public List<DocumentMethodParameter> Parameters { get; private set; }

        public List<DocumentMethodException> Exceptions { get; private set; }

        public bool IsConstructor { get; private set; }

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
