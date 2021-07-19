dotnet publish -r ubuntu.18.04-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false
dotnet publish -r win7-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false
