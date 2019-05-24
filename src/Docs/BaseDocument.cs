using System;

namespace Shared.Docs
{
    public class BaseDocument
    {
        #region Constructors

        public BaseDocument(in string assemblyName, in string namespaceName, in DocumentType documentType)
        {
            DocumentType = documentType;
            AssemblyName = assemblyName ?? String.Empty;
            NameSpaceName = namespaceName ?? String.Empty;
        }

        #endregion Constructors

        #region Properties

        public DocumentType DocumentType { get; protected set; }

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        #endregion Properties
    }
}
