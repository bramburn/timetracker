# TimeTracker - Employee Activity Monitor

A Windows service application that monitors user activity, stores data in SQL Server, and transmits it to a configurable endpoint.

## Features

- Runs as a background Windows Service
- Tracks active window changes and application usage
- Detects user activity (keyboard/mouse input)
- Stores data in SQL Server with optimized performance
- Batch processing for efficient data handling
- Transmits data to configurable endpoint
- Minimal system impact (< 5% CPU, < 100MB RAM)

## Requirements

- Windows 10/11 (x64)
- .NET 8 Runtime
- SQL Server Express LocalDB
- Administrator privileges for installation
- WiX Toolset v4.0 (for building installer)

## Quick Start

### Production Installation

1. Download the latest MSI installer from the [Releases](https://github.com/yourusername/timetracker/releases) page
2. Run the installer with administrator privileges
3. The service will automatically start and run in the background

### Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/timetracker.git
   cd timetracker
   ```

2. Install prerequisites:
   ```powershell
   # Install WiX Toolset
   winget install WiXToolset.WiXToolset
   
   # Install SQL Server Express LocalDB
   winget install Microsoft.SQLServer.2022.Express.LocalDB
   ```

3. Build the solution:
   ```powershell
   dotnet restore
   dotnet build
   ```

4. Choose your installation method:

   a. Direct service installation (recommended for development):
   ```powershell
   cd desktop-app
   .\install-service-direct.ps1 build    # Build the application
   .\install-service-direct.ps1 install  # Install as service
   ```

   b. MSI installer (recommended for production):
   ```powershell
   cd desktop-app
   .\build-installer-simple.ps1          # Build the MSI installer
   .\install-service.ps1                 # Install using MSI
   ```

## Configuration

The application uses SQL Server for data storage with the following configurable options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TimeTracker;Trusted_Connection=True;"
  },
  "TimeTracker": {
    "MaxBatchSize": 100,
    "BatchInsertIntervalMs": 30000,
    "EnableBulkOperations": true
  }
}
```

- `MaxBatchSize`: Maximum number of records to process in a single batch
- `BatchInsertIntervalMs`: Interval between batch processing in milliseconds
- `EnableBulkOperations`: Enable/disable bulk insert operations for better performance

## Development Workflow

### Running Without Service Installation

For quick testing and development:
```powershell
cd desktop-app
.\run-timetracker.ps1 -Build -Publish -Console
```

### Service Management

1. Check service status:
   ```powershell
   .\check-status.ps1
   ```

2. Start/Stop service:
   ```powershell
   .\install-service-direct.ps1 start
   .\install-service-direct.ps1 stop
   ```

3. Monitor service:
   ```powershell
   .\install-service-direct.ps1 monitor
   ```

### Troubleshooting

1. Run diagnostics:
   ```powershell
   .\diagnose-service-enhanced.ps1
   ```

2. Manual service test:
   ```powershell
   .\test-service-manual.ps1
   ```

3. Performance testing:
   ```powershell
   .\test-performance.ps1 -DurationMinutes 5
   ```

## Production Deployment

### Building the Installer

1. Build the MSI installer:
   ```powershell
   cd desktop-app
   .\build-installer.ps1
   ```

2. The installer will be created in the `dist` directory

### Installation Options

1. Silent installation:
   ```powershell
   .\install-service.ps1 -Silent
   ```

2. Uninstallation:
   ```powershell
   .\install-service.ps1 -Uninstall
   ```

### Service Configuration

- **Service Name**: `TimeTracker.DesktopApp`
- **Display Name**: `Internal Employee Activity Monitor`
- **Configuration File**: `appsettings.json`

Update the configuration:
1. Edit `appsettings.json`
2. Restart the service:
   ```powershell
   .\install-service-direct.ps1 restart
   ```

## Data Collection

The service collects:
- Active window title
- Application process name
- User activity status
- Windows username
- Timestamp

Data is stored in SQL Server with optimized indexes for:
- Timestamp-based queries
- Sync status tracking
- Username lookups
- Process name searches

Data is transmitted to the configured endpoint.

## Security & Privacy

- No keystroke logging
- HTTPS transmission
- Local database protection
- Data minimization

## Troubleshooting Guide

If you encounter issues:

1. Check service status:
   ```powershell
   .\check-status.ps1
   ```

2. Run enhanced diagnostics:
   ```powershell
   .\diagnose-service-enhanced.ps1
   ```

3. View service logs:
   ```powershell
   .\troubleshoot-service.ps1
   ```

4. Common solutions:
   - Service not visible: Run `.\install-service-direct.ps1 uninstall` followed by `install`
   - Service won't start: Check Event Viewer and rebuild/install
   - No data transmission: Verify endpoint URL in `appsettings.json`

## License

[License information to be added]

## Contributing

Please refer to the project documentation in the `docs/` directory for detailed requirements and specifications.