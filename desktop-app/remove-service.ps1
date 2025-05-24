#Requires -RunAsAdministrator

$serviceName = "TimeTracker.DesktopApp"
$displayName = "Internal Employee Activity Monitor"

Write-Host "Removing TimeTracker service..." -ForegroundColor Yellow

# Check if service exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($service) {
    # Stop the service if it's running
    if ($service.Status -eq 'Running') {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2  # Give it time to stop
    }

    # Remove the service
    Write-Host "Removing service..." -ForegroundColor Yellow
    try {
        $sc = New-Object System.ServiceProcess.ServiceController($serviceName)
        $sc.Delete()
        Write-Host "Service successfully removed." -ForegroundColor Green
    }
    catch {
        Write-Host "Error removing service: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "Service '$serviceName' not found." -ForegroundColor Yellow
}

# Clean up any remaining files
$installPath = "C:\Program Files\TimeTracker"
if (Test-Path $installPath) {
    Write-Host "Removing installation directory..." -ForegroundColor Yellow
    try {
        Remove-Item -Path $installPath -Recurse -Force
        Write-Host "Installation directory removed." -ForegroundColor Green
    }
    catch {
        Write-Host "Error removing installation directory: $_" -ForegroundColor Red
    }
}

Write-Host "`nCleanup complete!" -ForegroundColor Green 