@echo off
rmdir bin /S /Q

echo Starting Build Linux x64
dotnet publish -c Release -r "linux-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build OSX x64
dotnet publish -c Release -r "osx-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build Windows x64
dotnet publish -c Release -r "win-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build Windows x86
dotnet publish -c Release -r "win-x86" --self-contained=true -p:PublishSingleFile=true

rmdir Publish /S /Q

xcopy bin\Release\netcoreapp3.1\win-x64\publish\*.* Publish\win-x64\
xcopy bin\Release\netcoreapp3.1\win-x86\publish\*.* Publish\win-x86\
xcopy bin\Release\netcoreapp3.1\osx-x64\publish\*.* Publish\osx-x64\
xcopy bin\Release\netcoreapp3.1\linux-x64\publish\*.* Publish\linux-x64\