#Requires -RunAsAdministrator

# Direct TimeTracker Service Installation Script
# This script installs the TimeTracker service directly using the built executable

param(
    [string]$Action = "install",  # install, uninstall, start, stop, restart, status, build, monitor
    [string]$ServiceName = "TimeTracker.DesktopApp",
    [string]$DisplayName = "Internal Employee Activity Monitor",
    [string]$Description = "Monitors employee application usage and activity for productivity insights."
)

Write-Host "=== TimeTracker Service Management Script ===" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Yellow
Write-Host "Service Name: $ServiceName" -ForegroundColor White

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $scriptDir "TimeTracker.DesktopApp\bin\Release\net8.0-windows\win-x64\publish"
$exePath = Join-Path $publishDir "TimeTracker.DesktopApp.exe"
$installDir = "C:\Program Files\TimeTracker\DesktopApp"
$installExePath = Join-Path $installDir "TimeTracker.DesktopApp.exe"

function Test-ServiceExists {
    param([string]$Name)
    try {
        Get-Service -Name $Name -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Build-Application {
    Write-Host "`n=== BUILDING APPLICATION ===" -ForegroundColor Cyan

    $projectPath = Join-Path $scriptDir "TimeTracker.DesktopApp\TimeTracker.DesktopApp.csproj"

    if (-not (Test-Path $projectPath)) {
        Write-Host "ERROR: Project file not found at: $projectPath" -ForegroundColor Red
        return $false
    }

    try {
        Write-Host "Cleaning previous build..." -ForegroundColor White
        & dotnet clean $projectPath -c Release

        Write-Host "Building and publishing application..." -ForegroundColor White
        & dotnet publish $projectPath -c Release -r win-x64 --self-contained -p:PublishSingleFile=false

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Build completed successfully!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "ERROR: Build failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Monitor-Service {
    Write-Host "`n=== MONITORING SERVICE ===" -ForegroundColor Cyan

    if (-not (Test-ServiceExists $ServiceName)) {
        Write-Host "ERROR: Service '$ServiceName' is not installed" -ForegroundColor Red
        return
    }

    Write-Host "Monitoring service status and logs. Press Ctrl+C to stop..." -ForegroundColor Yellow
    Write-Host ""

    $logPath = "C:\ProgramData\TimeTracker\Logs"
    $pipedreamUrl = "https://eo2etvy0q1v9l11.m.pipedream.net"

    try {
        while ($true) {
            Clear-Host
            Write-Host "=== TimeTracker Service Monitor ===" -ForegroundColor Green
            Write-Host "Time: $(Get-Date)" -ForegroundColor White
            Write-Host ""

            # Service Status
            $service = Get-Service -Name $ServiceName
            Write-Host "Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })

            # Database Status
            $dbPath = "C:\Program Files\TimeTracker\DesktopApp\TimeTracker.db"
            if (Test-Path $dbPath) {
                $dbInfo = Get-Item $dbPath
                Write-Host "Database Size: $([math]::Round($dbInfo.Length / 1KB, 2)) KB" -ForegroundColor White
                Write-Host "Database Modified: $($dbInfo.LastWriteTime)" -ForegroundColor White
            }

            # Recent Logs
            Write-Host "`nRecent Log Entries:" -ForegroundColor Cyan
            if (Test-Path $logPath) {
                $logFiles = Get-ChildItem -Path $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                if ($logFiles) {
                    $recentLines = Get-Content $logFiles.FullName -Tail 5 -ErrorAction SilentlyContinue
                    foreach ($line in $recentLines) {
                        if ($line -match "ERROR|WARN") {
                            Write-Host $line -ForegroundColor Red
                        } elseif ($line -match "INFO.*batch|INFO.*submit") {
                            Write-Host $line -ForegroundColor Green
                        } else {
                            Write-Host $line -ForegroundColor Gray
                        }
                    }
                } else {
                    Write-Host "No log files found" -ForegroundColor Yellow
                }
            } else {
                Write-Host "Log directory not found: $logPath" -ForegroundColor Yellow
            }

            Write-Host "`nPipedream Endpoint: $pipedreamUrl" -ForegroundColor White
            Write-Host "Press Ctrl+C to stop monitoring..." -ForegroundColor Yellow

            Start-Sleep -Seconds 5
        }
    }
    catch [System.Management.Automation.PipelineStoppedException] {
        Write-Host "`nMonitoring stopped by user." -ForegroundColor Yellow
    }
}

function Show-ServiceStatus {
    Write-Host "`n=== SERVICE STATUS ===" -ForegroundColor Cyan

    if (Test-ServiceExists $ServiceName) {
        $service = Get-Service -Name $ServiceName
        Write-Host "Service Name: $($service.Name)" -ForegroundColor White
        Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor White
        Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })

        $wmiService = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
        Write-Host "Start Type: $($wmiService.StartMode)" -ForegroundColor White
        Write-Host "Service Account: $($wmiService.StartName)" -ForegroundColor White

        # Check if desktop interaction is enabled
        $serviceConfig = & sc.exe qc $ServiceName 2>$null
        if ($serviceConfig -match "TYPE.*INTERACTIVE") {
            Write-Host "Desktop Interaction: Enabled" -ForegroundColor Green
        } else {
            Write-Host "Desktop Interaction: Disabled" -ForegroundColor Red
        }

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

        # Create the service using sc.exe for better control
        Write-Host "Creating Windows service..." -ForegroundColor White
        & sc.exe create $ServiceName binPath= "`"$installExePath`"" DisplayName= "$DisplayName" start= auto obj= LocalSystem type= interact | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create service using sc.exe. Exit code: $LASTEXITCODE"
        }

        # Set service description
        & sc.exe description $ServiceName "$Description" | Out-Null

        # Configure service recovery options
        & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

        # Configure the service to interact with desktop (required for monitoring user activity)
        Write-Host "Configuring service for desktop interaction..." -ForegroundColor White
        & sc.exe config $ServiceName type= interact type= own | Out-Null

        Write-Host "Service '$ServiceName' installed successfully!" -ForegroundColor Green

        # Verify service configuration
        Write-Host "Verifying service configuration..." -ForegroundColor White
        $serviceConfig = & sc.exe qc $ServiceName
        if ($serviceConfig -match "TYPE.*INTERACTIVE") {
            Write-Host "Desktop interaction configured successfully!" -ForegroundColor Green
        } else {
            Write-Host "WARNING: Desktop interaction may not be configured properly" -ForegroundColor Yellow
        }

        # Try to start the service
        Write-Host "Starting service..." -ForegroundColor White
        Start-Service -Name $ServiceName

        # Wait a moment and check status
        Start-Sleep -Seconds 3
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Host "Service started successfully!" -ForegroundColor Green
            Write-Host "The service should now be able to monitor user activity." -ForegroundColor Green
            Write-Host "Use '.\install-service-direct.ps1 monitor' to watch the service in action." -ForegroundColor Yellow
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
        $serviceObj = Get-WmiObject -Class Win32_Service -Filter "Name='$ServiceName'"
        if ($serviceObj) {
            $result = $serviceObj.Delete()
            if ($result.ReturnValue -ne 0) {
                throw "Failed to remove service via WMI. ReturnValue: $($result.ReturnValue)"
            }
        }

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
    "build" {
        Build-Application
    }
    "install" {
        # Build first if needed
        if (-not (Test-Path $exePath)) {
            Write-Host "Executable not found. Building application first..." -ForegroundColor Yellow
            if (-not (Build-Application)) {
                Write-Host "Build failed. Cannot proceed with installation." -ForegroundColor Red
                exit 1
            }
        }
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
    "monitor" {
        Monitor-Service
    }
    "status" {
        # Status is shown at the end anyway
    }
    default {
        Write-Host "Invalid action: $Action" -ForegroundColor Red
        Write-Host "Valid actions: build, install, uninstall, start, stop, restart, monitor, status" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Examples:" -ForegroundColor White
        Write-Host "  .\install-service-direct.ps1 build" -ForegroundColor Gray
        Write-Host "  .\install-service-direct.ps1 install" -ForegroundColor Gray
        Write-Host "  .\install-service-direct.ps1 monitor" -ForegroundColor Gray
        exit 1
    }
}

# Always show status at the end
Show-ServiceStatus

Write-Host "`nScript completed." -ForegroundColor Green
