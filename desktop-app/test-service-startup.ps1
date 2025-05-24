# Test TimeTracker service startup manually
Write-Host "Testing TimeTracker service startup..."

$publishPath = "TimeTracker.DesktopApp\bin\Release\net8.0-windows\win-x64\publish"
$exePath = Join-Path $publishPath "TimeTracker.DesktopApp.exe"

Write-Host "Executable path: $exePath"
Write-Host "Executable exists: $(Test-Path $exePath)"

if (Test-Path $exePath) {
    Write-Host "Starting executable with verbose output..."
    
    # Set environment variables for debugging
    $env:DOTNET_ENVIRONMENT = "Development"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    
    # Change to the publish directory
    Push-Location $publishPath
    
    try {
        # Run the executable and capture output
        Write-Host "Running: .\TimeTracker.DesktopApp.exe"
        $process = Start-Process -FilePath ".\TimeTracker.DesktopApp.exe" -Wait -PassThru -NoNewWindow -RedirectStandardOutput "output.txt" -RedirectStandardError "error.txt"
        
        Write-Host "Process exit code: $($process.ExitCode)"
        
        if (Test-Path "output.txt") {
            Write-Host "=== STANDARD OUTPUT ==="
            Get-Content "output.txt"
        }
        
        if (Test-Path "error.txt") {
            Write-Host "=== STANDARD ERROR ==="
            Get-Content "error.txt"
        }
        
        # Check for log files
        $logDir = "$env:ProgramData\TimeTracker\Logs"
        if (Test-Path $logDir) {
            Write-Host "=== LOG FILES ==="
            Get-ChildItem $logDir | Sort-Object LastWriteTime -Descending | Select-Object -First 3 | ForEach-Object {
                Write-Host "Log file: $($_.Name)"
                if ($_.Length -lt 10KB) {
                    Get-Content $_.FullName | Select-Object -Last 20
                }
            }
        }
        
    } finally {
        Pop-Location
    }
} else {
    Write-Host "Executable not found!"
}
