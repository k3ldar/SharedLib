/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Method Parameter
 *
 */
using System;

namespace Shared.Docs
{
    public sealed class DocumentMethodParameter
    {
        #region Constructors

        public DocumentMethodParameter(in string assemblyName, in string namespaceName,
            in string className, in string methodName, in string parameterName)
        {
            if (String.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException(nameof(assemblyName));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));

            if (String.IsNullOrEmpty(parameterName))
                throw new ArgumentNullException(nameof(parameterName));

            AssemblyName = assemblyName;
            NameSpaceName = namespaceName;
            ClassName = className;
            MethodName = methodName;
            ParameterName = parameterName;
        }

        #endregion Constructors

        #region Properties

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ClassName { get; set; }

        public string MethodName { get; set; }

        public string ParameterName { get; set; }

        public string Summary { get; set; }

        public string Value { get; set; }

        public bool IsByRef { get; internal set; }

        #endregion Properties

        #region Internal Methods

        internal void PostProcess(in Document document, in DocumentMethod method)
        {
            MethodName = document.StandardReplace(MethodName);

            MethodName = document.PostReplace(MethodName);
        }

        #endregion Internal Methods
    }
}
