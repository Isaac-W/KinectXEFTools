dotnet publish XEFExtract.sln -c Release -r linux-x64 || pause
"%SystemRoot%\explorer.exe" ".\bin\Release\netcoreapp2.1\linux-x64"