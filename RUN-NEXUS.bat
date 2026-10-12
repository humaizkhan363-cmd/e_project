@echo off
rem Starts the Nexus Service Marketing System and opens it in the browser.
rem Requires the .NET 8 SDK. The database is created automatically on the first run.
cd /d "%~dp0"
echo Starting Nexus Service Marketing System... (first start takes 1-2 minutes)
start "" cmd /c "timeout /t 25 >nul & start http://localhost:5294"
dotnet run --launch-profile http
pause
