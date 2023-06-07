using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CreateConsignment
{
    public partial class CreateConsignment : ServiceBase
    {
        static ConcurrentQueue<logger> cq = new ConcurrentQueue<logger>();
        public CreateConsignment()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread loggingThread = null, CreateConsignmentThread = null;
            loggingThread = new Thread(() =>
            {
                RunLoggerThread();
            });
            loggingThread.Start();
            string StoragePath = ConfigurationManager.AppSettings.Get("StoragePath");
            string BackupPath = ConfigurationManager.AppSettings.Get("BackupPath");
            int Interval = int.Parse(ConfigurationManager.AppSettings.Get("Interval"));
            string Username = ConfigurationManager.AppSettings.Get("Username");
            string clientIdentCode = ConfigurationManager.AppSettings.Get("clientIdentCode");
            string clientSystemId = ConfigurationManager.AppSettings.Get("clientSystemId");
            string url = ConfigurationManager.AppSettings.Get("url");
            string password = ConfigurationManager.AppSettings.Get("password");
            string FinishStatus = ConfigurationManager.AppSettings.Get("FinishStatus");
            string attachmentFolder = ConfigurationManager.AppSettings.Get("AttachmentsFolderPath");
            CreateConsignmentThread = new Thread(() =>
            {
                CallCreateConsignment(StoragePath, BackupPath, Interval, Username, clientIdentCode, clientSystemId, url, password, FinishStatus, attachmentFolder);
            });
            CreateConsignmentThread.Start();
        }

        private void CallCreateConsignment(string storagePath, string backupPath, int interval, string username, string clientIdentCode, string clientSystemId, string url, string password, string finishStatus, string attachmentFolder)
        {
            while (true)
            {
                try
                {

                    DBManager dBManager = new DBManager();
                    var orderInstructions = dBManager.GetConsignmentInstructions();
                    if (orderInstructions.Count > 0)
                    {
                        log("OrderInstructions fetched from Commercials:" + orderInstructions.Count);
                        foreach (var orderInstruction in orderInstructions)
                        {
                            dBManager.UpdateCommericalTable(orderInstruction.localReference, "XML");
                            log("Processing order with local Reference:" + orderInstruction.localReference);
                            log("Username:" + username + ",clientIdentCode:" + clientIdentCode + ",password:" + password + ",clientSystemId:" + clientSystemId + ",_url:" + url);
                            #region generate XML
                            log("Generating XML for local Reference:" + orderInstruction.localReference);
                            XmlDocument soapEnvelopeXml = CreateSoapEnvelopeForCreateConsignment(orderInstruction, storagePath, clientSystemId, clientIdentCode, attachmentFolder);
                            #endregion


                            //make web reqeust for the api call and set its params
                            HttpWebRequest webRequest = CreateWebRequest(url);
                            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                            webRequest.KeepAlive = true;

                            // Authentication of password and username using basic http
                            string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(username + "@" + clientIdentCode + ":" + password));
                            webRequest.Headers.Add("Authorization", auth);

                            //get response
                            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                            asyncResult.AsyncWaitHandle.WaitOne(3000);

                            string soapResult;
                            XmlDocument docResult = new XmlDocument();

                            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                            {
                                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                                {
                                    soapResult = rd.ReadToEnd();
                                    log("result:" + soapResult);
                                    // load response to xml document
                                    docResult.LoadXml(soapResult);

                                    var checkError = docResult.GetElementsByTagName("hasErrors");
                                    // check if response has errors
                                    if (checkError[0].InnerText == "false")
                                    {

                                        XmlNodeList nodes = docResult.GetElementsByTagName("businessObjectId");
                                        foreach (XmlNode node in nodes)
                                        {
                                            dBManager.UpdateCommericalTableBOIDnr(orderInstruction.localReference, node.InnerText);
                                        }

                                        log("[AddBrokerInstruction] No error found. Going to update status " + finishStatus + " in db.");
                                        dBManager.UpdateCommericalTable(orderInstruction.localReference, finishStatus);
                                        //dBManager.InsertInLogTable(orderInstructions.localReference, status, "status changed from " + orderInstructions.identCode + " to " + status + ".", "AEBCustomsAPI");


                                    }
                                    else
                                    {
                                        XmlNodeList nodes = docResult.GetElementsByTagName("messageType");
                                        foreach (XmlNode node in nodes)
                                        {


                                            if (node.InnerText == "ERROR")
                                            {
                                                log("[AddBrokerInstruction] Error found. Going to update error failed in db.");
                                                dBManager.UpdateCommericalTable(orderInstruction.localReference, "ERR");
                                                XmlNode ErrorNode = node.NextSibling;
                                                dBManager.InsertInLogTable(orderInstruction.localReference, "ERR", "status changed from " + orderInstruction.status + " to ERR. Error Recieved: " + ErrorNode.InnerText + ". Please check log file for more details.", "AEBCustomsAPI");
                                            }
                                        }
                                    }
                                }
                            }

                            backupPath = ConfigurationManager.AppSettings.Get("BackupPath") + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.Month + "\\" + DateTime.Now.Day;
                            CreateDirectory(backupPath);
                            MoveFile(storagePath + "\\" + orderInstruction.consignmentNr + ".xml", backupPath);
                            Thread.Sleep(1000);

                        }

                    }
                    else
                    {
                        log("[AddBrokerInstruction] No record found in DB.");
                        int minutes = 1000 * 60 * interval;
                        Thread.Sleep(minutes);
                    }

                }
                catch (Exception ex)
                {
                    log(ex.Message);
                    log(ex.StackTrace);
                    int minutes = 1000 * 60 * interval;
                    Thread.Sleep(minutes);
                }
            }
        }
        private void MoveFile(string file, string backupPath)
        {
            string fileName = Path.GetFileName(file);
            File.Move(file, backupPath + "\\" + fileName);
        }

        private XmlDocument CreateSoapEnvelopeForCreateConsignment(OrderInstructions orderInstruction, string storagePath, string clientSystemId, string clientIdentCode, string attachmentFolder)
        {
            CreateConsignmentDocument createConsignmentDocument = new CreateConsignmentDocument();

            createConsignmentDocument.SetXMLNodeValue(clientIdentCode, createConsignmentDocument.xmlDocument, "clientIdentCode");
            createConsignmentDocument.SetXMLNodeValue(clientSystemId, createConsignmentDocument.xmlDocument, "clientSystemId");
            DBManager dBManager = new DBManager();
            Queue<XMLMapping> xMLMappings = dBManager.GetXmlMapping(orderInstruction);
            if (xMLMappings.Count > 0)
            {
                while (xMLMappings.Count != 0)
                {
                    XMLMapping xMLMapping = xMLMappings.Dequeue();
                    if (xMLMapping.Value != "")
                    {

                        XmlNodeList xmlParentNodes = createConsignmentDocument.xmlDocument.GetElementsByTagName(xMLMapping.ParentNode);
                        foreach (XmlNode parentNode in xmlParentNodes)
                        {
                            if (xMLMapping.Value.ToLower() == "datetime.now")
                            {
                                createConsignmentDocument.CreateElementWithInnerText(parentNode, xMLMapping.NodeName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            else if (xMLMapping.Value.ToLower() == "att")
                            {
                                string[] files = Directory.GetFiles(attachmentFolder + "\\" + orderInstruction.localReference);
                                int i = 1;
                                foreach (var file in files)
                                {
                                    XmlNode attNode = createConsignmentDocument.CreateElement(parentNode, xMLMapping.NodeName);
                                    byte[] bytes = File.ReadAllBytes(file);
                                    string EncodedString = Convert.ToBase64String(bytes);
                                    createConsignmentDocument.CreateElementWithInnerText(attNode, "attachmentCode", Path.GetFileName(file));
                                    createConsignmentDocument.CreateElementWithInnerText(attNode, "data", EncodedString);
                                    createConsignmentDocument.CreateElementWithInnerText(attNode, "description", "Attachment "+ i);
                                    i++;
                                }
                                
                            }
                            else
                                createConsignmentDocument.CreateElementWithInnerText(parentNode, xMLMapping.NodeName, xMLMapping.Value);
                        }
                    }
                    else if (xMLMapping.Query != "")
                    {
                        XmlNodeList xmlParentNodes = createConsignmentDocument.xmlDocument.GetElementsByTagName(xMLMapping.ParentNode);
                        foreach (XmlNode parentNode in xmlParentNodes)
                        {
                            string value = dBManager.getValue(xMLMapping.Query, orderInstruction.localReference);
                            createConsignmentDocument.CreateElementWithInnerText(parentNode, xMLMapping.NodeName, value);
                        }
                    }
                    else if (xMLMapping.ChildNodes != "")
                    {
                        XmlNodeList xmlParentNodes = createConsignmentDocument.xmlDocument.GetElementsByTagName(xMLMapping.ParentNode);

                        foreach (XmlNode parentNode in xmlParentNodes)
                        {
                            List<Dictionary<string, string>> childNodesList;
                            string[] nodeName = CheckParentNodes(parentNode);
                            switch (nodeName[0])
                            {
                                case "parties":
                                    childNodesList = dBManager.getChildNodesParties(xMLMapping.ChildNodes, orderInstruction.localReference, nodeName[1]);
                                    break;
                                case "items":
                                    childNodesList = dBManager.getChildNodesItems(xMLMapping.ChildNodes, orderInstruction.localReference, nodeName[1]);
                                    break;

                                default:
                                    childNodesList = dBManager.getChildNodes(xMLMapping.ChildNodes, orderInstruction.localReference);
                                    break;
                            }
                            foreach (var childNodesdict in childNodesList)
                            {

                                XmlNode currentNode = createConsignmentDocument.CreateElement(parentNode, xMLMapping.NodeName);
                                createConsignmentDocument.CreateChildElements(childNodesdict, currentNode);
                            }
                        }


                    }
                    else
                    {
                        XmlNodeList xmlParentNodes = createConsignmentDocument.xmlDocument.GetElementsByTagName(xMLMapping.ParentNode);
                        foreach (XmlNode parentNode in xmlParentNodes)
                        {
                            createConsignmentDocument.CreateElement(parentNode, xMLMapping.NodeName);
                        }
                    }
                }
            }
            else
            {
                log("No xml mapping found in DB for process Code:" + orderInstruction.processCode);
            }
            createConsignmentDocument.xmlDocument.Save(storagePath + "\\" + orderInstruction.consignmentNr + ".xml");
            return createConsignmentDocument.xmlDocument;
        }

        private string[] CheckParentNodes(XmlNode parentNode)
        {
            if (parentNode.ParentNode == null)
            {
                return new string[] { "" };
            }
            else if (parentNode.Name == "parties")
            {
                return new string[] { "parties", parentNode.SelectSingleNode("partyType").InnerText };
            }
            else if (parentNode.Name == "items")
            {
                return new string[] { "items", parentNode.SelectSingleNode("itemIdClientSystem").InnerText };
            }
            else if (parentNode.Name == "packages")
            {
                return new string[] { "items", parentNode.SelectSingleNode("packageIdClientSystem").InnerText };
            }
            return CheckParentNodes(parentNode.ParentNode);
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        private static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.ContentType = "text/xml;charset=UTF-8";
            webRequest.Accept = "*/*";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static void RunLoggerThread()
        {
            while (true)
            {
                try
                {
                    logger myLogger = null;
                    if (cq.TryDequeue(out myLogger))
                    {
                        using (System.IO.StreamWriter file =
                                       new System.IO.StreamWriter(myLogger.path + "\\AEBCustomsAPILog" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true))
                        {
                            file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " AEBCustomsAPILog: [CreateConsignment] " + myLogger.data);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                    using (System.IO.StreamWriter file =
                                     new System.IO.StreamWriter(ConfigurationManager.AppSettings.Get("logpath") + "\\AEBCustomsAPILog" + DateTime.Now.ToString("yyyyMMdd") + ".txt", true))
                    {
                        file.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " AEBCustomsAPILog: [CreateConsignment] " + ex.Message);
                    }

                }
            }
        }
        protected override void OnStop()
        {
        }
        public static void log(string data)
        {
            var logPath = ConfigurationManager.AppSettings.Get("logpath");
            CreateDirectory(logPath);
            cq.Enqueue(new logger(logPath, data));

        }
        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        internal void Start()
        {
            this.OnStart(null);
        }
    }
}
