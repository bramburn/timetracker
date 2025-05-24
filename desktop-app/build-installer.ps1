# PowerShell script to build the TimeTracker installer
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPublish = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean = $false
)

Write-Host "Building TimeTracker Installer..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Get script directory
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootDir = Split-Path -Parent $scriptDir
    
    # Define paths
    $appProjectPath = Join-Path $scriptDir "TimeTracker.DesktopApp\TimeTracker.DesktopApp.csproj"
    $installerProjectPath = Join-Path $scriptDir "TimeTracker.Installer\TimeTrackerInstaller.wixproj"
    $publishDir = Join-Path $scriptDir "TimeTracker.DesktopApp\bin\$Configuration\net8.0-windows\win-x64\publish"
    
    Write-Host "Script Directory: $scriptDir" -ForegroundColor Cyan
    Write-Host "App Project: $appProjectPath" -ForegroundColor Cyan
    Write-Host "Installer Project: $installerProjectPath" -ForegroundColor Cyan
    Write-Host "Publish Directory: $publishDir" -ForegroundColor Cyan
    
    # Clean if requested
    if ($Clean) {
        Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
        if (Test-Path $publishDir) {
            Remove-Item $publishDir -Recurse -Force
            Write-Host "Removed publish directory" -ForegroundColor Yellow
        }
        
        # Clean installer output
        $installerBinDir = Join-Path $scriptDir "TimeTracker.Installer\bin"
        if (Test-Path $installerBinDir) {
            Remove-Item $installerBinDir -Recurse -Force
            Write-Host "Removed installer bin directory" -ForegroundColor Yellow
        }
    }
    
    # Step 1: Check if WiX is installed
    Write-Host "Checking WiX Toolset installation..." -ForegroundColor Yellow
    try {
        $wixVersion = & dotnet tool list --global | Select-String "wix"
        if ($wixVersion) {
            Write-Host "WiX Toolset found: $wixVersion" -ForegroundColor Green
        } else {
            Write-Host "WiX Toolset not found. Installing..." -ForegroundColor Yellow
            & dotnet tool install --global wix
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to install WiX Toolset"
            }
            Write-Host "WiX Toolset installed successfully" -ForegroundColor Green
        }
    } catch {
        Write-Host "Error checking/installing WiX: $_" -ForegroundColor Red
        throw
    }
    
    # Step 2: Build and publish the main application (unless skipped)
    if (-not $SkipPublish) {
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
    } else {
        Write-Host "Skipping application publish (using existing files)" -ForegroundColor Yellow
        
        # Verify publish directory exists
        if (-not (Test-Path $publishDir)) {
            throw "Publish directory not found: $publishDir. Run without -SkipPublish first."
        }
    }
    
    # Step 3: Build the installer
    Write-Host "Building WiX installer..." -ForegroundColor Yellow
    
    Set-Location (Join-Path $scriptDir "TimeTracker.Installer")
    
    & dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build WiX installer"
    }
    
    # Step 4: Locate and report the MSI file
    $msiPattern = Join-Path $scriptDir "TimeTracker.Installer\bin\$Configuration\*\*.msi"
    $msiFiles = Get-ChildItem $msiPattern -ErrorAction SilentlyContinue
    
    if ($msiFiles) {
        $msiFile = $msiFiles[0]
        Write-Host "Installer built successfully!" -ForegroundColor Green
        Write-Host "MSI Location: $($msiFile.FullName)" -ForegroundColor Cyan
        Write-Host "MSI Size: $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        
        # Copy to a more accessible location
        $outputDir = Join-Path $rootDir "dist"
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir | Out-Null
        }
        
        $finalMsiPath = Join-Path $outputDir "TimeTrackerInstaller-$Configuration.msi"
        Copy-Item $msiFile.FullName $finalMsiPath -Force
        Write-Host "Installer copied to: $finalMsiPath" -ForegroundColor Green
        
        return $finalMsiPath
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
