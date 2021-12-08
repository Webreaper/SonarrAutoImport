dotnet publish -r ubuntu.18.04-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.4.0
dotnet publish -r win7-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.4.0
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.4.0
