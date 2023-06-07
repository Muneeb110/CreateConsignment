using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CreateConsignment
{
    class CreateConsignmentDocument
    {
        public XmlDocument xmlDocument = new XmlDocument();
        public CreateConsignmentDocument()
        {
            xmlDocument.Load(ConfigurationManager.AppSettings.Get("ResourceFolderPath") + "/Resources/CreateConsignmentXML.xml");
        }
        public void SetXMLNodeValue(string clientIdentCode, XmlDocument xmlDocument, string tagName)
        {
            XmlNodeList xmlNodes = xmlDocument.GetElementsByTagName(tagName);
            foreach (XmlNode xmlNode in xmlNodes)
            {
                xmlNode.InnerText = clientIdentCode;
            }
        }

        public XmlNode CreateElement(XmlNode parentNode, string nodeName)
        {
            XmlElement xElement = xmlDocument.CreateElement(nodeName);
            xElement.InnerText = "";
            parentNode.AppendChild(xElement);
            return xElement;

        }
        public void CreateElementWithInnerText(XmlNode parentNode, string nodeName, string innerText)
        {
            XmlElement xElement = xmlDocument.CreateElement(nodeName);
            xElement.InnerText = innerText;
            parentNode.AppendChild(xElement);

        }
        public void CreateChildElements(Dictionary<string, string> childNodesdict, XmlNode parentNode)
        {
            foreach (var NodeAndValues in childNodesdict)
            {
                CreateElementWithInnerText(parentNode, NodeAndValues.Key, NodeAndValues.Value);
            }
        }
    }
}
