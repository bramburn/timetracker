# Check TimeTracker Service Status
param([string]$ServiceName = "TimeTracker.DesktopApp")

Write-Host "TimeTracker Service Status Check" -ForegroundColor Green

# Check if service exists
try {
    $service = Get-Service -Name $ServiceName -ErrorAction Stop
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green
    Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor White
} catch {
    Write-Host "Service is not installed" -ForegroundColor Red
}

# Check source files
$publishDir = "TimeTracker.DesktopApp\bin\Release\net8.0-windows\win-x64\publish"
$sourceExe = Join-Path $publishDir "TimeTracker.DesktopApp.exe"
$sourceExists = Test-Path $sourceExe

Write-Host "Source executable exists: $sourceExists" -ForegroundColor $(if ($sourceExists) { 'Green' } else { 'Red' })

# Check logs
$logsDir = "$env:ProgramData\TimeTracker\Logs"
$logsExist = Test-Path $logsDir
Write-Host "Logs directory exists: $logsExist" -ForegroundColor $(if ($logsExist) { 'Green' } else { 'Red' })

Write-Host "Status check completed." -ForegroundColor Green
