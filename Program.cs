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

using OfficeOpenXml;
using OfficeOpenXml.Table;
using System.Drawing;
using System.IO;

namespace Data_Lake_Export
{
    using Flurl.Http;
    using MigraDoc.DocumentObjectModel;
    using MigraDoc.DocumentObjectModel.Tables;
    using MigraDoc.Rendering;
    using Newtonsoft.Json;
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.Text;
    using Font = MigraDoc.DocumentObjectModel.Font;

    namespace DLExport
    {
        class Program
        {
            private static string _sql;
            private static string _filename;
            private static string _title;
            private static string _connectionFile;
            private static bool _renderAsPDF;
            private static bool _renderAsCSV;
            private static string _optionCSV = "0";
            private static string _csvDL;
            private static string _csvFL;
            private static string _optionPDF = "0";
            private static double[] _columnSizeOverrides;
            private static string _rowBreak = "";
            private static int? _rowBreakLocation;
            private static readonly string _status;
            private static Shading shadingHeader = new Shading();
            private static readonly Shading shadingAlternateRow = new Shading();
            private static double _intMajorWidth = 0; //Used to hold the overall width of PDF
            private static IONAPI _ionAPI;
            private static string _proxy;
            public static string CompassURL { get; set; }

            static void Main(string[] args)
            {
                try
                {
                    ParseCommandLineParameters(args);

                    LoadIONAPI(_connectionFile);

                    //Setup Proxy if listed on command line
                    if (_proxy != null && !_proxy.Equals(""))
                        FlurlHttp.Configure(settings =>
                        {
                            settings.HttpClientFactory = new ProxyHttpClientFactory(_proxy);

                        });

                    Console.WriteLine("Output will be written to: " + _filename);
                    DataTable queryResults = DataLake.RunDataLakeAsync(_sql, _ionAPI).Result;

                    if (queryResults == null)
                    {
                        Console.WriteLine("Error pulling data from Compass");
                        Environment.Exit(-1);
                    };

                    if (System.IO.File.Exists(_filename))
                    {
                        Console.WriteLine("Deleting " + _filename);
                        System.IO.File.Delete(_filename);
                    }

                    if (_renderAsPDF)//PDF Option
                    {
                        Environment.Exit(ExportToPDF(queryResults));
                    }

                    if (_renderAsCSV)//CSV Option
                    {
                        Environment.Exit(ExportToCSV(_csvFL, _csvDL, queryResults));
                    }

                    //Default, Excel Option
                    Environment.Exit(ExportToExcel(queryResults));

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Environment.Exit(-2);
                }

            }

