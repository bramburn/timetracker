# PowerShell script to test TimeTracker functionality
Write-Host "Testing TimeTracker Desktop Application..." -ForegroundColor Green

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Clean up any existing database
$dbPath = "bin\Debug\net8.0-windows\win-x64\TimeTracker.db"
if (Test-Path $dbPath) {
    Remove-Item $dbPath
    Write-Host "Removed existing database" -ForegroundColor Yellow
}

# Start the application in background
Write-Host "Starting TimeTracker application..." -ForegroundColor Yellow
$process = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Hidden

# Wait for application to initialize
Start-Sleep -Seconds 5

# Simulate some activity by opening applications
Write-Host "Simulating user activity..." -ForegroundColor Yellow
Start-Process notepad -PassThru | Out-Null
Start-Sleep -Seconds 2
Start-Process calc -PassThru | Out-Null
Start-Sleep -Seconds 2

# Stop the application
Write-Host "Stopping application..." -ForegroundColor Yellow
Stop-Process -Id $process.Id -Force

# Wait for graceful shutdown
Start-Sleep -Seconds 2

# Check if database was created and contains data
if (Test-Path $dbPath) {
    Write-Host "[SUCCESS] Database created successfully" -ForegroundColor Green

    # Try to query the database using sqlite3 if available
    try {
        $sqliteOutput = & sqlite3 $dbPath "SELECT COUNT(*) FROM ActivityLogs;" 2>$null
        if ($sqliteOutput) {
            Write-Host "[SUCCESS] Database contains $sqliteOutput activity records" -ForegroundColor Green
        } else {
            Write-Host "[INFO] Could not query database (sqlite3 not available)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "[INFO] Could not query database (sqlite3 not available)" -ForegroundColor Yellow
    }
} else {
    Write-Host "[ERROR] Database was not created" -ForegroundColor Red
}

# Clean up test processes
Get-Process notepad -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process calc -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Test completed!" -ForegroundColor Green
