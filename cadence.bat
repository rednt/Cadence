@echo off
REM Prefer the published .exe when present, else run from source (dev).
if exist "%~dp0publish\Cadence\cadence.exe" (
  "%~dp0publish\Cadence\cadence.exe" %*
) else (
  dotnet run --project "%~dp0Cadence.Cli\Cadence.Cli.csproj" -- %*
)
