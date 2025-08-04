/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2019 Simon Carter
 *
 *  Purpose:  Documentation Builder
 *
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Shared.Docs
{
    /// <summary>
    /// DocumentBuilder is used to load a documentation xml file
    /// </summary>
    public sealed class DocumentBuilder
    {
        #region Public Methods

        public void LoadDocuments(in List<Document> currentDocuments, in string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(filename))
                throw new ArgumentException(nameof(filename));

            XmlDocument document = new XmlDocument();
            document.Load(filename);

            BuildDocumentList(currentDocuments, document);
        }

        #endregion Public Methods

        #region Private Methods

        private void BuildDocumentList(in List<Document> currentDocuments,
            XmlDocument document)
        {
            if (!document.HasChildNodes)
                throw new InvalidOperationException("Not a valid document file");

            XmlNodeList nodes = document.ChildNodes;

            XmlNode docNode = nodes.Item(1);
            XmlNode assemblyNode = docNode.FirstChild;
            string assemblyName = assemblyNode.InnerText;

            if (assemblyNode.NextSibling == null)
            {
                // custom document add a custom document 
                Document doc = new Document(DocumentType.Custom, nameof(DocumentType.Custom));
                doc.Title = assemblyNode.InnerText;
                doc.ShortDescription = doc.Title;

                currentDocuments.Add(doc);
            }
            else
            {
                XmlNodeList memberNodes = assemblyNode.NextSibling.ChildNodes;

                for (int i = 0; i < memberNodes.Count; i++)
                {
                    ProcessMember(currentDocuments, assemblyName, memberNodes.Item(i));
                }
            }

            currentDocuments.ForEach(r => r.PostProcess());
        }

        private void ProcessMember(in List<Document> documents,
            in string assemblyName, in XmlNode memberNode)
        {
            if (memberNode.Attributes.Count != 1)
                return;

            string memberNameParts = memberNode.Attributes[0].InnerText;

            string[] memberParts = memberNameParts.Split(':');

            if (memberParts.Length != 2)
                return;

            List<string> parts = new List<string>(memberParts[1].Split('.'));
            Document document = null;
            string namespaceName;
            string className;

            if (memberParts[0] == "T")
            {
                className = parts[parts.Count - 1];
                namespaceName = memberParts[1].Substring(0, memberParts[1].Length - (className.Length + 1));

                string ns = assemblyName;

                if (documents.Where(d => d.DocumentType == DocumentType.Assembly && d.AssemblyName == ns).FirstOrDefault() == null)
                {
                    documents.Add(new Document(assemblyName, memberNameParts));
                }

                document = new Document(DocumentType.Class, assemblyName, namespaceName, className, memberNameParts);
                documents.Add(document);

                if (memberNode.HasChildNodes)
                    ProcessChildNodes(document, memberNode);
            }
            else
            {
                document = GetMemberDocument(documents, memberParts[1],
                    out namespaceName, out className, out string memberName);

                if (document == null)
                    return;

                if (memberParts[0] == "M")
                {
                    DocumentMethod method = new DocumentMethod(
                        memberName.StartsWith("#ctor") ? DocumentType.Constructor : DocumentType.Method,
                        assemblyName, namespaceName, className, memberName, memberNameParts);

                    if (method.IsConstructor)
                        document.Constructors.Add(method);
                    else
                        document.Methods.Add(method);

                    if (memberNode.HasChildNodes)
                        ProcessMethodChildNodes(method, memberNode, assemblyName,
                            namespaceName, className, memberName, memberNameParts);
                }
                else if (memberParts[0] == "P")
                {
                    DocumentProperty property = new DocumentProperty(assemblyName,
                        namespaceName, className, memberName, memberNameParts);
                    document.Properties.Add(property);

                    if (memberNode.HasChildNodes)
                        ProcessPropertyChildNodes(property, memberNode, assemblyName,
                            namespaceName, className, memberName);
                }
                else if (memberParts[0] == "F")
                {
                    DocumentField field = new DocumentField(assemblyName,
                        namespaceName, className, memberName, memberNameParts);
                    document.Fields.Add(field);

                    if (memberNode.HasChildNodes)
                        ProcessFieldChildNodes(field, memberNode, assemblyName,
                            namespaceName, className, memberName);
                }
            }
        }

        private Document GetMemberDocument(in List<Document> documents,
            in string memberNameParts, out string namespaceName,
            out string className, out string memberName)
        {
            string mnParts = memberNameParts;
            string nameParams = String.Empty;

            if (mnParts.Contains('('))
            {
                nameParams = mnParts.Substring(mnParts.IndexOf("("));
                mnParts = mnParts.Substring(0, mnParts.Length - nameParams.Length);
            }

            List<string> parts = new List<string>(mnParts.Split('.'));
            memberName = parts[parts.Count - 1];
            className = parts[parts.Count - 2];

            namespaceName = mnParts.Substring(0,
                mnParts.Length - (memberName.Length + className.Length + 2));

            memberName += nameParams;
            string cn = className;
            string nsn = namespaceName;

            return documents.Where(d => d.ClassName == cn && d.NameSpaceName == nsn).FirstOrDefault();
        }

        private void ProcessMethodChildNodes(in DocumentMethod method, in XmlNode node,
            in string assemblyName, in string namespaceName, in string className,
            in string memberName, in string fullMemberName)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode childNode = node.ChildNodes.Item(i);

                if (childNode.Name == "summary")
                {
                    method.Summary = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "returns")
                {
                    method.Returns = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "param")
                {
                    string paramName = String.Empty;

                    if (childNode.Attributes.Count > 0)
                        paramName = childNode.Attributes.GetNamedItem("name").InnerText;

                    DocumentMethodParameter param = new DocumentMethodParameter(assemblyName,
                        namespaceName, className, memberName, paramName, fullMemberName);
                    method.Parameters.Add(param);
                    param.Summary = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "exception")
                {
                    if (childNode.Attributes.Count > 0)
                    {
                        method.Exception.Add(new DocumentException(childNode.Attributes.GetNamedItem("cref").InnerText, childNode.InnerXml));
                    }
                    else
                    {
                        method.Exception.Add(new DocumentException("missing exception cref", childNode.InnerXml));
                    }

                }
                else if (childNode.Name == "example")
                {
                    method.Examples.Add(new DocumentExample(childNode.InnerXml));
                }
                else if (childNode.Name == "remarks")
                {
                    method.Remarks = childNode.InnerXml;
                }
            }
        }

        private void ProcessPropertyChildNodes(in DocumentProperty property, in XmlNode node,
            in string assemblyName, in string namespaceName, in string className,
            in string memberName)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode childNode = node.ChildNodes.Item(i);

                if (childNode.Name == "summary")
                {
                    property.Summary = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "value")
                {
                    property.Value = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "exception")
                {
                    property.Exception.Add(new DocumentException(childNode.Attributes.GetNamedItem("cref").InnerText, childNode.InnerXml));
                }
                else if (childNode.Name == "example")
                {
                    property.Examples.Add(new DocumentExample(childNode.InnerXml));
                }
                else if (childNode.Name == "remarks")
                {
                    property.Remarks = childNode.InnerXml;
                }
            }
        }

        private void ProcessFieldChildNodes(in DocumentField field, in XmlNode node,
            in string assemblyName, in string namespaceName, in string className,
            in string memberName)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode childNode = node.ChildNodes.Item(i);

                if (childNode.Name == "summary")
                {
                    field.Summary = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "value")
                {
                    field.Value = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "example")
                {
                    field.Examples.Add(new DocumentExample(childNode.InnerXml));
                }
                else if (childNode.Name == "remarks")
                {
                    field.Remarks = childNode.InnerXml;
                }
            }
        }

        private void ProcessChildNodes(in Document document, in XmlNode node)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode childNode = node.ChildNodes.Item(i);

                if (childNode.Name == "summary")
                {
                    document.Summary = childNode.InnerXml.Trim();
                }
                else if (childNode.Name == "example")
                {
                    document.Examples.Add(new DocumentExample(childNode.InnerXml));
                }
                else if (childNode.Name == "remarks")
                {
                    document.Remarks = childNode.InnerXml;
                }
            }
        }

        #endregion Private Methods
    }
}
