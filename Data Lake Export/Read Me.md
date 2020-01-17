Data Lake Export

###This is an application to run SQL against the Infor Data Lake and generate flat export files. 

###It currently supports CSV, XLSX, and PDF formats in the current version. XML support will be added in the future.

####Original source created as DataMover in 2006.  
####This branch project has been updated to support report outputs and M3 Data Lake APIs.
####©2006 - 2020 Billy Willoughby

The default export is Excel XLSX files. 

Several common uses have been listed in the Samples Folder.  The batch file in the Samples Folder must be updated with 
your Compass API endpoint and your IONAPI Authentiation file before the samples will execute.

###Syntax
                  
	Mandatory Syntax:
		SQL="Full Path to SQL input file"
		Filename="OutputFileName.xlsx"
		Connection="ION API Connection file"
		Compass="Compass URL"
		Title="Title Information in Excel" (Make sure Excel Compatible)

	All Parameters above are mandatory. 

	Optional Parameters:
	PDF=#
		 0 = Autosize PDF Form to document 
		 1 = Letter, Landscape forced 
		 2 = Legal, Landscape forced 
		 3 = Legal, Landscape forced, 8 point font 
		 4 = Letter, Landscape forced, 8 point font 

	Columns = Array of values
		 Example: COLUMNS=",,,1,1.5,2,"
		 Used for force the size of each column in a document

	CSV=#
		Create a Flat File
		Option 1: Column = tab, Field = single quote
		Option 2: Column = comma, Field = single quote
		Option 3: Column = tab, Field = double quote
		Option 4: Column = comma, Field = double quote
		Option 5: Column = tab, Field = nothing
		Option 6: Column = comma, Field = nothing
		Option 7: Column = nothing, Field = nothing
		Option 8: Column = pipe, Field = nothing

	EOL="<Value>"
		Override the default environment linefeed text
		LF = Char(10)
		CR = Char(13)
		LF, CR, CRLF, and LFCR are valid options