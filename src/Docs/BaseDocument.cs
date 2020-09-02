using System;
using System.Collections.Generic;

namespace Shared.Docs
{
    /// <summary>
    /// Base Documentation
    /// </summary>
    /// <remarks>
    /// This class is used as a descendent for other classes with basic information which is shared between all
    /// </remarks>
    public class BaseDocument
    {
        #region Constructors

        public BaseDocument(in string assemblyName, in string namespaceName,
            in DocumentType documentType, in string fullMemberName)
        {
            if (String.IsNullOrEmpty(fullMemberName))
                throw new ArgumentNullException(nameof(fullMemberName));

            DocumentType = documentType;
            AssemblyName = assemblyName ?? String.Empty;
            NameSpaceName = namespaceName ?? String.Empty;
            FullMemberName = fullMemberName;

            SeeAlso = new Dictionary<string, string>();
            Exception = new List<DocumentException>();
            Examples = new List<DocumentExample>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Type of document
        /// </summary>
        /// <value>DocumentType</value>
        public DocumentType DocumentType { get; protected set; }

        /// <summary>
        /// Name of assembly where the code is contained
        /// </summary>
        /// <value>string</value>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Name of the namespace where the code is contained
        /// </summary>
        /// <value>string</value>

        public string NameSpaceName { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        public string FullMemberName { get; private set; }

        /// <summary>
        /// Any remarkable information for class, method, property type etc
        /// </summary>
        public string Remarks { get; set; }


        public Dictionary<string, string> SeeAlso { get; private set; }

        public List<DocumentException> Exception { get; private set; }

        public List<DocumentExample> Examples { get; private set; }

        #endregion Properties
    }
}
