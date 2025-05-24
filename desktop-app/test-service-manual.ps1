# Manual TimeTracker Service Test Script
param(
    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false,
    
    [Parameter(Mandatory=$false)]
    [int]$TimeoutSeconds = 60
)

Write-Host "TimeTracker Service Manual Test" -ForegroundColor Green

# Function to write verbose output
function Write-Verbose-Custom {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "[VERBOSE] $Message" -ForegroundColor Gray
    }
}

# Function to test service startup manually
function Test-ServiceStartup {
    Write-Host "Testing service startup manually..." -ForegroundColor Yellow
    
    try {
        # Get service information
        $service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-Host "ERROR: TimeTracker service not found. Please install the service first." -ForegroundColor Red
            return $false
        }

        Write-Verbose-Custom "Service found: $($service.Name)"
        Write-Verbose-Custom "Current status: $($service.Status)"

        # Stop service if running
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping existing service..." -ForegroundColor Yellow
            Stop-Service -Name "TimeTracker.DesktopApp" -Force -ErrorAction Stop
            
            # Wait for service to stop
            $stopTimeout = 30
            $stopTimer = 0
            do {
                Start-Sleep -Seconds 1
                $stopTimer++
                $service.Refresh()
                Write-Verbose-Custom "Waiting for service to stop... ($stopTimer/$stopTimeout)"
            } while ($service.Status -ne 'Stopped' -and $stopTimer -lt $stopTimeout)
            
            if ($service.Status -ne 'Stopped') {
                Write-Host "WARNING: Service did not stop within timeout" -ForegroundColor Yellow
            } else {
                Write-Host "Service stopped successfully" -ForegroundColor Green
            }
        }

        # Start service
        Write-Host "Starting TimeTracker service..." -ForegroundColor Yellow
        Start-Service -Name "TimeTracker.DesktopApp" -ErrorAction Stop
        
        # Monitor service startup
        $startTimer = 0
        do {
            Start-Sleep -Seconds 2
            $startTimer += 2
            $service.Refresh()
            Write-Verbose-Custom "Service status: $($service.Status) (elapsed: $startTimer seconds)"
            
            # Check for failure
            if ($service.Status -eq 'Stopped' -and $startTimer -gt 5) {
                Write-Host "ERROR: Service failed to start (stopped after $startTimer seconds)" -ForegroundColor Red
                return $false
            }
            
        } while ($service.Status -ne 'Running' -and $startTimer -lt $TimeoutSeconds)

        if ($service.Status -eq 'Running') {
            Write-Host "SUCCESS: Service started successfully in $startTimer seconds" -ForegroundColor Green
            
            # Get process information
            $serviceWmi = Get-CimInstance -ClassName Win32_Service -Filter "Name='TimeTracker.DesktopApp'"
            if ($serviceWmi -and $serviceWmi.ProcessId -gt 0) {
                $process = Get-Process -Id $serviceWmi.ProcessId -ErrorAction SilentlyContinue
                if ($process) {
                    Write-Host "Service Process ID: $($process.Id)" -ForegroundColor Green
                    Write-Host "Service Memory Usage: $([math]::Round($process.WorkingSet64 / 1MB, 2)) MB" -ForegroundColor Green
                    Write-Host "Service Start Time: $($process.StartTime)" -ForegroundColor Green
                }
            }
            
            return $true
        } else {
            Write-Host "ERROR: Service failed to start within $TimeoutSeconds seconds" -ForegroundColor Red
            return $false
        }

    } catch {
        Write-Host "ERROR: Failed to test service startup: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to check recent event logs
function Check-EventLogs {
    Write-Host "Checking recent event logs..." -ForegroundColor Yellow
    
    try {
        $events = Get-WinEvent -LogName Application -MaxEvents 20 -ErrorAction SilentlyContinue | 
                  Where-Object { 
                      $_.ProviderName -like "*TimeTracker*" -or 
                      $_.Message -like "*TimeTracker*" -or
                      ($_.ProviderName -eq "Service Control Manager" -and $_.Message -like "*TimeTracker*")
                  } | 
                  Sort-Object TimeCreated -Descending

        if ($events) {
            Write-Host "Recent TimeTracker-related events:" -ForegroundColor Yellow
            foreach ($event in $events) {
                $level = switch ($event.Level) {
                    1 { "CRITICAL" }
                    2 { "ERROR" }
                    3 { "WARNING" }
                    4 { "INFO" }
                    default { "UNKNOWN" }
                }
                
                $color = switch ($level) {
                    "CRITICAL" { "Magenta" }
                    "ERROR" { "Red" }
                    "WARNING" { "Yellow" }
                    "INFO" { "Green" }
                    default { "White" }
                }
                
                Write-Host "  [$($event.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss'))] [$level] $($event.Message)" -ForegroundColor $color
            }
        } else {
            Write-Host "No recent TimeTracker events found in Application log" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "ERROR: Failed to check event logs: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to check log files
function Check-LogFiles {
    Write-Host "Checking log files..." -ForegroundColor Yellow
    
    $logsPath = "$env:ProgramData\TimeTracker\Logs"
    
    if (Test-Path $logsPath) {
        $logFiles = Get-ChildItem -Path $logsPath -Filter "*.log" -ErrorAction SilentlyContinue | 
                   Sort-Object LastWriteTime -Descending
        
        if ($logFiles) {
            Write-Host "Recent log files:" -ForegroundColor Yellow
            foreach ($file in $logFiles | Select-Object -First 5) {
                Write-Host "  $($file.Name) - $($file.LastWriteTime) ($([math]::Round($file.Length / 1KB, 2)) KB)" -ForegroundColor Cyan
                
                # Show last few lines of the most recent log
                if ($file -eq $logFiles[0]) {
                    try {
                        $lastLines = Get-Content -Path $file.FullName -Tail 10 -ErrorAction SilentlyContinue
                        if ($lastLines) {
                            Write-Host "    Last 10 lines:" -ForegroundColor Gray
                            foreach ($line in $lastLines) {
                                Write-Host "      $line" -ForegroundColor Gray
                            }
                        }
                    } catch {
                        Write-Host "    Could not read log file: $($_.Exception.Message)" -ForegroundColor Red
                    }
                }
            }
        } else {
            Write-Host "No log files found in $logsPath" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Logs directory not found: $logsPath" -ForegroundColor Yellow
    }
}

# Main execution
try {
    Write-Host "Starting manual service test with timeout of $TimeoutSeconds seconds..." -ForegroundColor Cyan
    
    # Test service startup
    $startupSuccess = Test-ServiceStartup
    
    if ($startupSuccess) {
        Write-Host "Waiting 10 seconds for service to stabilize..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # Check if service is still running
        $service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq 'Running') {
            Write-Host "SUCCESS: Service is running and stable" -ForegroundColor Green
        } else {
            Write-Host "WARNING: Service stopped after initial startup" -ForegroundColor Yellow
            $startupSuccess = $false
        }
    }
    
    # Always check logs for diagnostic information
    Write-Host ""
    Check-EventLogs
    Write-Host ""
    Check-LogFiles
    
    # Summary
    Write-Host ""
    Write-Host "=== TEST SUMMARY ===" -ForegroundColor Cyan
    if ($startupSuccess) {
        Write-Host "RESULT: Service test PASSED" -ForegroundColor Green
        Write-Host "The TimeTracker service started successfully and appears to be working." -ForegroundColor Green
    } else {
        Write-Host "RESULT: Service test FAILED" -ForegroundColor Red
        Write-Host "The TimeTracker service failed to start or stopped unexpectedly." -ForegroundColor Red
        Write-Host "Check the event logs and log files above for error details." -ForegroundColor Red
    }

} catch {
    Write-Host "FATAL ERROR: Test script failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Offer to run diagnostics
Write-Host ""
$response = Read-Host "Would you like to run enhanced diagnostics? (y/n)"
if ($response -eq 'y' -or $response -eq 'Y') {
    $diagnosticsScript = Join-Path $PSScriptRoot "diagnose-service-enhanced.ps1"
    if (Test-Path $diagnosticsScript) {
        & $diagnosticsScript
    } else {
        Write-Host "Enhanced diagnostics script not found: $diagnosticsScript" -ForegroundColor Red
    }
}