            private static void ParseCommandLineParameters(string[] args)
            {

                foreach (string thisParameter in args)
                {
                    if (thisParameter.Contains("="))
                    {
                        string parameter = thisParameter.Substring(0, thisParameter.IndexOf("="));
                        string value = thisParameter.Substring(thisParameter.IndexOf("=") + 1);

                        if (value.Contains("%"))
                        {   //Workaround for VS not expanding variables in debug
                            value = Environment.GetEnvironmentVariable(value.Replace("%", ""), EnvironmentVariableTarget.User);
                        }

                        switch (parameter.ToUpper())
                        {
                            case "CSV":
                                _renderAsCSV = true;
                                _optionCSV = value;
                                switch (_optionCSV)
                                {
                                    case "1":
                                        _csvDL = "\t";
                                        _csvFL = "'";
                                        break;
                                    case "2":
                                        _csvDL = ",";
                                        _csvFL = "'";
                                        break;
                                    case "3":
                                        _csvDL = ",";
                                        _csvFL = "\"";
                                        break;
                                    case "4":
                                        _csvDL = "\t";
                                        _csvFL = "\"";
                                        break;
                                    case "5":
                                        _csvDL = ",";
                                        _csvFL = "";
                                        break;
                                    case "6":
                                        _csvDL = "\t";
                                        _csvFL = "";
                                        break;
                                    case "7":
                                        _csvDL = "";
                                        _csvFL = "";
                                        break;
                                    case "8":
                                        _csvDL = "|";
                                        _csvFL = "";
                                        break;
                                }

                                break;

                            case "SQL":
                                _sql = value;
                                break;

                            case "COMPASS":
                                CompassURL = value;
                                break;

                            case "FILENAME":
                                _filename = value;
                                break;

                            case "TITLE":
                                _title = value;
                                break;

                            case "CONNECTION":
                                _connectionFile = value;
                                break;

                            case "PROXY":
                                _proxy = value;
                                break;

                            case "PDF":
                                _renderAsPDF = true;
                                _optionPDF = value;
                                break;

                            case "COLUMNS":
                                string[] columns = value.Split(',');
                                _columnSizeOverrides = new double[columns.Length];
                                int intX = 0;
                                foreach (string thisColumn in columns)
                                {
                                    if (thisColumn != null && thisColumn.Trim() != "" && double.Parse(thisColumn) >= 0)
                                    {
                                        _columnSizeOverrides[intX] = double.Parse(thisColumn);
                                    }
                                    else
                                    {
                                        _columnSizeOverrides[intX] = -1;
                                    }

                                    intX += 1;
                                }

                                break;

                            case "BREAK":
                                _rowBreak = value;
                                break;

                            case "EOL":
                                switch (value)
                                {
                                    case "LF":
                                        EOLOverride = new char[1];
                                        EOLOverride[0] = ((char)10);

                                        break;

                                    case "CR":
                                        EOLOverride = new char[1];
                                        EOLOverride[0] = ((char)13);
                                        break;

                                    case "LFCR":
                                        EOLOverride = new char[2];
                                        EOLOverride[0] = ((char)10);
                                        EOLOverride[1] = ((char)13);
                                        break;

                                    case "CRLF":
                                        EOLOverride = new char[2];
                                        EOLOverride[0] = ((char)13);
                                        EOLOverride[1] = ((char)10);

                                        break;
                                }

                                break;
                        }
                    }
                }

                if (EOLOverride == null)
                {
                    EOLOverride = new char[2];
                    EOLOverride[0] = ((char)13);
                    EOLOverride[1] = ((char)10);
                }


                if (_sql is null || _filename is null || _title is null || _connectionFile is null || CompassURL is null
                || args.Length == 0)
                {
                    Console.WriteLine("Data Lake Export");
                    Console.WriteLine("Copyright 2020 Billy Willoughby");
                    Console.WriteLine("");
                    Console.WriteLine("Licensed under the Apache License, Version 2.0 (the \"License\")");
                    Console.WriteLine("you may not use this file except in compliance with the License.");
                    Console.WriteLine("See License.txt file for further details.");
                    Console.WriteLine("");
                    Console.WriteLine("Syntax:");
                    Console.WriteLine("\tSQL=\"File path to Compass Query\"");
                    Console.WriteLine("\tFilename=\"some output.xlsx\"");
                    Console.WriteLine("\tConnection=\"File path to ION API Authentication file\"");
                    Console.WriteLine("\tCompass=\"Compass URL\"");
                    Console.WriteLine("\tTitle=\"Title Information in Excel\" (Make sure Excel Compatible)");
                    Console.WriteLine("");
                    Console.WriteLine("All Parameters above are mandatory.  Please set parameters and call again.");
                    Console.WriteLine("");
                    Console.WriteLine("Optional Parameters:");
                    Console.WriteLine("");
                    Console.WriteLine("PDF=#");
                    Console.WriteLine("\t 0 = Autosize PDF Form to document ");
                    Console.WriteLine("\t 1 = Letter, Landscape forced ");
                    Console.WriteLine("\t 2 = Legal, Landscape forced ");
                    Console.WriteLine("\t 3 = Legal, Landscape forced, 8 point font ");
                    Console.WriteLine("\t 4 = Letter, Landscape forced, 8 point font ");
                    Console.WriteLine("");
                    Console.WriteLine("Columns = Array of values");
                    Console.WriteLine("\t Example: COLUMNS=\",,,1,1.5,2,\"");
                    Console.WriteLine("\t Used for force the size of each column in a document");
                    Console.WriteLine("");
                    Console.WriteLine("CSV=#");
                    Console.WriteLine("\tCreate a Flat File");
                    Console.WriteLine("\tOption 1: Column = tab, Field = single quote");
                    Console.WriteLine("\tOption 2: Column = comma, Field = single quote");
                    Console.WriteLine("\tOption 3: Column = tab, Field = double quote");
                    Console.WriteLine("\tOption 4: Column = comma, Field = double quote");
                    Console.WriteLine("\tOption 5: Column = tab, Field = nothing");
                    Console.WriteLine("\tOption 6: Column = comma, Field = nothing");
                    Console.WriteLine("\tOption 7: Column = nothing, Field = nothing");
                    Console.WriteLine("\tOption 8: Column = pipe, Field = nothing");
                    Console.WriteLine("");
                    Console.WriteLine("EOL=\"<Value>\"");
                    Console.WriteLine("\tOverride the default environment linefeed text");
                    Console.WriteLine("\tLF = Char(10)");
                    Console.WriteLine("\tCR = Char(13)");
                    Console.WriteLine("\tLF, CR, CRLF, and LFCR are valid options");
                    Console.ReadKey();
                    Environment.Exit(-21);
                }

            }

