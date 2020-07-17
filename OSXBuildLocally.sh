#!/bin/bash
#This application requires the .NetCore 3.1 be installed
clear
echo This Script will download the Data Lake Export console program and build it.
echo It *Requires* the .Net Core 3.1+ Framework to build correctly.
echo Testing for .Net Core Version...
dotnet --version
echo
read -s -n 1 -p "Press any key to continue . . . (or Ctrl-C to stop)"
echo
echo Downloading Code ...
git clone https://github.com/BillyWilloughby/data-lake-export.git --single-branch
cd data-lake-export
echo
echo Restoring Packages ...
dotnet build
echo
echo Compling Executable ...
dotnet publish -c Release -r "osx-x64" --self-contained=false -p:PublishSingleFile=true
echo
echo Moving Files ...
mv bin/Release/netcoreapp3.1/osx-x64/publish/*.* ../
mv bin/Release/netcoreapp3.1/osx-x64/publish/DataLakeExport  ../
mv -f bin/Release/netcoreapp3.1/osx-x64/Samples/*.* ../Samples/
echo
echo Cleaning Up ...
cd ..
rm -rf data-lake-export
echo
echo Setting Up Executable ...
chmod 770 ./DataLakeExport
chmod 770 Samples/Build\ Samples.sh
echo
echo Complete
echo
echo To run the Samples, you must either edit the files to put in your Information
echo or have setup the Environment variables for your environment per the Wiki.
echo The Sample extract requires MITMAS in the Data Lake.
echo 
echo Run: "./Build\\ Samples.sh Basic.sql" from the command line in the Samples 
echo directory to test the Sample extracts.



