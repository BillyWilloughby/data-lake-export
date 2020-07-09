@Echo Off
cls

echo Building Sample Reports
echo Running the samples requires setting the Compass URL 
echo and an IONAPI authentication file in your environment
echo variables.  See Read Me for more information.
echo.

echo Currently connected to %DL_CompassURL%
echo Loading IONAPI from %DL_ConnectionFile%

echo.

rem Create pdf
..\DataLakeExport PDF=0 SQL="basic.sql" Filename="Sample Export.pdf" Connection="%DL_ConnectionFile%" Title="Basic Test Query" Compass=%DL_CompassURL%
ECHO.

rem Create XLSX (Default)
..\DataLakeExport SQL="basic.sql" Filename="Sample Export.xlsx" Connection="%DL_ConnectionFile%" Title="Basic Test Query" Compass=%DL_CompassURL%
ECHO.

rem Create CSV
..\DataLakeExport CSV=3 SQL="basic.sql" Filename="Sample Export.csv" Connection="%DL_ConnectionFile%" Title="Basic Test Query" Compass=%DL_CompassURL%

ECHO.
rem Create Price List XLSX
..\DataLakeExport  SQL="PriceList.sql" Filename="Price List.xlsx" Connection="%DL_ConnectionFile%" Title="Price List" Compass=%DL_CompassURL%
ECHO.

rem Inventory
..\DataLakeExport PDF=0 BREAK="Warehouse" SQL="Inventory.sql" Filename="Inventory.pdf"  Connection="%DL_ConnectionFile%" Title="Inventory" Compass=%DL_CompassURL%
ECHO.
..\DataLakeExport                         SQL="Inventory.sql" Filename="Inventory.xlsx" Connection="%DL_ConnectionFile%" Title="Inventory" Compass=%DL_CompassURL%

pause