            private static void LoadIONAPI(string connection)
            {
                Console.WriteLine("Loading IONAPI configuration...");

                string connInfo = System.IO.File.ReadAllText(connection);

                dynamic dyn = JsonConvert.DeserializeObject(connInfo);

                string or = dyn.or;
                string application = dyn.cn;
                string ci = dyn.ci;
                string cs = dyn.cs;
                string pu = dyn.pu;
                string saak = dyn.saak;
                string sask = dyn.sask;
                string oa = dyn.oa;
                string ot = dyn.ot;
                _ionAPI = new IONAPI(application, ci, cs, pu, saak, sask, oa, ot, or);
            }

            public static char[] EOLOverride { get; set; }

            private static int ExportToCSV(string fl, string dl, DataTable queryResults)
            {
                try
                {

                    Console.WriteLine("Export to CSV selected");
                    StringBuilder thisFile = new StringBuilder();
                    int rowIndex = 0;
                    //int colIndex = 0;

                    int intX = 0;
                    string[] rowHeaders = new string[queryResults.Columns.Count];


                    foreach (DataRow thisDataRow in queryResults.Rows)
                    {
                        string thisRow = "";
                        intX = 0;
                        if (rowIndex / 5000.0 == Math.Truncate(rowIndex / 5000.0))
                        {
                            Console.WriteLine("Writing Rows " + rowIndex + "+");
                            System.IO.File.AppendAllText(_filename, thisFile.ToString());
                            thisFile = new StringBuilder();
                        }
                        foreach (string thisColumn in rowHeaders)
                        {
                            thisRow = thisRow + fl + Escape(thisDataRow[intX]) + fl + dl;
                            intX += 1;
                        }
                        rowIndex += 1;

                        if (EOLOverride.Length == 1)
                        {
                            thisFile.Append(thisRow.Substring(0, thisRow.Length - _csvDL.Length) + EOLOverride[0]);
                        }
                        else
                        {
                            thisFile.Append(thisRow.Substring(0, thisRow.Length - _csvDL.Length) + EOLOverride[0] + EOLOverride[1]);
                        }

                    }

                    Console.WriteLine("Writing Final ...");
                    System.IO.File.AppendAllText(_filename, thisFile.ToString());
                    Console.WriteLine("Complete");
                    return 0;


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return -12;
                }
            }

            private static string Escape(object getValue)
            {
                string thisValue = getValue.ToString();
                if (_csvFL.Length > 0)
                {

                    if (thisValue.Contains(_csvFL))
                    {
                        return _csvFL.Replace(_csvFL, _csvFL + _csvFL);
                    }
                    else
                    {
                        return thisValue;
                    }
                }
                else
                {
                    return thisValue;
                }
            }

