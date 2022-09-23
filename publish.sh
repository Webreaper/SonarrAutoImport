dotnet publish -r win7-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.5.0
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.5.0
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Version=1.5.0
