#!/bin/bash
# The file DataLakeExport must be marked executable after extracting
#
# Please set the environment variables to use this sample. (or just put the information below)
# $ionAPI  = The IONAPI authorization file
# $compass = The Environment URL for the COMPASS API
echo

if [ "$1" = "" ]; then
        echo "Please add the name of the SQL file you wish to execute as the first parameter"
        echo "Example: ./DataLakeExport basic.sql"
        echo
        exit 2
fi

executable=$(file DataLakeExport -b | grep executable -c)
echo "Processing SQL:            $1"
echo "Connecting to environment: $compass"
echo "Using security token:      $ionAPI"
echo

if [ "$executable" = "1" ]; then
        echo Starting Samples...
        echo
else
        echo DataLakeExport must be marked executable.
        echo Try
        echo chmod 770 ./DataLakeExport
        echo and run again.
        exit 1
fi

outputFilename=$1
outputFilename="${outputFilename%%.*}_Sample"


echo "Building XLSX..."
./DataLakeExport SQL=$1 Filename="$outputFilename.xlsx" Connection="$ionAPI" Title=Test\ Document Compass="$compass"
echo
echo "Building PDF..."
./DataLakeExport SQL=$1 Filename="$outputFilename.pdf"  Connection="$ionAPI" Title=Test\ Document Compass="$compass" PDF=0
echo
