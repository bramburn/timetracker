# Simple PowerShell script to build the TimeTracker installer using WiX CLI
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Write-Host "Building TimeTracker Installer (Simple)..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Get script directory
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootDir = Split-Path -Parent $scriptDir
    
    # Define paths
    $appProjectPath = Join-Path $scriptDir "TimeTracker.DesktopApp\TimeTracker.DesktopApp.csproj"
    $publishDir = Join-Path $scriptDir "TimeTracker.DesktopApp\bin\$Configuration\net8.0-windows\win-x64\publish"
    $wxsFile = Join-Path $scriptDir "TimeTracker.Installer\Product.wxs"
    $outputDir = Join-Path $rootDir "dist"
    
    Write-Host "Script Directory: $scriptDir" -ForegroundColor Cyan
    Write-Host "App Project: $appProjectPath" -ForegroundColor Cyan
    Write-Host "Publish Directory: $publishDir" -ForegroundColor Cyan
    Write-Host "WXS File: $wxsFile" -ForegroundColor Cyan
    Write-Host "Output Directory: $outputDir" -ForegroundColor Cyan
    
    # Step 1: Build and publish the main application
    Write-Host "Building and publishing TimeTracker.DesktopApp..." -ForegroundColor Yellow
    
    & dotnet publish $appProjectPath -c $Configuration -r win-x64 --self-contained false -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish TimeTracker.DesktopApp"
    }
    
    Write-Host "Application published successfully to: $publishDir" -ForegroundColor Green
    
    # Verify required files exist
    $requiredFiles = @(
        "TimeTracker.DesktopApp.exe",
        "appsettings.json"
    )
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $publishDir $file
        if (-not (Test-Path $filePath)) {
            throw "Required file not found: $filePath"
        }
    }
    
    Write-Host "All required files verified in publish directory" -ForegroundColor Green
    
    # Step 2: Create output directory
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
        Write-Host "Created output directory: $outputDir" -ForegroundColor Yellow
    }
    
    # Step 3: Build the installer using WiX CLI
    Write-Host "Building WiX installer using CLI..." -ForegroundColor Yellow
    
    $msiPath = Join-Path $outputDir "TimeTrackerInstaller-$Configuration.msi"
    
    # Set the preprocessor variable for the publish directory
    $defineArg = "TimeTrackerAppPublishDir=$publishDir"
    
    Set-Location (Join-Path $scriptDir "TimeTracker.Installer")
    
    & wix build Product.wxs -o $msiPath -d $defineArg
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build WiX installer"
    }
    
    # Step 4: Verify and report the MSI file
    if (Test-Path $msiPath) {
        $msiFile = Get-Item $msiPath
        Write-Host "Installer built successfully!" -ForegroundColor Green
        Write-Host "MSI Location: $($msiFile.FullName)" -ForegroundColor Cyan
        Write-Host "MSI Size: $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        
        return $msiFile.FullName
    } else {
        throw "MSI file not found after build"
    }
    
} catch {
    Write-Host "Build failed: $_" -ForegroundColor Red
    exit 1
} finally {
    # Return to original directory
    Set-Location $scriptDir
}

Write-Host "Build completed successfully!" -ForegroundColor Green
