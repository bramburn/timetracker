# Enhanced TimeTracker Service Diagnostic Script
param(
    [Parameter(Mandatory=$false)]
    [string]$LogPath = "$env:TEMP\TimeTracker_Diagnostics.log"
)

Write-Host "TimeTracker Service Enhanced Diagnostics" -ForegroundColor Green
Write-Host "Log file: $LogPath" -ForegroundColor Yellow

# Function to write to both console and log
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Write-Host $logEntry
    Add-Content -Path $LogPath -Value $logEntry
}

# Initialize log file
"=== TimeTracker Service Diagnostics - $(Get-Date) ===" | Out-File -FilePath $LogPath -Encoding UTF8

try {
    Write-Log "Starting enhanced diagnostics..."

    # 1. System Information
    Write-Log "=== SYSTEM INFORMATION ===" "SECTION"
    Write-Log "Computer Name: $env:COMPUTERNAME"
    Write-Log "User Name: $env:USERNAME"
    Write-Log "OS Version: $((Get-CimInstance Win32_OperatingSystem).Caption)"
    Write-Log "OS Build: $((Get-CimInstance Win32_OperatingSystem).BuildNumber)"
    Write-Log "Architecture: $env:PROCESSOR_ARCHITECTURE"
    Write-Log "PowerShell Version: $($PSVersionTable.PSVersion)"

    # 2. .NET Runtime Check
    Write-Log "=== .NET RUNTIME CHECK ===" "SECTION"
    try {
        $dotnetVersion = & dotnet --version 2>&1
        Write-Log ".NET CLI Version: $dotnetVersion"

        $runtimes = & dotnet --list-runtimes 2>&1
        Write-Log ".NET Runtimes:"
        $runtimes | ForEach-Object { Write-Log "  $_" }
    }
    catch {
        Write-Log "Failed to check .NET runtime: $($_.Exception.Message)" "ERROR"
    }

    # 3. Service Status
    Write-Log "=== SERVICE STATUS ===" "SECTION"
    try {
        $service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        if ($service) {
            Write-Log "Service Name: $($service.Name)"
            Write-Log "Service Status: $($service.Status)"
            Write-Log "Service Start Type: $($service.StartType)"
            Write-Log "Service Display Name: $($service.DisplayName)"

            # Get service details from WMI
            $serviceWmi = Get-CimInstance -ClassName Win32_Service -Filter "Name='TimeTracker.DesktopApp'" -ErrorAction SilentlyContinue
            if ($serviceWmi) {
                Write-Log "Service Path: $($serviceWmi.PathName)"
                Write-Log "Service Account: $($serviceWmi.StartName)"
                Write-Log "Service State: $($serviceWmi.State)"
                Write-Log "Service Process ID: $($serviceWmi.ProcessId)"
            }
        } else {
            Write-Log "TimeTracker service not found" "WARNING"
        }
    }
    catch {
        Write-Log "Failed to check service status: $($_.Exception.Message)" "ERROR"
    }

    # 4. Process Information
    Write-Log "=== PROCESS INFORMATION ===" "SECTION"
    try {
        $processes = Get-Process -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        if ($processes) {
            foreach ($proc in $processes) {
                Write-Log "Process ID: $($proc.Id)"
                Write-Log "Process Name: $($proc.ProcessName)"
                Write-Log "Start Time: $($proc.StartTime)"
                Write-Log "Working Set: $($proc.WorkingSet64 / 1MB) MB"
                Write-Log "CPU Time: $($proc.TotalProcessorTime)"
            }
        } else {
            Write-Log "No TimeTracker processes found"
        }
    }
    catch {
        Write-Log "Failed to check process information: $($_.Exception.Message)" "ERROR"
    }

    # 5. File System Check
    Write-Log "=== FILE SYSTEM CHECK ===" "SECTION"
    $installPath = "C:\Program Files (x86)\TimeTracker\DesktopApp"
    $dataPath = "$env:ProgramData\TimeTracker"
    $logsPath = "$env:ProgramData\TimeTracker\Logs"

    # Check installation directory
    if (Test-Path $installPath) {
        Write-Log "Installation directory exists: $installPath"
        $files = Get-ChildItem -Path $installPath -ErrorAction SilentlyContinue
        Write-Log "Files in installation directory:"
        $files | ForEach-Object { Write-Log "  $($_.Name) ($($_.Length) bytes)" }
    } else {
        Write-Log "Installation directory not found: $installPath" "ERROR"
    }

    # Check data directory
    if (Test-Path $dataPath) {
        Write-Log "Data directory exists: $dataPath"
    } else {
        Write-Log "Data directory not found: $dataPath" "WARNING"
    }

    # Check logs directory
    if (Test-Path $logsPath) {
        Write-Log "Logs directory exists: $logsPath"
        $logFiles = Get-ChildItem -Path $logsPath -Filter "*.log" -ErrorAction SilentlyContinue
        Write-Log "Log files:"
        $logFiles | ForEach-Object { Write-Log "  $($_.Name) ($($_.Length) bytes, Modified: $($_.LastWriteTime))" }
    } else {
        Write-Log "Logs directory not found: $logsPath" "WARNING"
    }

    # 6. Event Log Check
    Write-Log "=== EVENT LOG CHECK ===" "SECTION"
    try {
        $events = Get-WinEvent -LogName Application -MaxEvents 50 -ErrorAction SilentlyContinue |
                  Where-Object { $_.ProviderName -like "*TimeTracker*" -or $_.Message -like "*TimeTracker*" }

        if ($events) {
            Write-Log "Recent TimeTracker events in Application log:"
            $events | ForEach-Object {
                Write-Log "  [$($_.TimeCreated)] [$($_.LevelDisplayName)] $($_.Message)"
            }
        } else {
            Write-Log "No TimeTracker events found in Application log"
        }
    }
    catch {
        Write-Log "Failed to check Event Log: $($_.Exception.Message)" "ERROR"
    }

    # 7. Network Connectivity Test
    Write-Log "=== NETWORK CONNECTIVITY ===" "SECTION"
    try {
        $testUrls = @(
            "https://google.com",
            "https://api.pipedream.com"
        )

        foreach ($url in $testUrls) {
            try {
                $response = Invoke-WebRequest -Uri $url -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
                Write-Log "Network test to ${url}: SUCCESS (Status: $($response.StatusCode))"
            }
            catch {
                Write-Log "Network test to ${url}: FAILED ($($_.Exception.Message))" "WARNING"
            }
        }
    }
    catch {
        Write-Log "Failed to test network connectivity: $($_.Exception.Message)" "ERROR"
    }

    # 8. Permissions Check
    Write-Log "=== PERMISSIONS CHECK ===" "SECTION"
    try {
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object System.Security.Principal.WindowsPrincipal($currentUser)
        $isAdmin = $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)

        Write-Log "Current User: $($currentUser.Name)"
        Write-Log "Is Administrator: $isAdmin"
        Write-Log "Authentication Type: $($currentUser.AuthenticationType)"
    }
    catch {
        Write-Log "Failed to check permissions: $($_.Exception.Message)" "ERROR"
    }

    Write-Log "Diagnostics completed successfully"
    Write-Host "Diagnostics completed. Log saved to: $LogPath" -ForegroundColor Green

} catch {
    Write-Log "Diagnostics failed: $($_.Exception.Message)" "ERROR"
    Write-Host "Diagnostics failed. Check log file: $LogPath" -ForegroundColor Red
}

# Offer to open log file
$response = Read-Host "Would you like to open the log file? (y/n)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Start-Process notepad.exe -ArgumentList $LogPath
}
