# TimeTracker Deployment Guide

This guide covers the complete deployment workflow for the Internal Employee Activity Monitor (TimeTracker) Windows Service.

## Quick Start

### Prerequisites
- Windows 10/11 (x64)
- .NET 8 Runtime (or SDK for development)
- Administrator privileges for installation
- WiX Toolset v4+ (for building from source)

### Build and Install (Development)
```powershell
# 1. Install WiX Toolset (if not already installed)
dotnet tool install --global wix

# 2. Navigate to desktop-app directory
cd desktop-app

# 3. Build the installer
.\build-installer-simple.ps1

# 4. Install the service (requires Administrator)
.\install-service.ps1
```

### Production Deployment
```powershell
# Use pre-built MSI from dist/ directory
.\install-service.ps1 -MsiPath "..\dist\TimeTrackerInstaller-Release.msi"
```

## Build Process

### Available Build Scripts

1. **build-installer-simple.ps1** (Recommended)
   - Uses WiX CLI directly
   - Simpler, more reliable
   - Faster build times

2. **build-installer.ps1** (Advanced)
   - Uses MSBuild with WiX SDK
   - More complex project structure
   - Better for CI/CD integration

### Build Options

```powershell
# Debug build (default for development)
.\build-installer-simple.ps1 -Configuration Debug

# Release build (for production)
.\build-installer-simple.ps1 -Configuration Release
```

### Build Output
- **Location**: `../dist/TimeTrackerInstaller-{Configuration}.msi`
- **Size**: ~30KB (minimal installer)
- **Type**: Windows Installer (MSI) package

## Installation Process

### Manual Installation
```powershell
# Basic installation with UI
.\install-service.ps1

# Silent installation (no UI)
.\install-service.ps1 -Silent

# Specify custom MSI path
.\install-service.ps1 -MsiPath "path\to\installer.msi"
```

### What Gets Installed
- **Service Executable**: `TimeTracker.DesktopApp.exe`
- **Configuration**: `appsettings.json`
- **Dependencies**: .NET 8 runtime dependencies
- **Installation Path**: `C:\Program Files\TimeTracker\DesktopApp\`

### Service Configuration
- **Service Name**: `TimeTracker.DesktopApp`
- **Display Name**: `Internal Employee Activity Monitor`
- **Start Type**: Automatic (starts with Windows)
- **Account**: LocalSystem
- **Dependencies**: None

## Service Management

### PowerShell Commands
```powershell
# Check service status
Get-Service -Name "TimeTracker.DesktopApp"

# Start service
Start-Service -Name "TimeTracker.DesktopApp"

# Stop service
Stop-Service -Name "TimeTracker.DesktopApp"

# Restart service
Restart-Service -Name "TimeTracker.DesktopApp"
```

### Windows Services Manager
1. Open `services.msc`
2. Find "Internal Employee Activity Monitor"
3. Right-click for options (Start, Stop, Properties, etc.)

## Uninstallation

### Using Install Script
```powershell
.\install-service.ps1 -Uninstall
```

### Manual Uninstallation
1. **Control Panel** → Programs and Features
2. Find "Internal Employee Activity Monitor"
3. Click "Uninstall"

### Force Removal (if needed)
```powershell
# Stop service
Stop-Service -Name "TimeTracker.DesktopApp" -Force

# Remove service
sc.exe delete "TimeTracker.DesktopApp"

# Remove files manually
Remove-Item "C:\Program Files\TimeTracker" -Recurse -Force
```

## Troubleshooting

### Common Issues

#### 1. Build Fails - WiX Not Found
```
Error: Could not resolve SDK "WiX.SDK"
```
**Solution**: Install WiX Toolset
```powershell
dotnet tool install --global wix
```

#### 2. Installation Fails - Access Denied
```
Error: Installation failed with exit code: 1603
```
**Solution**: Run PowerShell as Administrator

#### 3. Service Won't Start
```
Error: Service failed to start
```
**Solutions**:
- Check Windows Event Log (Application)
- Verify .NET 8 Runtime is installed
- Check file permissions in installation directory

#### 4. Missing Dependencies
```
Error: Could not load file or assembly
```
**Solution**: Ensure .NET 8 Runtime is installed
```powershell
# Check installed .NET versions
dotnet --list-runtimes
```

### Log Files
- **Installation Log**: `%TEMP%\TimeTrackerInstall.log`
- **Service Logs**: Windows Event Log → Application
- **Application Logs**: Service writes to Event Log with source "TimeTracker.DesktopApp"

### Diagnostic Commands
```powershell
# Check .NET runtime
dotnet --info

# Check service status
Get-Service -Name "TimeTracker.DesktopApp" | Format-List *

# Check installation directory
Get-ChildItem "C:\Program Files\TimeTracker\DesktopApp\"

# Check event logs
Get-EventLog -LogName Application -Source "TimeTracker.DesktopApp" -Newest 10
```

## Enterprise Deployment

### Silent Installation
```powershell
# For enterprise deployment
msiexec.exe /i "TimeTrackerInstaller-Release.msi" /quiet /l*v "install.log"
```

### Group Policy Deployment
1. Copy MSI to network share
2. Create Group Policy Object (GPO)
3. Computer Configuration → Software Settings → Software Installation
4. Add new package pointing to MSI

### SCCM Deployment
1. Import MSI as application
2. Configure detection method (service exists)
3. Deploy to target collections

## Security Considerations

### Service Account
- **Default**: LocalSystem (high privileges)
- **Alternative**: NetworkService (lower privileges)
- **Custom**: Domain service account (for network access)

### File Permissions
- Installation directory: Administrators (Full), Users (Read)
- Database file: Service account (Full)
- Configuration file: Service account (Read)

### Network Security
- HTTPS communication to Pipedream endpoint
- No inbound network connections
- Firewall: Allow outbound HTTPS (port 443)

## Monitoring and Maintenance

### Health Checks
```powershell
# Service health
if ((Get-Service -Name "TimeTracker.DesktopApp").Status -eq "Running") {
    Write-Host "Service is healthy" -ForegroundColor Green
} else {
    Write-Host "Service needs attention" -ForegroundColor Red
}

# Database check
if (Test-Path "C:\Program Files\TimeTracker\DesktopApp\TimeTracker.db") {
    Write-Host "Database file exists" -ForegroundColor Green
}
```

### Performance Monitoring
- CPU usage should be < 5%
- Memory usage should be < 100MB
- Monitor via Task Manager or Performance Monitor

### Updates
1. Stop service
2. Install new MSI (will upgrade existing installation)
3. Start service
4. Verify functionality

## Development Notes

### Building from Source
```powershell
# Full development build
git clone <repository>
cd timetracker/desktop-app
dotnet restore
.\build-installer-simple.ps1 -Configuration Debug
```

### Customization
- Modify `appsettings.json` for configuration changes
- Update `Product.wxs` for installer changes
- Rebuild installer after any changes

### Testing
```powershell
# Run functional test
.\TimeTracker.DesktopApp\test-functionality.ps1

# Install and test service
.\build-installer-simple.ps1
.\install-service.ps1
# ... test functionality ...
.\install-service.ps1 -Uninstall
```
