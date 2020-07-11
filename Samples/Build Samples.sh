#!/bin/bash
# The file DataLakeExport must be marked executable after extracting
#
# Please set the environment variables to use this sample. (or just put the information below)
# $ionAPI  = The IONAPI authorization file
# $compass = The Environment URL for the COMPASS API
echo

executable=$(file DataLakeExport -b | grep executable -c)

echo "Connecting to $compass"
echo "Using Security token: $ionAPI"
echo

if [ "$executable" = "1" ]; then
        echo Starting...
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