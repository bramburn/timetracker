#!/usr/bin/env pwsh
# =============================================================================
# Qt Platform Plugin Diagnostic and Fix Script
# =============================================================================
# This script helps diagnose and fix Qt platform plugin initialization errors
# for the TimeTracker application.

param(
    [string]$BuildConfig = "Debug",
    [switch]$Verbose,
    [switch]$Clean
)

Write-Host "Qt Platform Plugin Diagnostic and Fix Script" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Configuration
$QtPath = "C:/Qt/6.9.0/msvc2022_64"
$AppPath = "$PSScriptRoot/build/bin/$BuildConfig"
$AppExe = "$AppPath/TimeTrackerApp.exe"

# Function to check if a file exists
function Test-FileExists {
    param([string]$Path, [string]$Description)
    
    if (Test-Path $Path) {
        Write-Host "✓ $Description found: $Path" -ForegroundColor Green
        return $true
    } else {
        Write-Host "✗ $Description missing: $Path" -ForegroundColor Red
        return $false
    }
}

# Function to check environment variables
function Test-QtEnvironment {
    Write-Host "`nChecking Qt Environment Variables..." -ForegroundColor Yellow

    $qtVars = @(
        "QT_QPA_PLATFORM_PLUGIN_PATH",
        "QT_PLUGIN_PATH",
        "QT_QPA_PLATFORM",
        "QT_AUTO_SCREEN_SCALE_FACTOR",
        "QT_SCALE_FACTOR"
    )

    $hasConflicts = $false
    foreach ($var in $qtVars) {
        # Check both user and machine environment variables
        $userValue = [Environment]::GetEnvironmentVariable($var, "User")
        $machineValue = [Environment]::GetEnvironmentVariable($var, "Machine")
        $processValue = [Environment]::GetEnvironmentVariable($var, "Process")

        if ($userValue) {
            Write-Host "⚠ User environment variable $var is set to: $userValue" -ForegroundColor Yellow
            $hasConflicts = $true
        }
        if ($machineValue) {
            Write-Host "⚠ Machine environment variable $var is set to: $machineValue" -ForegroundColor Yellow
            $hasConflicts = $true
        }
        if ($processValue -and $processValue -ne $userValue -and $processValue -ne $machineValue) {
            Write-Host "⚠ Process environment variable $var is set to: $processValue" -ForegroundColor Yellow
            $hasConflicts = $true
        }
    }

    if (-not $hasConflicts) {
        Write-Host "✓ No conflicting Qt environment variables found" -ForegroundColor Green
    }

    return $hasConflicts
}

# Function to check PATH for Qt installations
function Test-QtInPath {
    Write-Host "`nChecking PATH for Qt installations..." -ForegroundColor Yellow
    
    $pathEntries = $env:PATH -split ";"
    $qtPaths = $pathEntries | Where-Object { $_ -like "*Qt*" -or $_ -like "*qt*" }
    
    if ($qtPaths) {
        Write-Host "Found Qt-related PATH entries:" -ForegroundColor Yellow
        foreach ($path in $qtPaths) {
            Write-Host "  - $path" -ForegroundColor Cyan
        }
    } else {
        Write-Host "✓ No Qt-related PATH entries found" -ForegroundColor Green
    }
}

# Function to verify Qt installation
function Test-QtInstallation {
    Write-Host "`nVerifying Qt Installation..." -ForegroundColor Yellow
    
    $qtBinPath = "$QtPath/bin"
    $qtPluginsPath = "$QtPath/plugins"
    
    Test-FileExists $QtPath "Qt installation directory"
    Test-FileExists $qtBinPath "Qt bin directory"
    Test-FileExists $qtPluginsPath "Qt plugins directory"
    Test-FileExists "$qtPluginsPath/platforms" "Qt platforms plugin directory"
    Test-FileExists "$qtPluginsPath/platforms/qwindows.dll" "qwindows.dll platform plugin"
}

# Function to verify application deployment
function Test-AppDeployment {
    Write-Host "`nVerifying Application Deployment..." -ForegroundColor Yellow
    
    Test-FileExists $AppExe "TimeTrackerApp executable"
    Test-FileExists "$AppPath/Qt6Core$($BuildConfig -eq 'Debug' ? 'd' : '').dll" "Qt6Core DLL"
    Test-FileExists "$AppPath/Qt6Gui$($BuildConfig -eq 'Debug' ? 'd' : '').dll" "Qt6Gui DLL"
    Test-FileExists "$AppPath/Qt6Widgets$($BuildConfig -eq 'Debug' ? 'd' : '').dll" "Qt6Widgets DLL"
    Test-FileExists "$AppPath/platforms" "Platforms plugin directory"
    Test-FileExists "$AppPath/platforms/qwindows.dll" "qwindows.dll in app directory"
}

