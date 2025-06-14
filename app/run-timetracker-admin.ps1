#!/usr/bin/env pwsh
# =============================================================================
# Run TimeTracker with Administrator Privileges and Clean Environment
# =============================================================================
# This script clears Qt environment variables and runs TimeTracker as admin

param(
    [string]$BuildConfig = "Debug"
)

Write-Host "TimeTracker Admin Launcher" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Clear Qt environment variables
function Clear-QtEnvironment {
    Write-Host "`nClearing Qt environment variables..." -ForegroundColor Yellow
    
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
        if ($env:$var) {
            Remove-Item "Env:$var" -ErrorAction SilentlyContinue
            Write-Host "Cleared: $var" -ForegroundColor Green
        }
    }
}

# Main execution
try {
    $AppPath = "$PSScriptRoot/build/bin/$BuildConfig"
    $AppExe = "$AppPath/TimeTrackerApp.exe"
    
    # Check if application exists
    if (-not (Test-Path $AppExe)) {
        Write-Host "✗ Application not found: $AppExe" -ForegroundColor Red
        Write-Host "Please build the application first:" -ForegroundColor Yellow
        Write-Host "  cmake --build build --config $BuildConfig" -ForegroundColor Cyan
        exit 1
    }
    
    # Clear environment variables
    Clear-QtEnvironment
    
    # Check if running as administrator
    if (-not (Test-Administrator)) {
        Write-Host "`n⚠ Not running as administrator. TimeTracker requires admin privileges for Windows hooks." -ForegroundColor Yellow
        Write-Host "Attempting to restart as administrator..." -ForegroundColor Yellow
        
        # Restart as administrator
        $arguments = "-ExecutionPolicy Bypass -File `"$PSCommandPath`" -BuildConfig $BuildConfig"
        Start-Process powershell -Verb RunAs -ArgumentList $arguments
        exit 0
    }
    
    Write-Host "`n✓ Running as administrator" -ForegroundColor Green
    Write-Host "Starting TimeTracker application..." -ForegroundColor Cyan
    
    # Change to application directory
    Push-Location $AppPath
    
    try {
        # Start the application
        Write-Host "Launching: $AppExe" -ForegroundColor Cyan
        Start-Process -FilePath ".\TimeTrackerApp.exe" -WorkingDirectory $AppPath -Wait
        
        Write-Host "`n✓ TimeTracker application closed normally" -ForegroundColor Green
        
    } catch {
        Write-Host "`n✗ Error starting TimeTracker: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
        Write-Host "1. Run: .\fix-qt-platform-plugins.ps1 -Clean" -ForegroundColor Cyan
        Write-Host "2. Rebuild: cmake --build build --config $BuildConfig" -ForegroundColor Cyan
        Write-Host "3. Check Qt installation at: C:/Qt/6.9.0/msvc2022_64/" -ForegroundColor Cyan
    } finally {
        Pop-Location
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`nScript completed." -ForegroundColor Green
Read-Host "Press Enter to exit"
