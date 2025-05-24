# TimeTracker Installer

This directory contains the WiX Toolset installer project for the Internal Employee Activity Monitor (TimeTracker) Windows Service.

## Prerequisites

1. **.NET 8 SDK** - Required for building the application and installer
2. **WiX Toolset v4+** - Installed as a .NET global tool
3. **Administrator privileges** - Required for service installation

## Quick Start

### Install WiX Toolset (if not already installed)
```powershell
dotnet tool install --global wix
```

### Build the Installer
From the `desktop-app` directory:
```powershell
.\build-installer.ps1
```

### Install the Service
```powershell
.\install-service.ps1
```

### Uninstall the Service
```powershell
.\install-service.ps1 -Uninstall
```

## Build Options

### Build Configurations
- **Release** (default): Optimized build for production
- **Debug**: Development build with debug symbols

```powershell
# Build Release version
.\build-installer.ps1 -Configuration Release

# Build Debug version
.\build-installer.ps1 -Configuration Debug
```

### Build Parameters
- `-Configuration`: Debug or Release (default: Release)
- `-SkipPublish`: Skip application publish step (use existing files)
- `-Clean`: Clean previous builds before building

```powershell
# Clean build
.\build-installer.ps1 -Clean

# Quick rebuild (skip publish)
.\build-installer.ps1 -SkipPublish
```

## Installation Options

### Silent Installation
```powershell
.\install-service.ps1 -Silent
```

### Specify MSI Path
```powershell
.\install-service.ps1 -MsiPath "C:\path\to\installer.msi"
```

## Project Structure

```
TimeTracker.Installer/
├── TimeTrackerInstaller.wixproj    # WiX project file
├── Product.wxs                     # Main installer definition
└── README.md                       # This file
```

## Installer Features

### What Gets Installed
- **TimeTracker.DesktopApp.exe** - Main service executable
- **Required .NET libraries** - Microsoft.Extensions.*, Microsoft.Data.Sqlite, etc.
- **Configuration files** - appsettings.json
- **SQLite native library** - e_sqlite3.dll

### Installation Location
- **Default**: `C:\Program Files\TimeTracker\DesktopApp\`
- **Configurable**: Users can change during installation

### Windows Service Configuration
- **Service Name**: `TimeTracker.DesktopApp`
- **Display Name**: `Internal Employee Activity Monitor`
- **Start Type**: Automatic (starts with Windows)
- **Account**: LocalSystem
- **Description**: Monitors employee application usage and activity for productivity insights

### Security & Permissions
- **Requires Administrator**: Installation requires elevated privileges
- **Service Account**: Runs under LocalSystem account
- **File Permissions**: Standard program files permissions

## Troubleshooting

### Common Issues

1. **WiX not found**
   ```
   Solution: Install WiX toolset
   dotnet tool install --global wix
   ```

2. **Build fails with missing files**
   ```
   Solution: Ensure application is published first
   .\build-installer.ps1 -Clean
   ```

3. **Service installation fails**
   ```
   Solution: Run PowerShell as Administrator
   Right-click PowerShell → "Run as Administrator"
   ```

4. **Service won't start**
   ```
   Check Windows Event Log:
   - Windows Logs → Application
   - Look for TimeTracker.DesktopApp events
   ```

### Log Files
- **Installation Log**: `%TEMP%\TimeTrackerInstall.log`
- **Service Logs**: Windows Event Log (Application)
- **Application Logs**: Service writes to Event Log

### Manual Service Management
```powershell
# Check service status
Get-Service -Name "TimeTracker.DesktopApp"

# Start service
Start-Service -Name "TimeTracker.DesktopApp"

# Stop service
Stop-Service -Name "TimeTracker.DesktopApp"

# Remove service (if installer fails)
sc.exe delete "TimeTracker.DesktopApp"
```

## Development Notes

### Updating the Installer
1. Modify `Product.wxs` for installer changes
2. Update version numbers in `Product.wxs`
3. Rebuild with `.\build-installer.ps1`

### Adding New Files
1. Add file references to appropriate `<Component>` in `Product.wxs`
2. Ensure files are included in the publish output
3. Test installation and verify files are deployed

### Changing Service Configuration
- Modify `<ServiceInstall>` element in `Product.wxs`
- Update service account, start type, or other properties
- Rebuild and test installation

## Version Information
- **WiX Toolset**: v4.0.5+
- **Target Platform**: x64 Windows
- **Installer Type**: MSI (Windows Installer)
- **.NET Version**: .NET 8.0
