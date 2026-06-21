$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

try {
    Write-Host "Starting Futuristic Ctrl HUD with .NET..."
    Write-Host "Double-tap Ctrl to open the overlay, or run .\run.ps1 -Show to open it immediately."
    dotnet run --project .\FuturisticCtrlHud.csproj -- @args
} catch {
    Write-Host ""
    Write-Host "Startup failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Press Enter to close this window."
    Read-Host | Out-Null
    exit 1
}
