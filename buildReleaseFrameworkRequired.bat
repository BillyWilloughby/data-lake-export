@echo off
rmdir bin /S /Q

echo Starting Build Linux x64
dotnet publish -c Release -r "linux-x64" --self-contained=false -p:PublishSingleFile=true
echo.
echo Starting Build OSX x64
dotnet publish -c Release -r "osx-x64" --self-contained=false -p:PublishSingleFile=true
echo.
echo Starting Build Windows x64
dotnet publish -c Release -r "win-x64" --self-contained=false -p:PublishSingleFile=true
echo.
echo Starting Build Windows x86
dotnet publish -c Release -r "win-x86" --self-contained=false -p:PublishSingleFile=true

rmdir PublishFrameworkRequired /S /Q

xcopy bin\Release\netcoreapp3.1\win-x64\publish\*.* PublishFrameworkRequired\win-x64\
xcopy bin\Release\netcoreapp3.1\win-x86\publish\*.* PublishFrameworkRequired\win-x86\
xcopy bin\Release\netcoreapp3.1\osx-x64\publish\*.* PublishFrameworkRequired\osx-x64\
xcopy bin\Release\netcoreapp3.1\linux-x64\publish\*.* PublishFrameworkRequired\linux-x64\