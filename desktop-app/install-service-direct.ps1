#Requires -RunAsAdministrator

# Direct TimeTracker Service Installation Script
# This script installs the TimeTracker service directly using the built executable

param(
    [string]$Action = "install",  # install, uninstall, start, stop, restart, status
    [string]$ServiceName = "TimeTracker.DesktopApp",
    [string]$DisplayName = "Internal Employee Activity Monitor",
    [string]$Description = "Monitors employee application usage and activity for productivity insights."
)

Write-Host "TimeTracker Service Management Script" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Yellow

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $scriptDir "TimeTracker.DesktopApp\bin\Release\net8.0-windows\win-x64\publish"
$exePath = Join-Path $publishDir "TimeTracker.DesktopApp.exe"
$installDir = "C:\Program Files\TimeTracker\DesktopApp"
$installExePath = Join-Path $installDir "TimeTracker.DesktopApp.exe"

function Test-ServiceExists {
    param([string]$Name)
    try {
        $service = Get-Service -Name $Name -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Show-ServiceStatus {
    Write-Host "`n=== SERVICE STATUS ===" -ForegroundColor Cyan
    
    if (Test-ServiceExists $ServiceName) {
        $service = Get-Service -Name $ServiceName
        Write-Host "Service Name: $($service.Name)" -ForegroundColor White
        Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor White
        Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })
        Write-Host "Start Type: $((Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'").StartMode)" -ForegroundColor White
        
        # Check if executable exists
        $exeExists = Test-Path $installExePath
        Write-Host "Executable exists: $exeExists" -ForegroundColor $(if ($exeExists) { 'Green' } else { 'Red' })
        
        if ($exeExists) {
            $fileInfo = Get-Item $installExePath
            Write-Host "Executable path: $($fileInfo.FullName)" -ForegroundColor White
            Write-Host "File size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor White
            Write-Host "Last modified: $($fileInfo.LastWriteTime)" -ForegroundColor White
        }
    } else {
        Write-Host "Service '$ServiceName' is not installed" -ForegroundColor Red
    }
    
    # Check source files
    Write-Host "`n=== SOURCE FILES ===" -ForegroundColor Cyan
    $sourceExists = Test-Path $exePath
    Write-Host "Source executable exists: $sourceExists" -ForegroundColor $(if ($sourceExists) { 'Green' } else { 'Red' })
    if ($sourceExists) {
        $sourceInfo = Get-Item $exePath
        Write-Host "Source path: $($sourceInfo.FullName)" -ForegroundColor White
        Write-Host "Source size: $([math]::Round($sourceInfo.Length / 1KB, 2)) KB" -ForegroundColor White
        Write-Host "Source modified: $($sourceInfo.LastWriteTime)" -ForegroundColor White
    }
}

