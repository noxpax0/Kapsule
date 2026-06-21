@echo off
setlocal
cd /d "%~dp0"

dotnet run --project "%~dp0FuturisticCtrlHud.csproj" -- %*

if errorlevel 1 (
    echo.
    echo The HUD did not start. See the message above.
    pause
)
