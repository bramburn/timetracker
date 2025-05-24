# PowerShell script to diagnose TimeTracker service installation and startup issues
param(
    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false
)

# Require administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires administrator privileges. Please run as Administrator."
    exit 1
}

Write-Host "TimeTracker Service Diagnostic Tool"
Write-Host "====================================="

# Check if service is installed
Write-Host "`n1. Checking service installation..."
$service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "[OK] Service is installed"
    Write-Host "  Name: $($service.Name)"
    Write-Host "  Display Name: $($service.DisplayName)"
    Write-Host "  Status: $($service.Status)"
    Write-Host "  Start Type: $($service.StartType)"
} else {
    Write-Host "[ERROR] Service is not installed"
    Write-Host "Run the installer first: .\install-service.ps1"
    exit 1
}

# Check service executable and files
Write-Host "`n2. Checking service files..."
$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='TimeTracker.DesktopApp'"

if ($serviceInfo) {
    $executablePath = $serviceInfo.PathName.Trim('"')
    Write-Host "  Executable Path: $executablePath"

    if (Test-Path $executablePath) {
        Write-Host "[OK] Service executable exists"

        # Check file version
        $fileInfo = Get-ItemProperty $executablePath
        Write-Host "  File Version: $($fileInfo.VersionInfo.FileVersion)"
        Write-Host "  Last Modified: $($fileInfo.LastWriteTime)"
    } else {
        Write-Host "[ERROR] Service executable not found: $executablePath"
    }

    # Check configuration file
    $configPath = Join-Path (Split-Path $executablePath) "appsettings.json"
    if (Test-Path $configPath) {
        Write-Host "[OK] Configuration file exists: $configPath"
    } else {
        Write-Host "[ERROR] Configuration file missing: $configPath"
    }
}

# Check Event Log entries
Write-Host "`n3. Checking recent Event Log entries..."
try {
    $events = Get-EventLog -LogName Application -Source "TimeTracker.DesktopApp" -Newest 10 -ErrorAction SilentlyContinue

    if ($events) {
        Write-Host "[OK] Found $($events.Count) recent event log entries"

        foreach ($event in $events | Select-Object -First 5) {
            Write-Host "  [$($event.TimeGenerated)] $($event.EntryType): $($event.Message.Substring(0, [Math]::Min(100, $event.Message.Length)))..."
        }
    } else {
        Write-Host "! No event log entries found for TimeTracker.DesktopApp"
    }
} catch {
    Write-Host "! Could not access event log: $_"
}

# Check Windows Service Event Log
Write-Host "`n4. Checking Windows Service events..."
try {
    $serviceEvents = Get-EventLog -LogName System -Source "Service Control Manager" -Newest 20 -ErrorAction SilentlyContinue |
                     Where-Object { $_.Message -like "*TimeTracker*" }

    if ($serviceEvents) {
        Write-Host "[OK] Found $($serviceEvents.Count) service control events"

        foreach ($event in $serviceEvents | Select-Object -First 3) {
            Write-Host "  [$($event.TimeGenerated)] $($event.EntryType): $($event.Message)"
        }
    } else {
        Write-Host "! No service control events found"
    }
} catch {
    Write-Host "! Could not access system event log: $_"
}

# Try to start the service if it's not running
Write-Host "`n5. Service startup test..."
$service = Get-Service -Name "TimeTracker.DesktopApp"

if ($service.Status -eq "Running") {
    Write-Host "Service is already running"
} else {
    Write-Host "Attempting to start service..."

    try {
        Start-Service -Name "TimeTracker.DesktopApp" -ErrorAction Stop
        Start-Sleep -Seconds 5

        $service = Get-Service -Name "TimeTracker.DesktopApp"
        if ($service.Status -eq "Running") {
            Write-Host "Service started successfully"
        } else {
            Write-Host "Service failed to start. Status: $($service.Status)"
        }
    } catch {
        Write-Host "Failed to start service: $_"
    }
}

# Check database file
Write-Host "`n6. Checking database..."
$installPath = Split-Path $executablePath
$dbPath = Join-Path $installPath "TimeTracker.db"

if (Test-Path $dbPath) {
    Write-Host "[OK] Database file exists: $dbPath"
    $dbInfo = Get-ItemProperty $dbPath
    Write-Host "  Size: $($dbInfo.Length) bytes"
    Write-Host "  Last Modified: $($dbInfo.LastWriteTime)"
} else {
    Write-Host "! Database file not found: $dbPath"
    Write-Host "  This is normal for first-time installation"
}

Write-Host "`n7. Recommendations:"

if ($service.Status -ne "Running") {
    Write-Host "• Service is not running. Check Event Log for startup errors"
    Write-Host "• Try starting manually: Start-Service -Name 'TimeTracker.DesktopApp'"
}

Write-Host "• Monitor Event Log: Get-EventLog -LogName Application -Source 'TimeTracker.DesktopApp' -Newest 5"
Write-Host "• Check service status: Get-Service -Name 'TimeTracker.DesktopApp'"

Write-Host ""
Write-Host "Diagnostic completed!"
