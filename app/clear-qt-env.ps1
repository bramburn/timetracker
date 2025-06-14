#!/usr/bin/env pwsh
# Quick script to clear Qt environment variables

Write-Host "Clearing Qt Environment Variables..." -ForegroundColor Yellow

$qtVars = @(
    "QT_QPA_PLATFORM_PLUGIN_PATH",
    "QT_PLUGIN_PATH", 
    "QT_QPA_PLATFORM",
    "QT_AUTO_SCREEN_SCALE_FACTOR",
    "QT_SCALE_FACTOR",
    "QT_DEBUG_PLUGINS",
    "QT_LOGGING_RULES"
)

foreach ($var in $qtVars) {
    if (Get-Item "Env:$var" -ErrorAction SilentlyContinue) {
        Remove-Item "Env:$var" -ErrorAction SilentlyContinue
        Write-Host "Cleared: $var" -ForegroundColor Green
    }
}

Write-Host "Environment variables cleared. Now try running TimeTrackerApp.exe as Administrator." -ForegroundColor Green
