@echo off
REM Publish CLI + Worker (framework-dependent) into one portable folder.
REM Result: publish\Cadence\ contains cadence.exe + Cadence.Worker.exe.
REM Copy that folder anywhere and run cadence.exe from any working directory.
setlocal
set OUT=%~dp0publish\Cadence
dotnet publish "%~dp0Cadence.Cli\Cadence.Cli.csproj" -c Release -o "%OUT%" --nologo
if errorlevel 1 exit /b 1
dotnet publish "%~dp0Cadence.Worker\Cadence.Worker.csproj" -c Release -o "%OUT%" --nologo
if errorlevel 1 exit /b 1
echo Published to %OUT%
