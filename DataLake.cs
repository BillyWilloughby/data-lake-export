using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Data_Lake_Export.DLExport
{
    public class DataLake
    {
        public static async Task<DataTable> RunDataLakeAsync(string sqlFile, IONAPI ionAPI)
        {
            string responseBody;
            string Status;
            dynamic columnTypes;
            bool quickSave;
            dynamic dyn;
            string SQL = "";

            Boolean.TryParse(Environment.GetEnvironmentVariable("DL_QuickDataTable"), out quickSave);

            if (quickSave && File.Exists("DL_QuickDataTable.tmp"))
            {
                try
                {
                    responseBody = File.ReadAllText("DL_QuickDataTable.tmp");
                    columnTypes = File.ReadAllText("DL_QuickDataColumns.tmp");
                    SQL = File.ReadAllText("DL_QuickSQL.tmp", Encoding.UTF8);

                    if (SQL.Equals(File.ReadAllText(sqlFile, Encoding.UTF8)))
                    {

                        dyn = JsonConvert.DeserializeObject(columnTypes);
                        columnTypes = dyn.columns;
                        Status = "Quick";
                        goto QuickDataLoad;
                    }
                }
                catch (Exception ex)
                { //Do nothing if lookup failes
                }
            }

            string bearerToken = ionAPI.getBearerToken();

            Console.WriteLine("Loading Compass SQL File : " + sqlFile);


            if (File.Exists(sqlFile))
                SQL = File.ReadAllText(sqlFile, Encoding.UTF8);
            else
            {
                Console.WriteLine("Compass Query file not found " + sqlFile);
                Environment.Exit(-22);
            }
            if (quickSave)
                File.WriteAllText("DL_QuickSQL.tmp", SQL, Encoding.UTF8);

            Console.WriteLine("Calling Compass API...");

            Task<HttpResponseMessage> query =
                Program._compassURL
                    .AppendPathSegment("/v1/compass/jobs")
                    .SetQueryParam("resultFormat", "application/x-ndjson")
                    .WithOAuthBearerToken(bearerToken)
                    .WithHeader("User-Agent", "DataMover")
                    .WithHeader("Accept-Encoding", "deflate")
                    .WithHeader("Cache-Control", "no-cache")
                    .AllowAnyHttpStatus()
                    .SendStringAsync(HttpMethod.Post, SQL);


            if (!query.Result.IsSuccessStatusCode)
            {
                Console.WriteLine("Compass API: " + query.Result.ReasonPhrase);
                return null;
            }

            responseBody = await query.ReceiveString();

            dyn = JsonConvert.DeserializeObject(responseBody);

            Status = dyn.status;
            string Location = dyn.location;
            string queryID = dyn.queryId;

            Console.Write("Compass API: " + dyn.status);

            query =
                Program._compassURL
                    .AppendPathSegment("/v1/compass/jobs/" + queryID + "/status", false)
                    .SetQueryParam("timeout", "1")
                    .WithOAuthBearerToken(bearerToken)
                    .WithHeader("User-Agent", "DataMover")
                    .WithHeader("Accept-Encoding", "deflate")
                    .WithHeader("Cache-Control", "no-cache")
                    .AllowAnyHttpStatus()
                    .GetAsync();

            if (!query.Result.IsSuccessStatusCode)
            {
                Console.WriteLine("Compass API: " + query.Result.ReasonPhrase);
                return null;
            }

            responseBody = await query.ReceiveString();

            dyn = JsonConvert.DeserializeObject(responseBody);

            Status = dyn.status;

            int loops = 0;
            while (!Status.Equals("FINISHED") && !Status.Equals("FAILED"))
            {
                query =
                    Program._compassURL
                        .AppendPathSegment("/v1/compass/jobs/" + queryID + "/status", false)
                        .SetQueryParam("timeout", "1")
                        .WithOAuthBearerToken(bearerToken)
                        .WithHeader("User-Agent", "DataMover")
                        .WithHeader("Accept-Encoding", "deflate")
                        .AllowAnyHttpStatus()
                        .GetAsync();

                if (!query.Result.IsSuccessStatusCode)
                {
                    Console.WriteLine("Compass API: " + query.Result.ReasonPhrase);
                    return null;
                }

                responseBody = await query.ReceiveString();

                dyn = JsonConvert.DeserializeObject(responseBody);
                if (dyn.status != Status)
                {
                    Console.WriteLine("");
                    Console.Write("Compass API: " + dyn.status);
                }

                Status = dyn.status;

                System.Threading.Thread.Sleep(1000);
                Console.Write(".");
                loops++;
            }
            Console.WriteLine("");

            columnTypes = dyn.columns;
            if (quickSave)
                File.WriteAllText("DL_QuickDataColumns.tmp", responseBody);

            Console.WriteLine("Result ID  : " + queryID + "");

            query =
                Program._compassURL
                    .AppendPathSegment("/v1/compass/jobs/" + queryID + "/result", false)
                    .WithOAuthBearerToken(bearerToken)
                    .WithHeader("Connection", "keep-alive")
                    .WithHeader("Cache-Control", "no-cache")
                    .WithHeader("User-Agent", "DataMover")
                    .WithHeader("Accept-Encoding", "Deflate")
                    .WithHeader("Accept", "application/x-ndjson")
                    .WithTimeout(600)
                    .AllowAnyHttpStatus()
                    .GetAsync();

            if (!query.Result.IsSuccessStatusCode)
            {
                Console.WriteLine("Compass API: " + query.Result.ReasonPhrase);
                return null;
            }

            responseBody = await query.ReceiveString();
            if (quickSave)
                File.WriteAllText("DL_QuickDataTable.tmp", responseBody);

            QuickDataLoad:

            DataTable thisData = new DataTable("ExportMI");

            int recordID = 0;
            int columnID = 0;
            if (Status.Equals("FAILED"))
            {
                Console.WriteLine("Compass API: " + responseBody);
                return null;
            }


            var jsonReader = new JsonTextReader(new StringReader(responseBody))
            {
                SupportMultipleContent = true // This is important!
            };

            var jsonSerializer = new JsonSerializer();


            //Return Results
            Console.WriteLine("Parsing Results to Data Table...");
            while (jsonReader.Read())
            {
                columnID = 0;
                dyn = jsonSerializer.Deserialize(jsonReader);

                if (recordID == 0)
                {
                    foreach (JProperty property in dyn)
                    {
                        thisData.Columns.Add(property.Name);
                        thisData.Columns[columnID].DataType = getTypeFromString(columnTypes[columnID].datatype.Value);
                        columnID++;
                    }
                    columnID = 0;
                }

                DataRow thisDataRow = thisData.NewRow();
                foreach (JProperty property in dyn)
                {


                    if (thisData.Columns[columnID].DataType == typeof(long)
                        && property.Value.ToString().IndexOf('.') > 0
                    )
                    {
                        thisDataRow[columnID] =
                            Convert.ChangeType(
                                property.Value.ToString().Substring(0,
                                    property.Value.ToString().IndexOf('.'))
                                , thisData.Columns[columnID].DataType);
                    }
                    else
                    {
                        thisDataRow[columnID] =
                            Convert.ChangeType(property.Value.ToString(), thisData.Columns[columnID].DataType);
                    }
                    columnID++;
                }

                thisData.Rows.Add(thisDataRow);

                recordID++;
            }
            return thisData;

        }

        private static Type getTypeFromString(string typeName)
        {
            switch (typeName.ToUpper())
            {
                case "STRING":
                    return typeof(string);

                case "INT":
                case "INTEGER":
                case "BIGINT":
                    return typeof(long);

                case "DECIMAL":
                    return typeof(decimal);
                case "BOOLEAN":
                    return typeof(string);
                case "TIMESTAMP":
                    return typeof(DateTime);
                default:
                    System.Diagnostics.Debug.WriteLine(typeName);
                    return typeof(string);
            }
        }

    }
}