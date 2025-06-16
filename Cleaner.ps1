dotnet clean
dotnet nuget locals all --clear
Get-ChildItem -Recurse -Force -Directory -Include bin,obj | Remove-Item -Recurse -Force
Remove-Item -Recurse -Force .vs/,TestResults/
dotnet restore
dotnet build