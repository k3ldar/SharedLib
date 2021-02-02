using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Docs;

namespace SharedLibTests
{
    [TestClass]
    public class DocumentTests
    {
        [TestMethod]
        public void CreateDocumentBuilder()
        {
            DocumentBuilder builder = new DocumentBuilder();
        }

        [TestMethod]
        public void LoadXmlFile()
        {
            DocumentBuilder builder = new DocumentBuilder();
            List<Document> documents = new List<Document>();

            string[] files = System.IO.Directory.GetFiles("c:\\GitProjects\\.NetCorePluginManager\\Docs\\XmlFiles\\", "*.xml");

            foreach (string file in files)
            {
                builder.LoadDocuments(documents, file);
            }
        }
    }
}
