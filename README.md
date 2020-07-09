

# Data Lake Export

Data Lake Export is a small tool to allow the creation of files from the Infor(c) Data Lake SaaS product.  

It uses the general SQL syntax supported by the Infor(c) Compass API to pull the information, and based on the command line options, generates a formatted Excel, PDF, or a CSV document.  To connect to the Compass API requires an IONAPI(c) authorization file.  This tool was specifically tested with Infor M3(c) in mind, but should work with any JSON data in the Data Lake.

![Image of Workflow](https://billywilloughby.com/dle/Data%20Lake%20Export%20Flow.png)

<!--```mermaid-->
<!--graph LR;-->
<!--A[Data Lake Export] - - Compass API - -> B((Infor Data Lake))-->
<!--C[Result Data]; D[Output Document];-->
<!--B - -> C;-->
<!--C - -> D;-->
<!--```-->

## Usage
Data Lake Export works by reading the query from a file, connecting to the Data Lake via ION API, and taking the result set and formatting it into the requested output document. 

Quick Steps to Use:

 1. Create an ION API Authorization file for Data Lake Export
 2. Prototype your query in the Compass Query tool in ION Desk
 3. Save the query in a file accessible by the application
 4. Run the command specifying the desired output type and filename

An ION API authorization file to be created as a Backend Service for this application to use.  Create the authorization file in the ION API application in IOS(c), and save it to your local machine. 

Write a sample query in Compass in ION Desk.  For M3 data, a simple test would be to select something from the Item Master.

> **Query:** Select top 100 CONO, ITNO, FUDS from MITMAS order by CONO, ITNO

After testing the query, save it on the computer where the Data Lake Export application can access it.  With the query saved, and the authorization file saved, you're ready to build a file.


## Syntax Examples
In the Samples folder in the source code, you will find some sample SQL queries and batch files to generate various file types.  The samples use Windows Environment variables for the connection information.  You will need to set these variables for your environment, or edit the files as needed.
### Required Parameters
 - SQL
	 - Set to the filename containing the Compass / SQL query for the Data Lake.
 - Filename
	 - Set to the file name you wish to create.  This file will be deleted if it exists.  The default file type is Excel(c) xlsx.
 - Connection
	 - Set this to the filename of the IONAPI Authorization file used to connect to the Data Lake.
 - Compass
	 - Set this to the URL for the Compass endpoint for the Infor IOS(c) environment you will be connecting to.
 - Title
	 - Set this to the title of the document.  Make sure the title is Excel compatible.
### Optional Parameters
 - PDF
	 - Creates a PDF File
	 - Set to 0 to generate a PDF and set paper size based on data width
	 - Set to 1 to force to fit a letter landscape.  This will truncate data that's too wide.
	 - Set to 2 to force to fit a legal page, landscape.  This will truncate data that's too wide.
	 - Set to 3 to force to fit a legal page, landscape, 8 point font.  This will truncate data that's too wide.
	 - Set to 4 to force to fit a letter page, landscape, 8 point font.  This will truncate data that's too wide.
 - CSV
	 - Create a Flat File
	 - Set to 1 to set the Column delimiter to tab and the field delimiter to single quote
	 - Set to 2 to set the Column delimiter to comma and the field delimiter to single quote
	 - Set to 3 to set the Column delimiter to tab and the field delimiter to double quote
	 - Set to 4 to set the Column delimiter to comma and the field delimiter to double quote
	 - Set to 5 to set the Column delimiter to tab and the field delimiter to nothing
	 - Set to 6 to set the Column delimiter to comma and the field delimiter to nothing
	 - Set to 7 to set the Column delimiter to nothing and the field delimiter to nothing
	 - Set to 8 to set the Column delimiter to pipe and the field delimiter to nothing
 - EOL="<Value>"
 	- Override the default environment linefeed text
 	- LF = Char(10)
 	- CR = Char(13)
 	- LF, CR, CRLF, and LFCR are valid options

### Sample Syntax

*Create a PDF*
> DataLakeExport PDF=0 SQL="basic.sql" Filename="Sample Export.pdf" Connection=InforDEV.ionapi Title="Basic Test Query" Compass=https://mingle-ionapi.inforcloudsuite.com/YOURENVIRONMENT_TRN/IONSERVICES/datalakeapi

*Create an Excel workbook*
> DataLakeExport SQL="basic.sql" Filename="Sample Export.xlsx" Connection=InforDEV.ionapi Title="Basic Test Query" Compass=https://mingle-ionapi.inforcloudsuite.com/YOURENVIRONMENT_TRN/IONSERVICES/datalakeapi

Create a CSV flat file
> DataLakeExport CSV=3 SQL="basic.sql" Filename="Sample Export.csv" Connection=%DL_ConnectionFile% Title="Basic Test Query" Compass=%DL_CompassURL%



## Warranty
Copyright (C) 2020 Billy Willoughby
This program comes with ABSOLUTELY NO WARRANTY;
This is free software, and you are welcome to redistribute it under certain conditions, see the License.txt file for further details.
