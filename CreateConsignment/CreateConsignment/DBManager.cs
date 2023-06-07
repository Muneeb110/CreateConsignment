using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateConsignment
{
    class DBManager
    {
        public List<OrderInstructions> GetConsignmentInstructions()
        {
            List<OrderInstructions> orderInstructions = new List<OrderInstructions>();
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();


                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = "SELECT localReference,status,processCode,consignmentNr FROM [dbo].[commercial] where status = 'OK';"; //change to OK
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            OrderInstructions orderInstruction = new OrderInstructions();
                            orderInstruction.localReference = oReader["localReference"].ToString();
                            orderInstruction.status = oReader["status"].ToString();
                            orderInstruction.processCode = oReader["processCode"].ToString();
                            orderInstruction.consignmentNr = oReader["consignmentNr"].ToString();
                            orderInstructions.Add(orderInstruction);
                        }

                        myConnection.Close();
                    }
                }
                return orderInstructions;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public void UpdateCommericalTable(string localReference, string status)
        {
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    SqlCommand SqlComm = new SqlCommand("update commercial set status = @status where localReference = @localReference", myConnection);
                    SqlComm.Parameters.AddWithValue("@localReference", localReference);
                    SqlComm.Parameters.AddWithValue("@status", status);

                    myConnection.Open();
                    int i = SqlComm.ExecuteNonQuery();

                    myConnection.Close();
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public void InsertInLogTable(string localReference, string status, string message, string table)
        {
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    SqlCommand SqlComm = new SqlCommand("insert into dbo.History (dateTime, [key], [table], status, message)values(CURRENT_TIMESTAMP, @key,@table,@status,@message)", myConnection);
                    SqlComm.Parameters.AddWithValue("@key", localReference);
                    SqlComm.Parameters.AddWithValue("@table", table);
                    SqlComm.Parameters.AddWithValue("@status", status);
                    SqlComm.Parameters.AddWithValue("@message", message);

                    myConnection.Open();
                    SqlComm.ExecuteNonQuery();

                    myConnection.Close();
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }


        public Queue<XMLMapping> GetXmlMapping(OrderInstructions orderInstruction)
        {
            Queue<XMLMapping> xMLMappings = new Queue<XMLMapping>();
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();


                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = "SELECT * FROM [dbo].[stam_xmlMapping] where ProcessCode = @processCode Order by Step ASC;";
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@processCode", orderInstruction.processCode);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            XMLMapping xMLMapping = new XMLMapping();
                            xMLMapping.step = int.Parse(oReader["step"].ToString());
                            xMLMapping.ProcessCode = oReader["ProcessCode"].ToString();
                            xMLMapping.NodeName = oReader["NodeName"].ToString();
                            xMLMapping.Value = oReader["Value"].ToString();
                            xMLMapping.Query = oReader["Query"].ToString();
                            xMLMapping.ParentNode = oReader["ParentNode"].ToString();
                            xMLMapping.ChildNodes = oReader["ChildNodes"].ToString();
                            xMLMappings.Enqueue(xMLMapping);
                        }

                        myConnection.Close();
                    }
                }
                return xMLMappings;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        internal List<Dictionary<string, string>> getChildNodes(string query, string localReference)
        {
            List<Dictionary<string, string>> xMLMappings = new List<Dictionary<string, string>>();
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                CreateConsignment.log("Running Query:" + query.Replace("@localReference", localReference));

                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = query;
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@localReference", localReference);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {

                        while (oReader.Read())
                        {
                            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

                            for (int i = 0; i < oReader.FieldCount; i++)
                            {
                                string test = oReader[i].ToString();
                                keyValuePairs[oReader.GetName(i)] = oReader[i].ToString();
                            }
                            xMLMappings.Add(keyValuePairs);
                        }

                        myConnection.Close();
                    }
                }
                return xMLMappings;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public void UpdateCommericalTableBOIDnr(string localReference, string BOIDnr)
        {
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    SqlCommand SqlComm = new SqlCommand("update commercial set BOIDnr = @BOIDnr where localReference = @localReference", myConnection);
                    SqlComm.Parameters.AddWithValue("@localReference", localReference);
                    SqlComm.Parameters.AddWithValue("@BOIDnr", BOIDnr);

                    myConnection.Open();
                    int i = SqlComm.ExecuteNonQuery();

                    myConnection.Close();
                }

            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public List<Dictionary<string, string>> getChildNodesParties(string query, string localReference, string partyType)
        {
            List<Dictionary<string, string>> xMLMappings = new List<Dictionary<string, string>>();
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                CreateConsignment.log("Running Query:" + query.Replace("@localReference", localReference).Replace("@partyType", partyType));

                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = query;
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@localReference", localReference);
                    oCmd.Parameters.AddWithValue("@partyType", partyType);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {

                        while (oReader.Read())
                        {
                            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

                            for (int i = 0; i < oReader.FieldCount; i++)
                            {
                                string test = oReader[i].ToString();
                                keyValuePairs[oReader.GetName(i)] = oReader[i].ToString();
                            }
                            xMLMappings.Add(keyValuePairs);
                        }

                        myConnection.Close();
                    }
                }
                return xMLMappings;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public List<Dictionary<string, string>> getChildNodesItems(string query, string localReference, string itemsId)
        {
            List<Dictionary<string, string>> xMLMappings = new List<Dictionary<string, string>>();
            try
            {
                CreateConsignment.log("Running Query:" + query.Replace("@localReference", localReference).Replace("@itemsID", itemsId));
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();


                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = query;
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@localReference", localReference);
                    oCmd.Parameters.AddWithValue("@itemsID", itemsId);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {

                        while (oReader.Read())
                        {
                            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

                            for (int i = 0; i < oReader.FieldCount; i++)
                            {
                                string test = oReader[i].ToString();
                                keyValuePairs[oReader.GetName(i)] = oReader[i].ToString();
                            }
                            xMLMappings.Add(keyValuePairs);
                        }

                        myConnection.Close();
                    }
                }
                return xMLMappings;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        internal string getValue(string query, string localReference)
        {
            string value = "";
            try
            {
                var con = ConfigurationManager.AppSettings["dbConnectionString"].ToString();
                CreateConsignment.log("Running Query:" + query.Replace("@localReference", localReference));

                using (SqlConnection myConnection = new SqlConnection(con))
                {
                    string oString = query;
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@localReference", localReference);
                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {

                        while (oReader.Read())
                        {

                            value = oReader[0].ToString();
                        }

                        myConnection.Close();
                    }
                }
                return value;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
    }


    public class OrderInstructions
    {
        public string processCode { get; set; }
        public string consignmentNr { get; set; }
        public string status { get; set; }
        public string localReference { get; set; }

    }

    public class XMLMapping
    {
        public int step { get; set; }
        public string ProcessCode { get; set; }
        public string NodeName { get; set; }
        public string Value { get; set; }
        public string Query { get; set; }
        public string ParentNode { get; set; }
        public string ChildNodes { get; set; }
    }
}
