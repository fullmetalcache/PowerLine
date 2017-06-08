@ECHO OFF

copy "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Microsoft.Build.Tasks.v4.0.dll" "C:\Program Files (x86)\MSBuild\14.0\Bin\amd64\Microsoft.Build.Tasks.v4.0.dll"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %~dp0\PowerLine.sln /t:rebuild /p:Configuration=Release /p:Platform=x64

ECHO ON