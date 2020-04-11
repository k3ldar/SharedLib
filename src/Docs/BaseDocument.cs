using System;

namespace Shared.Docs
{
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
        }

        #endregion Constructors

        #region Properties

        public DocumentType DocumentType { get; protected set; }

        public string AssemblyName { get; set; }

        public string NameSpaceName { get; set; }

        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        public string FullMemberName { get; private set; }

        #endregion Properties
    }
}
