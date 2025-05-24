# Run TimeTracker directly without installing as service
# This is useful for testing and development

param(
    [switch]$Build,
    [switch]$Publish,
    [switch]$Console,
    [int]$TimeoutSeconds = 30
)

Write-Host "TimeTracker Direct Runner" -ForegroundColor Green

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "TimeTracker.DesktopApp\TimeTracker.DesktopApp.csproj"
$publishDir = Join-Path $scriptDir "TimeTracker.DesktopApp\bin\Release\net8.0-windows\win-x64\publish"
$exePath = Join-Path $publishDir "TimeTracker.DesktopApp.exe"

# Build if requested
if ($Build) {
    Write-Host "`n=== BUILDING APPLICATION ===" -ForegroundColor Cyan
    dotnet build $projectPath -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Publish if requested
if ($Publish -or $Build) {
    Write-Host "`n=== PUBLISHING APPLICATION ===" -ForegroundColor Cyan
    dotnet publish $projectPath -c Release -r win-x64 --self-contained
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!" -ForegroundColor Red
        exit 1
    }
}

# Check if executable exists
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at: $exePath" -ForegroundColor Red
    Write-Host "Run with -Publish to build the application first" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== EXECUTABLE INFO ===" -ForegroundColor Cyan
$fileInfo = Get-Item $exePath
Write-Host "Path: $($fileInfo.FullName)" -ForegroundColor White
Write-Host "Size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor White
Write-Host "Modified: $($fileInfo.LastWriteTime)" -ForegroundColor White

# Check dependencies
Write-Host "`n=== CHECKING DEPENDENCIES ===" -ForegroundColor Cyan
$appSettingsPath = Join-Path $publishDir "appsettings.json"
$appSettingsExists = Test-Path $appSettingsPath
Write-Host "appsettings.json: $appSettingsExists" -ForegroundColor $(if ($appSettingsExists) { 'Green' } else { 'Red' })

if ($appSettingsExists) {
    $config = Get-Content $appSettingsPath | ConvertFrom-Json
    Write-Host "Pipedream Endpoint: $($config.TimeTracker.PipedreamEndpointUrl)" -ForegroundColor White
    Write-Host "Database Path: $($config.TimeTracker.DatabasePath)" -ForegroundColor White
}

# Create logs directory
$logsDir = "$env:ProgramData\TimeTracker\Logs"
if (-not (Test-Path $logsDir)) {
    Write-Host "Creating logs directory: $logsDir" -ForegroundColor White
    New-Item -Path $logsDir -ItemType Directory -Force | Out-Null
}

Write-Host "`n=== RUNNING TIMETRACKER ===" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host "Logs will be written to: $logsDir" -ForegroundColor White

# Change to publish directory
Push-Location $publishDir

try {
    if ($Console) {
        # Run in console mode with output visible
        Write-Host "Starting in console mode..." -ForegroundColor White
        & .\TimeTracker.DesktopApp.exe
    } else {
        # Run with timeout and capture output
        Write-Host "Starting with $TimeoutSeconds second timeout..." -ForegroundColor White

        $process = Start-Process -FilePath ".\TimeTracker.DesktopApp.exe" -PassThru -NoNewWindow -RedirectStandardOutput "output.txt" -RedirectStandardError "error.txt"

        # Wait for specified timeout or until process exits
        $exited = $process.WaitForExit($TimeoutSeconds * 1000)

        if (-not $exited) {
            Write-Host "Stopping application after $TimeoutSeconds seconds..." -ForegroundColor Yellow
            $process.Kill()
            $process.WaitForExit()
        }

        Write-Host "Application stopped. Exit code: $($process.ExitCode)" -ForegroundColor White

        # Show output
        if (Test-Path "output.txt") {
            Write-Host "`n=== STANDARD OUTPUT ===" -ForegroundColor Cyan
            Get-Content "output.txt" | Select-Object -Last 20
        }

        if ((Test-Path "error.txt") -and ((Get-Item "error.txt").Length -gt 0)) {
            Write-Host "`n=== STANDARD ERROR ===" -ForegroundColor Red
            Get-Content "error.txt"
        }
    }
} finally {
    Pop-Location
}

# Check for log files
Write-Host "`n=== LOG FILES ===" -ForegroundColor Cyan
if (Test-Path $logsDir) {
    $logFiles = Get-ChildItem $logsDir | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    foreach ($logFile in $logFiles) {
        Write-Host "Log: $($logFile.Name) ($(Get-Date $logFile.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
        if ($logFile.Length -lt 10KB) {
            Write-Host "Last 10 lines:" -ForegroundColor Gray
            Get-Content $logFile.FullName | Select-Object -Last 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
        Write-Host ""
    }
} else {
    Write-Host "No log files found in $logsDir" -ForegroundColor Yellow
}

Write-Host "Done." -ForegroundColor Green