            private static void createNewSection(ref Document workingDoc, string sectionHead)
            {
                workingDoc.AddSection();
                workingDoc.LastSection.PageSetup.TopMargin = Unit.FromInch(.25);
                workingDoc.LastSection.PageSetup.BottomMargin = Unit.FromInch(.5);
                workingDoc.LastSection.PageSetup.LeftMargin = Unit.FromInch(.25);
                workingDoc.LastSection.PageSetup.RightMargin = Unit.FromInch(.25);
                Paragraph pTitle = new Paragraph();
                workingDoc.LastSection.AddParagraph(sectionHead, "Heading1");
                if (_rowBreak != null && _rowBreak != "" && sectionHead != "")
                {

                    //workingDoc.LastSection.Headers.Primary.AddParagraph(sectionHead);
                    //workingDoc.LastSection.PageSetup.HeaderDistance = new Unit(.75, UnitType.Inch);
                    //workingDoc.LastSection.AddParagraph(sectionHead, "Heading1");
                    workingDoc.LastSection.Headers.Primary.AddParagraph(_rowBreak + ":" + sectionHead);

                }

                Unit myFooter = new Unit(.20, UnitType.Inch);
                workingDoc.LastSection.PageSetup.FooterDistance = myFooter;
                //workingDoc.DefaultPageSetup.FooterDistance = myFooter;
                Paragraph pFooter = new Paragraph();
                pFooter.AddText("Page ");
                pFooter.AddPageField();
                pFooter.AddText(" of ");
                pFooter.AddNumPagesField();
                workingDoc.LastSection.Footers.Primary.Add(pFooter);
            }

            private static Table CreateNewTable(ref Document workingDoc, ref string[] rowHeaders, Font fontHeader, int fontSize)
            {
                int intX = 0;
                Table workingTable = new Table();
                workingTable.Borders.Width = 0.5;

                foreach (string thisColumn in rowHeaders)
                {
                    workingTable.AddColumn(Unit.FromCentimeter(fontSize));
                }

                Row thisHeader = workingTable.AddRow();

                thisHeader.Format.Font = fontHeader.Clone();
                intX = 0;

                thisHeader.Format.Shading = shadingHeader.Clone();
                thisHeader.Shading = shadingHeader.Clone();
                thisHeader.HeadingFormat = true;

                foreach (string thisColumn in rowHeaders)
                {
                    thisHeader.Cells[intX].AddParagraph(rowHeaders[intX]);
                    thisHeader.Cells[intX].Shading = shadingHeader.Clone();
                    intX = intX + 1;
                }

                return workingTable;

            }

