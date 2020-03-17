@echo off
rmdir bin /S /Q

echo Starting Build Linux x64
dotnet publish -c Release -r "linux-x64" --self-contained=true -p:PublishTrimmed=true
echo.
echo Starting Build OSX x64
dotnet publish -c Release -r "osx-x64" --self-contained=true -p:PublishTrimmed=true
echo.
echo Starting Build Windows x64
dotnet publish -c Release -r "win-x64" --self-contained=true -p:PublishTrimmed=true
echo.
echo Starting Build Windows x86
dotnet publish -c Release -r "win-x86" --self-contained=true -p:PublishTrimmed=true
