@Echo Off
@echo Off
cls

echo Building Sample Reports
echo Running the samples requires setting the Compass URL 
echo and an IONAPI authentication file for your environment.
echo.
set CompassURL="https://mingle-ionapi.inforcloudsuite.com/APPGROUP_DEM/IONSERVICES/datalakeapi"
set ConnectionFile="C:\Temp\Samples.ionapi"

echo Currently connected to %CompassURL%
echo Loading IONAPI from %ConnectionFile%

echo.

rem Create pdf
..\DataLakeExport PDF=0 SQL="basic.sql" Filename="Sample Export.pdf" Connection=%ConnectionFile% Title="Basic Test Query" Compass=%CompassURL%
ECHO.
rem Create XLSX
..\DataLakeExport  SQL="basic.sql" Filename="Sample Export.xlsx" Connection=%ConnectionFile% Title="Basic Test Query" Compass=%CompassURL%
ECHO.
rem Create CSV
..\DataLakeExport CSV=3 SQL="basic.sql" Filename="Sample Export.csv" Connection=%ConnectionFile% Title="Basic Test Query" Compass=%CompassURL%

ECHO.
rem Create Price List XLSX
..\DataLakeExport  SQL="PriceList.sql" Filename="Price List.xlsx" Connection=%ConnectionFile% Title="Price List" Compass=%CompassURL%
ECHO.
rem Inventory
..\DataLakeExport PDF=0 BREAK="Warehouse" SQL="Inventory.sql" Filename="Inventory.pdf" Connection=%ConnectionFile% Title="Inventory" Compass=%CompassURL%
ECHO.
..\DataLakeExport       BREAK="Warehouse" SQL="Inventory.sql" Filename="Inventory.xlsx" Connection=%ConnectionFile% Title="Inventory" Compass=%CompassURL%

pause