            private static int ExportToExcel(DataTable queryResults)
            {
                try
                {
                    // If you are a commercial business and have
                    // purchased commercial licenses use the static property
                    // LicenseContext of the ExcelPackage class :
                    ExcelPackage.LicenseContext = LicenseContext.Commercial;
                    using (ExcelPackage p = new ExcelPackage())
                    {
                        p.Workbook.Properties.Author = "Data Lake Export";
                        p.Workbook.Properties.Title = _title;
                        p.Workbook.Properties.Company = "";

                        // The rest of our code will go here...
                        p.Workbook.Worksheets.Add(_title);
                        ExcelWorksheet ws = p.Workbook.Worksheets[0];
                        // 1 is the position of the worksheet
                        ws.Name = _title;
                        //Build Headers

                        int rowIndex = 0;
                        int colIndex = 0;

                        string[] rowHeaders = new string[queryResults.Columns.Count];

                        while (queryResults.Columns.Count > colIndex)
                        {
                            rowHeaders[colIndex] = queryResults.Columns[colIndex].ColumnName;

                            //Excel starts at 1,1
                            ws.Cells[rowIndex + 1, colIndex + 1].Value = queryResults.Columns[colIndex].ColumnName;

                            switch (
                                queryResults.Columns[colIndex].DataType.ToString().ToUpper().Trim())
                            {
                                case "BIGINT":
                                case "SMALLINT":
                                case "SYSTEM.INT64":
                                case "INT":
                                    ws.Column(colIndex + 1).Style.Numberformat.Format = "##0";
                                    break;

                                case "SYSTEM.DECIMAL":
                                case "DECIMAL":
                                    ws.Column(colIndex + 1).Style.Numberformat.Format = "#,##0";
                                    break;

                                case "SYSTEM.STRING":
                                case "VARCHAR":
                                case "NVARCHAR":
                                case "NCHAR":
                                case "CHAR":
                                    break;

                                case "SYSTEM.DATETIME":
                                case "DATETIME":
                                case "DATETIME2":
                                    ws.Column(colIndex + 1).Style.Numberformat.Format = "yyyy-mm-dd HH:MM";
                                    break;

                                default:
                                    Console.WriteLine("Unknown Data type: " +
                                                      queryResults.Columns[colIndex].DataType.ToString().ToUpper().Trim());
                                    break;

                            }
                            colIndex++;

                        }

                        rowIndex++;//First Row is Header Row  Start Writing on Next Row


                        if (queryResults.Rows.Count > 0)
                        {
                            foreach (DataRow thisDataRow in queryResults.Rows)
                            {
                                colIndex = 0;
                                if (rowIndex / 500.0 == Math.Truncate(rowIndex / 500.0))
                                {
                                    Console.Write("\rProcessing Rows " + rowIndex + "+                             ");
                                }
                                foreach (string thisColumn in rowHeaders)
                                {
                                    ws.Cells[rowIndex + 1, colIndex + 1].Value = thisDataRow.ItemArray[colIndex].ToString();
                                    colIndex += 1;
                                }
                                //Console.Write("\rRecord {0}...          ", rowIndex);
                                rowIndex++;
                            }

                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine("No rows found.");

                            return 0;

                        }


                        Console.WriteLine("Formatting ...");
                        ExcelTable table1 = ws.Tables.Add(ws.Cells[ws.Dimension.Address], "table1");
                        table1.TableStyle = TableStyles.Medium2;
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

                        // Save the Excel file
                        Console.WriteLine("Writing ...");
                        byte[] bin = p.GetAsByteArray();

                        File.WriteAllBytes(_filename, bin);
                        Console.WriteLine("Fini");
                        return 0;

                    };

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return -2;
                }
            }

