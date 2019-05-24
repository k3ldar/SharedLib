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

            builder.LoadDocuments(documents, "T:\\GitProjects\\.NetCorePluginManager\\Docs\\XmlFiles\\Modular.xml");
            builder.LoadDocuments(documents, "T:\\GitProjects\\.NetCorePluginManager\\Docs\\XmlFiles\\SharedPluginFeatures.xml");
            builder.LoadDocuments(documents, "T:\\GitProjects\\.NetCorePluginManager\\Docs\\XmlFiles\\BadEgg.Plugin.xml");
        }
    }
}
