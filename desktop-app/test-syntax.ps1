# Test syntax for the problematic section
$service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue

if ($service.Status -eq "Running") {
    Write-Host "Service is running"
} else {
    Write-Host "Attempting to start service..."
    
    try {
        Start-Service -Name "TimeTracker.DesktopApp" -ErrorAction Stop
        
        $service = Get-Service -Name "TimeTracker.DesktopApp"
        if ($service.Status -eq "Running") {
            Write-Host "Service started successfully"
        } else {
            Write-Host "Service failed to start"
        }
    } catch {
        Write-Host "Failed to start service: $_"
    }
}

Write-Host "Test completed"