            private static int ExportToPDF(DataTable queryResults)
            {
                try
                {
                    PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                    int fontSize = 10;
                    var pdfDoc = new PdfDocument();
                    var pdfPage = pdfDoc.AddPage();
                    var pdfGfx = XGraphics.FromPdfPage(pdfPage);
                    shadingHeader.Color = Color.FromRgb(189, 212, 249);
                    shadingAlternateRow.Color = Color.FromRgb(224, 236, 255);

                    // Create a new MigraDoc document
                    Document pdfDDocument = new Document();
                    pdfDDocument.Info.Title = _title;
                    pdfDDocument.Info.Subject = _title + " PDF Generated by Data Lake Export (PDF) ";
                    pdfDDocument.Info.Author = "Billy Willoughby";

                    setupDefaultPage(ref pdfDDocument, ref fontSize);

                    //Setup Fonts
                    XFont myStandardFont = new XFont(FontFamily.GenericMonospace, fontSize, XFontStyle.Regular);
                    XFont myHeaderFont = new XFont(FontFamily.GenericMonospace, fontSize, XFontStyle.Bold);
                    Font fontStandard = new Font(FontFamily.GenericMonospace.Name, fontSize);
                    Font fontHeader = new Font(FontFamily.GenericMonospace.Name, fontSize);
                    fontHeader.Bold = true;

                    createNewSection(ref pdfDDocument, _title);

                    string[] rowHeaders = new string[queryResults.Columns.Count];
                    XSize[] columnWidth = new XSize[queryResults.Columns.Count];


                    /*Holder for Column names*/
                    foreach (DataColumn thisColumn in queryResults.Columns)
                    {
                        rowHeaders[queryResults.Columns.IndexOf(thisColumn)] = thisColumn.ColumnName;
                        if (_rowBreak != "" && _rowBreak.ToUpper() == thisColumn.ColumnName.ToUpper() && _rowBreakLocation == null)
                        {
                            _rowBreakLocation = queryResults.Columns.IndexOf(thisColumn);
                        }
                    }

                    Table thisTable = CreateNewTable(ref pdfDDocument, ref rowHeaders, fontHeader, fontSize);

                    bool rowFlip = false;
                    //int documentRowIndex = 0;  //Row one was created in create table function
                    int tableRowIndex = 0;  //Row one was created in create table function

                    Row thisRow;
                    if (queryResults.Rows.Count > 0)
                    {
                        foreach (DataRow thisDataRow in queryResults.Rows)
                        {
                            if (queryResults.Rows.IndexOf(thisDataRow) / 5000.0 == Math.Truncate(queryResults.Rows.IndexOf(thisDataRow) / 5000.0))
                            {
                                Console.WriteLine("Reading Rows " + queryResults.Rows.IndexOf(thisDataRow) + "+");
                            }

                            if (queryResults.Rows.IndexOf(thisDataRow) == 1 && _rowBreak != "")
                            {

                                pdfDDocument.LastSection.AddParagraph(
                                    thisDataRow.ItemArray[(int)_rowBreakLocation].ToString().Trim());
                                pdfDDocument.LastSection.LastParagraph.AddBookmark(
                                    thisDataRow.ItemArray[(int)_rowBreakLocation].ToString().Trim());


                            }

                            if (_rowBreak != "" && _rowBreakLocation != null &&
                                thisDataRow.ItemArray[(int)_rowBreakLocation].ToString().Trim() != "" /*&& tableRowIndex > 1*/)
                            {

                                if (_rowBreak != "")
                                {
                                    _title = thisDataRow.ItemArray[(int)_rowBreakLocation].ToString().Trim();
                                    pdfDDocument.LastSection.PageSetup.TopMargin = Unit.FromInch(.65);
                                }

                                if (thisTable.Rows.Count > 1)
                                {
                                    Table finalTable = thisTable.Clone();
                                    autosizeTableColumns(ref finalTable, ref rowHeaders, ref columnWidth, myStandardFont, myHeaderFont, fontSize);
                                    pdfDDocument.LastSection.Add(finalTable);
                                    createNewSection(ref pdfDDocument, thisDataRow.ItemArray[(int)_rowBreakLocation].ToString().Trim());
                                    thisTable = CreateNewTable(ref pdfDDocument, ref rowHeaders, fontHeader, fontSize);
                                    columnWidth = new XSize[queryResults.Columns.Count];
                                    tableRowIndex = 0;
                                }
                            }
                            tableRowIndex++;

                            thisRow = thisTable.AddRow();
                            thisRow.Format.Font = fontStandard.Clone();
                            if (rowFlip)
                            {
                                rowFlip = false;
                                thisRow.Shading = shadingAlternateRow.Clone();
                            }
                            else
                            {
                                rowFlip = true;
                            }

                            foreach (DataColumn thisColumn in queryResults.Columns)
                            {
                                thisRow.Cells[queryResults.Columns.IndexOf(thisColumn)].AddParagraph(
                                   thisDataRow.ItemArray[queryResults.Columns.IndexOf(thisColumn)].ToString());

                                if (pdfGfx.MeasureString(thisDataRow.ItemArray[queryResults.Columns.IndexOf(thisColumn)].ToString(),
                                    myStandardFont).Width > columnWidth[queryResults.Columns.IndexOf(thisColumn)].Width)
                                {
                                    columnWidth[queryResults.Columns.IndexOf(thisColumn)] = pdfGfx.MeasureString(thisDataRow.ItemArray[queryResults.Columns.IndexOf(thisColumn)].ToString(), myStandardFont);
                                    columnWidth[queryResults.Columns.IndexOf(thisColumn)] = pdfGfx.MeasureString(thisDataRow.ItemArray[queryResults.Columns.IndexOf(thisColumn)].ToString(), myStandardFont);
                                    columnWidth[queryResults.Columns.IndexOf(thisColumn)].Width = columnWidth[queryResults.Columns.IndexOf(thisColumn)].Width + fontSize;
                                }

                            }

                        }
                        //End Loop




                        autosizeTableColumns(ref thisTable, ref rowHeaders, ref columnWidth, myStandardFont, myHeaderFont, fontSize);

                        pdfDDocument.LastSection.PageSetup.LeftMargin = Unit.FromInch(.25);

                        if (_rowBreak != "")
                        {
                            pdfDDocument.LastSection.PageSetup.TopMargin = Unit.FromInch(.75);
                        }
                        pdfDDocument.LastSection.Add(thisTable);

                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        return 0;
                    }

                    Console.WriteLine("Rendering PDF ...");
                    if (_optionPDF == "4")
                    {
                        pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                        pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(_intMajorWidth + .45 /*Margins*/);
                        pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(11);
                    }

                    if (_optionPDF == "0")
                    {
                        if (_intMajorWidth + .45 /*Margins*/<= 8) /*Letter*/
                        {
                            Console.WriteLine("Render Width: " + ((_intMajorWidth + .45).ToString("####.0")) + ", selecting Letter Portrait");
                            pdfDDocument.DefaultPageSetup.Orientation = Orientation.Portrait;
                            pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(11);
                            pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
                            pdfDDocument.DefaultPageSetup.RightMargin = .25;
                            pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                            pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                            pdfDDocument.DefaultPageSetup.TopMargin = .25;

                        }

                        if (_intMajorWidth + .45 /*Margins*/> 8 && _intMajorWidth + .45 /*Margins*/<= 11) /*Letter Landscape*/
                        {
                            Console.WriteLine("Render Width: " + ((_intMajorWidth + .45).ToString("####.0")) + ", selecting Letter Landscape");
                            pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                            pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(11);
                            pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
                            pdfDDocument.DefaultPageSetup.RightMargin = .25;
                            pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                            pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                            pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        }

                        if (_intMajorWidth + .45 /*Margins*/> 11 && _intMajorWidth + .45 /*Margins*/<= 14) /*Legal Landscape*/
                        {
                            Console.WriteLine("Render Width: " + ((_intMajorWidth + .45).ToString("####.0")) + ", selecting Legal Landscape");
                            pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                            pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(14);
                            pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
                            pdfDDocument.DefaultPageSetup.RightMargin = .25;
                            pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                            pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                            pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        }

                        if (_intMajorWidth + .45 /*Margins*/> 14) /*Custom*/
                        {
                            Console.WriteLine("Render Width: " + ((_intMajorWidth + .45).ToString("####.0")) + ", selecting Custom Paper");
                            pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(_intMajorWidth + .45 /*Margins*/);
                            pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(11);
                            pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                            pdfDDocument.DefaultPageSetup.RightMargin = .25;
                            pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                            pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                            pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        }

                    }

                    Console.WriteLine("Rendering ...");


                    renderer.Document = pdfDDocument;
                    renderer.DocumentRenderer.PrepareDocumentProgress += DocumentRenderer_PrepareDocumentProgress;
                    renderer.DocumentRenderer.PrepareDocument();

                    renderer.RenderDocument();

                    // Save the document...
                    string filename = _filename;
                    Console.WriteLine();
                    Console.WriteLine("Writing to disk ...");
                    renderer.PdfDocument.Save(filename);
                    Console.WriteLine("Fini");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return -4;
                }
            }

