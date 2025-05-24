#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enhanced installer and tester for TimeTracker Windows Service
.DESCRIPTION
    This script builds, installs, and tests the TimeTracker Windows Service with comprehensive error handling and diagnostics.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.
.PARAMETER SkipBuild
    Skip the build process and use existing binaries.
.PARAMETER TestOnly
    Only run tests without building or installing.
.PARAMETER Uninstall
    Uninstall the service and remove files.
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$TestOnly,
    [switch]$Uninstall
)

# Script configuration
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "TimeTracker.DesktopApp"
$installerDir = Join-Path $scriptDir "TimeTracker.Installer"
$outputDir = Join-Path $scriptDir "dist"
$serviceName = "TimeTracker.DesktopApp"
$logDir = Join-Path $env:ProgramData "TimeTracker\Logs"

function Write-Status {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "=== $Message ===" -ForegroundColor $Color
}

function Write-Error-Status {
    param([string]$Message)
    Write-Status $Message "Red"
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Stop-ServiceSafely {
    param([string]$ServiceName)
    
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq "Running") {
            Write-Host "Stopping service $ServiceName..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            
            # Wait for service to stop
            $timeout = 30
            $elapsed = 0
            while ($service.Status -ne "Stopped" -and $elapsed -lt $timeout) {
                Start-Sleep -Seconds 1
                $elapsed++
                $service.Refresh()
            }
            
            if ($service.Status -eq "Stopped") {
                Write-Host "Service stopped successfully" -ForegroundColor Green
            } else {
                Write-Host "Service did not stop within timeout" -ForegroundColor Yellow
            }
        }
    }
    catch {
        Write-Host "Error stopping service: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Uninstall-Service {
    Write-Status "Uninstalling TimeTracker Service"
    
    # Stop service first
    Stop-ServiceSafely $serviceName
    
    # Find and uninstall MSI
    try {
        $msiPath = Get-ChildItem -Path $outputDir -Filter "*.msi" | Select-Object -First 1
        if ($msiPath) {
            Write-Host "Uninstalling MSI: $($msiPath.FullName)" -ForegroundColor Yellow
            Start-Process -FilePath "msiexec.exe" -ArgumentList "/x", "`"$($msiPath.FullName)`"", "/quiet", "/l*v", "`"$outputDir\uninstall.log`"" -Wait
            Write-Host "MSI uninstalled" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "Error uninstalling MSI: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Clean up any remaining files
    $installPath = "${env:ProgramFiles(x86)}\TimeTracker"
    if (Test-Path $installPath) {
        try {
            Remove-Item -Path $installPath -Recurse -Force
            Write-Host "Removed installation directory" -ForegroundColor Green
        }
        catch {
            Write-Host "Could not remove installation directory: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    Write-Status "Uninstallation completed"
}

function Build-Application {
    Write-Status "Building TimeTracker Application"
    
    # Clean and restore
    Set-Location $projectDir
    dotnet clean --configuration $Configuration
    dotnet restore
    
    # Build and publish
    dotnet publish --configuration $Configuration --runtime win-x64 --self-contained false --output "bin\$Configuration\net8.0-windows\win-x64\publish"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build application"
    }
    
    Write-Host "Application built successfully" -ForegroundColor Green
}

function Build-Installer {
    Write-Status "Building WiX Installer"
    
    if (!(Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    
    $publishDir = Join-Path $projectDir "bin\$Configuration\net8.0-windows\win-x64\publish"
    $msiPath = Join-Path $outputDir "TimeTrackerInstaller-$Configuration.msi"
    $defineArg = "TimeTrackerAppPublishDir=$publishDir"
    
    Set-Location $installerDir
    
    & wix build Product.wxs -o $msiPath -d $defineArg
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build WiX installer"
    }
    
    Write-Host "Installer built successfully: $msiPath" -ForegroundColor Green
    return $msiPath
}

function Install-Service {
    param([string]$MsiPath)
    
    Write-Status "Installing TimeTracker Service"
    
    # Create detailed log file
    $installLogPath = Join-Path $outputDir "install-detailed.log"
    
    Write-Host "Installing MSI: $MsiPath" -ForegroundColor Yellow
    Write-Host "Install log: $installLogPath" -ForegroundColor Yellow
    
    # Install with verbose logging
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", "`"$MsiPath`"", "/quiet", "/l*v", "`"$installLogPath`"" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "MSI installed successfully" -ForegroundColor Green
    } else {
        Write-Error-Status "MSI installation failed with exit code: $($process.ExitCode)"
        
        # Show last few lines of install log
        if (Test-Path $installLogPath) {
            Write-Host "Last 20 lines of install log:" -ForegroundColor Yellow
            Get-Content $installLogPath | Select-Object -Last 20 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
        }
        
        throw "MSI installation failed"
    }
}

function Test-ServiceInstallation {
    Write-Status "Testing Service Installation"
    
    # Check if service exists
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if (!$service) {
        throw "Service '$serviceName' was not installed"
    }
    
    Write-Host "Service found: $($service.DisplayName)" -ForegroundColor Green
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Yellow
    
    # Check installation directory
    $installPath = "${env:ProgramFiles(x86)}\TimeTracker\DesktopApp"
    if (!(Test-Path $installPath)) {
        throw "Installation directory not found: $installPath"
    }
    
    $exePath = Join-Path $installPath "TimeTracker.DesktopApp.exe"
    if (!(Test-Path $exePath)) {
        throw "Service executable not found: $exePath"
    }
    
    Write-Host "Installation files verified" -ForegroundColor Green
    
    # Check logs directory
    if (Test-Path $logDir) {
        Write-Host "Logs directory exists: $logDir" -ForegroundColor Green
    } else {
        Write-Host "Logs directory not found: $logDir" -ForegroundColor Yellow
    }
    
    return $service
}

function Start-ServiceWithDiagnostics {
    param($Service)
    
    Write-Status "Starting Service with Diagnostics"
    
    try {
        Write-Host "Attempting to start service..." -ForegroundColor Yellow
        Start-Service -Name $serviceName -ErrorAction Stop
        
        # Wait for service to start
        $timeout = 60
        $elapsed = 0
        while ($Service.Status -ne "Running" -and $elapsed -lt $timeout) {
            Start-Sleep -Seconds 2
            $elapsed += 2
            $Service.Refresh()
            Write-Host "Waiting for service to start... ($elapsed/$timeout seconds)" -ForegroundColor Gray
        }
        
        if ($Service.Status -eq "Running") {
            Write-Host "Service started successfully!" -ForegroundColor Green
            
            # Check for log files
            Start-Sleep -Seconds 5
            if (Test-Path $logDir) {
                $logFiles = Get-ChildItem -Path $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
                if ($logFiles) {
                    Write-Host "Recent log entries:" -ForegroundColor Yellow
                    $latestLog = $logFiles[0]
                    Get-Content $latestLog.FullName | Select-Object -Last 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                }
            }
        } else {
            Write-Error-Status "Service failed to start within timeout"
            Show-ServiceDiagnostics
        }
    }
    catch {
        Write-Error-Status "Failed to start service: $($_.Exception.Message)"
        Show-ServiceDiagnostics
        throw
    }
}

function Show-ServiceDiagnostics {
    Write-Host "=== SERVICE DIAGNOSTICS ===" -ForegroundColor Yellow
    
    # Check Windows Event Log
    try {
        $events = Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='TimeTracker.DesktopApp'} -MaxEvents 5 -ErrorAction SilentlyContinue
        if ($events) {
            Write-Host "Recent Event Log entries:" -ForegroundColor Yellow
            $events | ForEach-Object { Write-Host "  [$($_.TimeCreated)] $($_.LevelDisplayName): $($_.Message)" -ForegroundColor Gray }
        }
    }
    catch {
        Write-Host "Could not read Event Log: $($_.Exception.Message)" -ForegroundColor Gray
    }
    
    # Check log files
    if (Test-Path $logDir) {
        $logFiles = Get-ChildItem -Path $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending
        if ($logFiles) {
            Write-Host "Available log files:" -ForegroundColor Yellow
            $logFiles | ForEach-Object { Write-Host "  $($_.Name) - $($_.LastWriteTime)" -ForegroundColor Gray }
            
            # Show content of latest log
            $latestLog = $logFiles[0]
            Write-Host "Content of latest log file ($($latestLog.Name)):" -ForegroundColor Yellow
            Get-Content $latestLog.FullName | Select-Object -Last 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
    }
}

# Main execution
try {
    Write-Status "TimeTracker Service Installation and Test Script"
    Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
    Write-Host "Script Directory: $scriptDir" -ForegroundColor Cyan
    
    # Check administrator privileges
    if (!(Test-Administrator)) {
        throw "This script must be run as Administrator"
    }
    
    if ($Uninstall) {
        Uninstall-Service
        return
    }
    
    if ($TestOnly) {
        $service = Test-ServiceInstallation
        Start-ServiceWithDiagnostics $service
        return
    }
    
    # Stop existing service if running
    Stop-ServiceSafely $serviceName
    
    if (!$SkipBuild) {
        Build-Application
        $msiPath = Build-Installer
    } else {
        $msiPath = Get-ChildItem -Path $outputDir -Filter "*.msi" | Select-Object -First 1 | Select-Object -ExpandProperty FullName
        if (!$msiPath) {
            throw "No MSI file found in $outputDir. Run without -SkipBuild to build first."
        }
    }
    
    Install-Service $msiPath
    $service = Test-ServiceInstallation
    Start-ServiceWithDiagnostics $service
    
    Write-Status "Installation and testing completed successfully!" "Green"
}
catch {
    Write-Error-Status "Script failed: $($_.Exception.Message)"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
finally {
    Set-Location $scriptDir
}
