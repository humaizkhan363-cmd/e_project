@echo off
REM Creates / updates the NexusServiceMarketingDb database in SQL Server using the EF Core migrations.
REM Run this file from inside the Database folder (or double-click it). Requires the .NET 8 SDK.
cd /d "%~dp0.."
echo Installing / updating the EF Core command line tool...
dotnet tool update --global dotnet-ef --version 8.* || dotnet tool install --global dotnet-ef --version 8.*
echo.
echo Applying migrations to SQL Server...
dotnet ef database update
echo.
echo Done. Start the website with:  dotnet run
pause
