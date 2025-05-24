# TimeTracker - Employee Activity Monitor

A Windows service application that monitors user activity, stores data locally in SQLite, and transmits it to a configurable endpoint.

## Features

- Runs as a background Windows Service
- Tracks active window changes and application usage
- Detects user activity (keyboard/mouse input)
- Stores data locally in SQLite database
- Transmits data to configurable endpoint
- Minimal system impact (< 5% CPU, < 100MB RAM)

## Requirements

- Windows 10/11 (x64)
- .NET 8 Runtime
- Administrator privileges for installation

## Quick Start

### Installation

1. Download the latest MSI installer from the [Releases](https://github.com/yourusername/timetracker/releases) page
2. Run the installer with administrator privileges
3. The service will automatically start and run in the background

### Manual Installation (Development)

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/timetracker.git
   cd timetracker
   ```

2. Build the solution:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Install the service:
   ```powershell
   cd desktop-app
   .\install-service.ps1
   ```

### Configuration

1. Update `desktop-app/TimeTracker.DesktopApp/appsettings.json` with your endpoint URL
2. Restart the service to apply changes

## Service Management

- **Service Name**: `TimeTracker.DesktopApp`
- **Display Name**: `Internal Employee Activity Monitor`

### Commands

```powershell
# Start service
net start TimeTracker.DesktopApp

# Stop service
net stop TimeTracker.DesktopApp

# Uninstall
.\install-service.ps1 -Uninstall
```

## Data Collection

The service collects:
- Active window title
- Application process name
- User activity status
- Windows username
- Timestamp

Data is stored locally in SQLite and transmitted to the configured endpoint.

## Development

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 (or Rider/VS Code with C# Dev Kit)
- WiX Toolset v4.0 or later

### Setting Up WiX Toolset

1. Install WiX Toolset:
   ```powershell
   # Using winget (recommended)
   winget install WiXToolset.WiXToolset

   # Or download from https://github.com/wixtoolset/wix4/releases
   ```

2. Add WiX to your PATH if not already done:
   ```powershell
   # Add to your PowerShell profile
   $env:Path += ";C:\Program Files\WiX Toolset v4.0\bin"
   ```

### Building the Installer

1. Navigate to the desktop app directory:
   ```powershell
   cd desktop-app
   ```

2. Build the MSI installer:
   ```powershell
   # Build using the provided script
   .\build-installer-simple.ps1

   # Or manually
   dotnet build TimeTracker.Installer
   ```

3. The MSI installer will be created in the `dist` directory

### Installing for Development

1. Build the solution:
   ```powershell
   dotnet restore
   dotnet build
   ```

2. Install the service:
   ```powershell
   cd desktop-app
   .\install-service.ps1
   ```

3. Verify installation:
   ```powershell
   Get-Service TimeTracker.DesktopApp
   ```

## Security & Privacy

- No keystroke logging
- HTTPS transmission
- Local database protection
- Data minimization

## License

[License information to be added]

## Contributing

Please refer to the project documentation in the `docs/` directory for detailed requirements and specifications.