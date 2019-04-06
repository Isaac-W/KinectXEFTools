dotnet publish XEFExtract.sln -c Release -r win10-x64 || pause
"%SystemRoot%\explorer.exe" ".\bin\Release\netcoreapp2.1\win10-x64"