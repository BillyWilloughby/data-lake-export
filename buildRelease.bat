@echo off
rmdir bin /S /Q

echo Starting Build Linux x64
call dotnet publish -c Release -r "linux-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build OSX x64
call dotnet publish -c Release -r "osx-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build Windows x64
call dotnet publish -c Release -r "win-x64" --self-contained=true -p:PublishSingleFile=true
echo.
echo Starting Build Windows x86
call dotnet publish -c Release -r "win-x86" --self-contained=true -p:PublishSingleFile=true

rmdir PublishStandAlone /S /Q

xcopy bin\Release\netcoreapp3.1\win-x64\publish\*.* PublishStandAlone\win-x64\
xcopy bin\Release\netcoreapp3.1\win-x86\publish\*.* PublishStandAlone\win-x86\
xcopy bin\Release\netcoreapp3.1\osx-x64\publish\*.* PublishStandAlone\osx-x64\
xcopy bin\Release\netcoreapp3.1\linux-x64\publish\*.* PublishStandAlone\linux-x64\