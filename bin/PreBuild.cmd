"%~dp0nuget.exe" restore "%~dp0..\PiServerLite.sln"
if not %errorlevel% == 0 exit %errorlevel%

"%~dp0msbuild.cmd" "%~dp0..\PiServerLite.Publishing\PiServerLite.Publishing.csproj" /t:Rebuild /p:Configuration="Debug" /p:Platform="Any CPU" /p:OutputPath="bin\Debug" /v:m
if not %errorlevel% == 0 exit %errorlevel%