# Function to fix platform plugins
function Repair-PlatformPlugins {
    Write-Host "`nRepairing Platform Plugins..." -ForegroundColor Yellow
    
    # Create platforms directory if it doesn't exist
    $platformsDir = "$AppPath/platforms"
    if (-not (Test-Path $platformsDir)) {
        New-Item -ItemType Directory -Path $platformsDir -Force
        Write-Host "Created platforms directory: $platformsDir" -ForegroundColor Green
    }
    
    # Copy platform plugins
    $sourcePlugins = "$QtPath/plugins/platforms"
    $plugins = @("qwindows.dll", "qminimal.dll", "qoffscreen.dll")
    
    foreach ($plugin in $plugins) {
        $source = "$sourcePlugins/$plugin"
        $dest = "$platformsDir/$plugin"
        
        if (Test-Path $source) {
            Copy-Item $source $dest -Force
            Write-Host "Copied $plugin to platforms directory" -ForegroundColor Green
        } else {
            Write-Host "Warning: $plugin not found in Qt installation" -ForegroundColor Yellow
        }
    }
}

# Function to clean environment
function Clear-QtEnvironment {
    Write-Host "`nCleaning Qt Environment Variables..." -ForegroundColor Yellow

    $qtVars = @(
        "QT_QPA_PLATFORM_PLUGIN_PATH",
        "QT_PLUGIN_PATH",
        "QT_QPA_PLATFORM",
        "QT_AUTO_SCREEN_SCALE_FACTOR",
        "QT_SCALE_FACTOR"
    )

    foreach ($var in $qtVars) {
        # Clear from all scopes
        $userValue = [Environment]::GetEnvironmentVariable($var, "User")
        $machineValue = [Environment]::GetEnvironmentVariable($var, "Machine")
        $processValue = [Environment]::GetEnvironmentVariable($var, "Process")

        if ($processValue) {
            [Environment]::SetEnvironmentVariable($var, $null, "Process")
            Write-Host "Cleared process environment variable: $var" -ForegroundColor Green
        }

        if ($userValue) {
            Write-Host "⚠ User environment variable $var found. You may need to clear it manually from System Properties." -ForegroundColor Yellow
        }

        if ($machineValue) {
            Write-Host "⚠ Machine environment variable $var found. You may need administrator privileges to clear it." -ForegroundColor Yellow
        }
    }

    # Also clear any Qt-related PATH entries for this session
    $currentPath = $env:PATH
    $pathEntries = $currentPath -split ";"
    $cleanedPath = $pathEntries | Where-Object { $_ -notlike "*Qt*" -and $_ -notlike "*qt*" }

    if ($pathEntries.Count -ne $cleanedPath.Count) {
        $env:PATH = $cleanedPath -join ";"
        Write-Host "Removed Qt-related PATH entries for this session" -ForegroundColor Green
    }
}

# Function to test the application
function Test-Application {
    Write-Host "`nTesting Application..." -ForegroundColor Yellow
    
    if (-not (Test-Path $AppExe)) {
        Write-Host "✗ Application executable not found: $AppExe" -ForegroundColor Red
        return $false
    }
    
    Write-Host "Starting application with debug output..." -ForegroundColor Cyan
    
    # Set debug environment variables
    $env:QT_DEBUG_PLUGINS = "1"
    $env:QT_LOGGING_RULES = "qt.qpa.plugin.debug=true"
    
    try {
        # Start the application and capture output
        $process = Start-Process -FilePath $AppExe -WorkingDirectory $AppPath -PassThru -WindowStyle Hidden
        Start-Sleep -Seconds 3
        
        if ($process.HasExited) {
            Write-Host "✗ Application exited with code: $($process.ExitCode)" -ForegroundColor Red
            return $false
        } else {
            Write-Host "✓ Application started successfully" -ForegroundColor Green
            $process.Kill()
            return $true
        }
    } catch {
        Write-Host "✗ Failed to start application: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    } finally {
        # Clean up debug environment variables
        Remove-Item Env:QT_DEBUG_PLUGINS -ErrorAction SilentlyContinue
        Remove-Item Env:QT_LOGGING_RULES -ErrorAction SilentlyContinue
    }
}

# Main execution
try {
    # Run diagnostics
    Test-QtEnvironment
    Test-QtInPath
    Test-QtInstallation
    Test-AppDeployment
    
    # Clean environment if requested
    if ($Clean) {
        Clear-QtEnvironment
    }
    
    # Repair platform plugins
    Repair-PlatformPlugins
    
    # Test the application
    $success = Test-Application
    
    if ($success) {
        Write-Host "`n✓ Qt platform plugin issue has been resolved!" -ForegroundColor Green
    } else {
        Write-Host "`n✗ Qt platform plugin issue persists. Check the output above for details." -ForegroundColor Red
        Write-Host "Try running the script with -Clean parameter to clear environment variables." -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`nScript completed." -ForegroundColor Green