            private static void autosizeTableColumns(ref Table thisTable, ref string[] rowHeaders, ref XSize[] columnWidth,
                XFont myStandardFont, XFont myHeaderFont, int fontSize)
            {
                int colIndex = 0;
                double thisTableWidth = 0;
                var pdfDoc = new PdfDocument();
                var pdfPage = pdfDoc.AddPage();
                var pdfGfx = XGraphics.FromPdfPage(pdfPage);

                if (pdfGfx.MeasureString(rowHeaders[colIndex], myHeaderFont).Width > columnWidth[colIndex].Width)
                {
                    //Debug.WriteLine("Column: " + colIndex + " Width:" + columnWidth[colIndex].Width);
                    columnWidth[colIndex] = pdfGfx.MeasureString(rowHeaders[colIndex], myHeaderFont);
                    columnWidth[colIndex].Width = columnWidth[colIndex].Width + (fontSize * 2);
                    //Debug.WriteLine("Column: " + colIndex + " Width:" + columnWidth[colIndex].Width);
                }

                if (_columnSizeOverrides == null || _columnSizeOverrides.Length == 0)
                {
                    foreach (string thisColumn in rowHeaders)
                    {

                        Unit myUnit = columnWidth[colIndex].Width;
                        Debug.WriteLine(myUnit.Centimeter.ToString());
                        thisTable.Columns[colIndex].Width = myUnit;
                        thisTableWidth = thisTableWidth + myUnit.Inch;

                        colIndex = colIndex + 1;
                    }
                    if (thisTableWidth > _intMajorWidth)
                    {
                        _intMajorWidth = thisTableWidth;
                    }
                }
                else
                {
                    foreach (string thisColumn in rowHeaders)
                    {
                        Unit myUnit;
                        if (_columnSizeOverrides[colIndex] >= 0)
                        {/*Use Manual Size*/
                            myUnit = new Unit(_columnSizeOverrides[colIndex],
                               UnitType.Inch);
                        }
                        else
                        {/*Use Auto size*/
                            myUnit = columnWidth[colIndex].Width;
                        }
                        thisTable.Columns[colIndex].Width = myUnit;
                        _intMajorWidth = _intMajorWidth + myUnit.Inch;
                        colIndex = colIndex + 1;
                    }

                }
            }