function Install-TimeTrackerService {
    Write-Host "`n=== INSTALLING SERVICE ===" -ForegroundColor Cyan
    
    # Check if source executable exists
    if (-not (Test-Path $exePath)) {
        Write-Host "ERROR: Source executable not found at: $exePath" -ForegroundColor Red
        Write-Host "Please build the application first using: dotnet publish TimeTracker.DesktopApp -c Release -r win-x64 --self-contained" -ForegroundColor Yellow
        return $false
    }
    
    # Stop and remove existing service if it exists
    if (Test-ServiceExists $ServiceName) {
        Write-Host "Removing existing service..." -ForegroundColor Yellow
        Uninstall-TimeTrackerService
    }
    
    try {
        # Create installation directory
        Write-Host "Creating installation directory: $installDir" -ForegroundColor White
        if (-not (Test-Path $installDir)) {
            New-Item -Path $installDir -ItemType Directory -Force | Out-Null
        }
        
        # Copy all files from publish directory
        Write-Host "Copying application files..." -ForegroundColor White
        Copy-Item -Path "$publishDir\*" -Destination $installDir -Recurse -Force
        
        # Verify executable was copied
        if (-not (Test-Path $installExePath)) {
            throw "Failed to copy executable to installation directory"
        }
        
        # Create the service
        Write-Host "Creating Windows service..." -ForegroundColor White
        $params = @{
            Name = $ServiceName
            BinaryPathName = "`"$installExePath`""
            DisplayName = $DisplayName
            Description = $Description
            StartupType = "Automatic"
            Credential = $null  # Run as LocalSystem
        }
        
        New-Service @params | Out-Null
        
        Write-Host "Service '$ServiceName' installed successfully!" -ForegroundColor Green
        
        # Try to start the service
        Write-Host "Starting service..." -ForegroundColor White
        Start-Service -Name $ServiceName
        
        # Wait a moment and check status
        Start-Sleep -Seconds 3
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Host "Service started successfully!" -ForegroundColor Green
        } else {
            Write-Host "Service installed but failed to start. Status: $($service.Status)" -ForegroundColor Yellow
            Write-Host "Check the event log for details." -ForegroundColor Yellow
        }
        
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to install service: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Uninstall-TimeTrackerService {
    Write-Host "`n=== UNINSTALLING SERVICE ===" -ForegroundColor Cyan
    
    if (-not (Test-ServiceExists $ServiceName)) {
        Write-Host "Service '$ServiceName' is not installed" -ForegroundColor Yellow
        return $true
    }
    
    try {
        # Stop the service if running
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..." -ForegroundColor White
            Stop-Service -Name $ServiceName -Force
            
            # Wait for service to stop
            $timeout = 30
            $elapsed = 0
            while ($service.Status -ne 'Stopped' -and $elapsed -lt $timeout) {
                Start-Sleep -Seconds 1
                $elapsed++
                $service.Refresh()
            }
        }
        
        # Remove the service
        Write-Host "Removing service..." -ForegroundColor White
        Remove-Service -Name $ServiceName
        
        # Remove installation directory
        if (Test-Path $installDir) {
            Write-Host "Removing installation directory..." -ForegroundColor White
            Remove-Item -Path $installDir -Recurse -Force
        }
        
        Write-Host "Service '$ServiceName' uninstalled successfully!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to uninstall service: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Start-TimeTrackerService {
    Write-Host "`n=== STARTING SERVICE ===" -ForegroundColor Cyan
    
    if (-not (Test-ServiceExists $ServiceName)) {
        Write-Host "ERROR: Service '$ServiceName' is not installed" -ForegroundColor Red
        return $false
    }
    
    try {
        Start-Service -Name $ServiceName
        Write-Host "Service '$ServiceName' started successfully!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to start service: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Stop-TimeTrackerService {
    Write-Host "`n=== STOPPING SERVICE ===" -ForegroundColor Cyan
    
    if (-not (Test-ServiceExists $ServiceName)) {
        Write-Host "ERROR: Service '$ServiceName' is not installed" -ForegroundColor Red
        return $false
    }
    
    try {
        Stop-Service -Name $ServiceName -Force
        Write-Host "Service '$ServiceName' stopped successfully!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: Failed to stop service: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
switch ($Action.ToLower()) {
    "install" {
        Install-TimeTrackerService
    }
    "uninstall" {
        Uninstall-TimeTrackerService
    }
    "start" {
        Start-TimeTrackerService
    }
    "stop" {
        Stop-TimeTrackerService
    }
    "restart" {
        Stop-TimeTrackerService
        Start-Sleep -Seconds 2
        Start-TimeTrackerService
    }
    "status" {
        # Status is shown at the end anyway
    }
    default {
        Write-Host "Invalid action: $Action" -ForegroundColor Red
        Write-Host "Valid actions: install, uninstall, start, stop, restart, status" -ForegroundColor Yellow
        exit 1
    }
}

# Always show status at the end
Show-ServiceStatus

Write-Host "`nScript completed." -ForegroundColor Green
