# PowerShell script to install the TimeTracker Windows Service using the MSI installer
param(
    [Parameter(Mandatory=$false)]
    [string]$MsiPath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Uninstall = $false
)

# Require administrator privileges
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires administrator privileges. Please run as Administrator." -ForegroundColor Red
    exit 1
}

Write-Host "TimeTracker Service Installer" -ForegroundColor Green

try {
    if ($Uninstall) {
        Write-Host "Uninstalling TimeTracker Service..." -ForegroundColor Yellow
        
        # Try to find the product by name
        $product = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*Employee Activity Monitor*" }
        
        if ($product) {
            Write-Host "Found installed product: $($product.Name)" -ForegroundColor Cyan
            Write-Host "Uninstalling..." -ForegroundColor Yellow
            
            $result = $product.Uninstall()
            if ($result.ReturnValue -eq 0) {
                Write-Host "Uninstallation completed successfully" -ForegroundColor Green
            } else {
                Write-Host "Uninstallation failed with return code: $($result.ReturnValue)" -ForegroundColor Red
                exit 1
            }
        } else {
            Write-Host "TimeTracker service not found in installed programs" -ForegroundColor Yellow
        }
        
        # Also try to stop and remove service directly if it exists
        $service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        if ($service) {
            Write-Host "Found service directly, attempting to stop and remove..." -ForegroundColor Yellow
            
            if ($service.Status -eq "Running") {
                Stop-Service -Name "TimeTracker.DesktopApp" -Force
                Write-Host "Service stopped" -ForegroundColor Yellow
            }
            
            # Remove service using sc.exe
            & sc.exe delete "TimeTracker.DesktopApp"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Service removed successfully" -ForegroundColor Green
            }
        }
        
        return
    }
    
    # Installation logic
    if ([string]::IsNullOrEmpty($MsiPath)) {
        # Try to find the MSI in common locations
        $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
        $rootDir = Split-Path -Parent $scriptDir
        
        $possiblePaths = @(
            (Join-Path $rootDir "dist\TimeTrackerInstaller-Release.msi"),
            (Join-Path $rootDir "dist\TimeTrackerInstaller-Debug.msi"),
            (Join-Path $scriptDir "TimeTracker.Installer\bin\Release\*\*.msi"),
            (Join-Path $scriptDir "TimeTracker.Installer\bin\Debug\*\*.msi")
        )
        
        foreach ($path in $possiblePaths) {
            $files = Get-ChildItem $path -ErrorAction SilentlyContinue
            if ($files) {
                $MsiPath = $files[0].FullName
                break
            }
        }
        
        if ([string]::IsNullOrEmpty($MsiPath)) {
            Write-Host "MSI file not found. Please build the installer first or specify the path with -MsiPath" -ForegroundColor Red
            Write-Host "Run: .\build-installer.ps1" -ForegroundColor Yellow
            exit 1
        }
    }
    
    # Verify MSI file exists
    if (-not (Test-Path $MsiPath)) {
        Write-Host "MSI file not found: $MsiPath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Installing from: $MsiPath" -ForegroundColor Cyan
    
    # Check if service is already installed
    $existingService = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "TimeTracker service is already installed. Status: $($existingService.Status)" -ForegroundColor Yellow
        Write-Host "To reinstall, first uninstall with: .\install-service.ps1 -Uninstall" -ForegroundColor Yellow
        
        $response = Read-Host "Do you want to continue anyway? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Host "Installation cancelled" -ForegroundColor Yellow
            exit 0
        }
    }
    
    # Build msiexec arguments
    $msiArgs = @("/i", "`"$MsiPath`"")
    
    if ($Silent) {
        $msiArgs += "/quiet"
        Write-Host "Installing silently..." -ForegroundColor Yellow
    } else {
        $msiArgs += "/passive"
        Write-Host "Installing with basic UI..." -ForegroundColor Yellow
    }
    
    # Add logging
    $logPath = Join-Path $env:TEMP "TimeTrackerInstall.log"
    $msiArgs += "/l*v"
    $msiArgs += "`"$logPath`""
    
    Write-Host "Installation log will be written to: $logPath" -ForegroundColor Cyan
    Write-Host "Running msiexec with arguments: $($msiArgs -join ' ')" -ForegroundColor Cyan
    
    # Run the installer
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $msiArgs -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "Installation completed successfully!" -ForegroundColor Green
        
        # Verify service installation
        Start-Sleep -Seconds 2
        $service = Get-Service -Name "TimeTracker.DesktopApp" -ErrorAction SilentlyContinue
        
        if ($service) {
            Write-Host "Service installed successfully. Status: $($service.Status)" -ForegroundColor Green
            
            if ($service.Status -ne "Running") {
                Write-Host "Starting service..." -ForegroundColor Yellow
                Start-Service -Name "TimeTracker.DesktopApp"
                Start-Sleep -Seconds 2
                
                $service = Get-Service -Name "TimeTracker.DesktopApp"
                Write-Host "Service status after start: $($service.Status)" -ForegroundColor Cyan
            }
        } else {
            Write-Host "Warning: Service not found after installation" -ForegroundColor Yellow
        }
        
    } else {
        Write-Host "Installation failed with exit code: $($process.ExitCode)" -ForegroundColor Red
        Write-Host "Check the log file for details: $logPath" -ForegroundColor Yellow
        exit 1
    }
    
} catch {
    Write-Host "Installation error: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Installation process completed!" -ForegroundColor Green