            private static void setupDefaultPage(ref Document pdfDDocument, ref int fontSize)
            {
                switch (_optionPDF)
                {
                    case "0":
                        //Auto Code
                        break;
                    case "1":
                        //Letter Rotate
                        pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                        pdfDDocument.DefaultPageSetup.RightMargin = .25;
                        pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                        pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                        pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        break;

                    case "2":
                        //Legal Rotate Normal Font
                        pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(14);
                        pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
                        pdfDDocument.DefaultPageSetup.RightMargin = .25;
                        pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                        pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                        pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                        break;

                    case "3":
                        //Legal Rotate 8 point font
                        pdfDDocument.DefaultPageSetup.PageHeight = Unit.FromInch(14);
                        pdfDDocument.DefaultPageSetup.PageWidth = Unit.FromInch(8.5);
                        pdfDDocument.DefaultPageSetup.RightMargin = .25;
                        pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                        pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                        pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                        fontSize = 8;
                        break;

                    case "4":
                        //Make it Fit
                        pdfDDocument.DefaultPageSetup.RightMargin = .25;
                        pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                        pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                        pdfDDocument.DefaultPageSetup.TopMargin = .25;
                        pdfDDocument.DefaultPageSetup.Orientation = Orientation.Landscape;
                        fontSize = 8;
                        break;

                    default:
                        Console.WriteLine("Unknown Option for PDF selected:" + _optionPDF);
                        pdfDDocument.DefaultPageSetup.RightMargin = .25;
                        pdfDDocument.DefaultPageSetup.LeftMargin = .25;
                        pdfDDocument.DefaultPageSetup.BottomMargin = .25;
                        pdfDDocument.DefaultPageSetup.TopMargin = .25;

                        break;
                }



            }

            private static int PDFPageNumber = 1;

            private static void DocumentRenderer_PrepareDocumentProgress(object sender, DocumentRenderer.PrepareDocumentProgressEventArgs e)
            {
                Console.Write("\rPage {0}...          ", PDFPageNumber);
                PDFPageNumber++;
            }
        }
    }

    public class TokenHolder
    {
        public string BearerToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresTime { get; set; }
    }
}
