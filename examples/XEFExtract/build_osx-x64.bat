dotnet publish XEFExtract.sln -c Release -r osx-x64 || pause
"%SystemRoot%\explorer.exe" ".\bin\Release\netcoreapp2.1\osx-x64"