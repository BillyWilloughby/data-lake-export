// Copyright 2020 Billy Willoughby
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            dynamic dynDataReturn;
            string SQL = "";

            Boolean.TryParse(Environment.GetEnvironmentVariable("DL_QuickDataTable"), out bool quickSave);

            if (quickSave && File.Exists("DL_QuickDataTable.tmp"))
            {
                try
                {
                    responseBody = File.ReadAllText("DL_QuickDataTable.tmp");
                    columnTypes = File.ReadAllText("DL_QuickDataColumns.tmp");
                    SQL = File.ReadAllText("DL_QuickSQL.tmp", Encoding.UTF8);

                    if (SQL.Equals(File.ReadAllText(sqlFile, Encoding.UTF8)))
                    {

                        dynDataReturn = JsonConvert.DeserializeObject(columnTypes);
                        columnTypes = dynDataReturn.columns;
                        Status = "Quick";
                        goto QuickDataLoad;
                    }
                }
                catch (Exception ex)
                { //Do nothing if lookup fails
                    Console.WriteLine("Warning: " + ex.Message);
                }
            }

            string bearerToken = ionAPI.GetBearerAuthorization();

            if (bearerToken == null)
            {
                Console.WriteLine("Error getting IONAPI Token: " + ionAPI.LastMessage);
                Environment.Exit(-23);
            }

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
                Program.CompassURL
                    .AppendPathSegment("/v1/compass/jobs")
                    .SetQueryParam("resultFormat", "application/json")
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

            dynDataReturn = JsonConvert.DeserializeObject(responseBody);

            Status = dynDataReturn.status;
            string Location = dynDataReturn.location;
            string queryID = dynDataReturn.queryId;

            Console.Write("Compass API: " + dynDataReturn.status);

            query =
                Program.CompassURL
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

            dynDataReturn = JsonConvert.DeserializeObject(responseBody);

            Status = dynDataReturn.status;

            int loops = 0;
            while (!Status.Equals("FINISHED") && !Status.Equals("FAILED"))
            {
                query =
                    Program.CompassURL
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

                dynDataReturn = JsonConvert.DeserializeObject(responseBody);
                if (dynDataReturn.status != Status)
                {
                    Console.WriteLine("");
                    Console.Write("Compass API: " + dynDataReturn.status);
                }

                Status = dynDataReturn.status;

                System.Threading.Thread.Sleep(1000);
                Console.Write(".");
                loops++;
            }
            Console.WriteLine("");

            columnTypes = dynDataReturn.columns;
            if (quickSave)
                File.WriteAllText("DL_QuickDataColumns.tmp", responseBody);

            Console.WriteLine("Result ID  : " + queryID + "");

            query =
                Program.CompassURL
                    .AppendPathSegment("/v1/compass/jobs/" + queryID + "/result", false)
                    .WithOAuthBearerToken(bearerToken)
                    .WithHeader("Connection", "keep-alive")
                    .WithHeader("Cache-Control", "no-cache")
                    .WithHeader("User-Agent", "DataMover")
                    .WithHeader("Accept-Encoding", "Deflate")
                    .WithHeader("Accept", "application/json")
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

            if (Status.Equals("FAILED"))
            {
                Console.WriteLine("Compass API: " + responseBody);
                return null;
            }

            var jsonReader = new JsonTextReader(new StringReader(responseBody))
            {
                SupportMultipleContent = true // This is important!
            };

            thisData = JObject.Parse(responseBody)["data"].ToObject<DataTable>();

            return thisData;

        }

        private static Type GetTypeFromString(string typeName)
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