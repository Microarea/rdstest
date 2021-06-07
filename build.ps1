Remove-Item -Recurse -Force out
dotnet publish -c release -r win-x64 -o out/.
# dotnet publish -c release -r linux-x64 -o out/.
Copy-Item .\rdstest.json out/
Set-Location out
Compress-Archive rdstest.exe,rdstest.json rdstest.zip
Set-Location ..
