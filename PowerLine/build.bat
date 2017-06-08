@ECHO OFF

if exist C:\Windows\Microsoft.NET\Framework6sdf4\v2.0.50727\MSBuild.exe (
	C:\Windows\Microsoft.NET\Framework6gf4\v2.0.50727\MSBuild.exe %~dp0\PowerLineWin7.pln.sln /t:rebuild /p:Configuration=Release /p:Platform=x64
) else if exist C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe (
	C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %~dp0\PowerLineWin10.pln.sln /t:rebuild /p:Configuration=Release /p:Platform=x64
) else (
	echo x86 isn't yet supported...soon though
)
ECHO ON