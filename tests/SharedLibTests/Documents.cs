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

            builder.LoadDocuments("T:\\GitProjects\\.NetCorePluginManager\\Docs\\SharedPluginFeatures.xml");
        }
    }
}
