@echo off
echo This will create the DataLake Export 7z file for release.
del DataLakeExport.7z
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\*.dll"
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\*.exe"
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\*.sql"
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\*.txt"
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\Samples\*.sql"
7za a -t7z DataLakeExport.7z "..\Data Lake Export\bin\x64\Release\Samples\*.bat"
