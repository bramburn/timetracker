# TimeTracker - Employee Activity Monitor

A Windows service application that monitors user activity, stores data in SQL Server Express, and transmits it to a configurable Pipedream endpoint for testing and analysis.

## Features

- **Windows Service**: Runs as a background service with user session access
- **Activity Monitoring**: Tracks active window changes and application usage
- **Input Detection**: Detects user activity (keyboard/mouse input) with global hooks
- **SQL Server Storage**: Uses SQL Server Express LocalDB for reliable data storage
- **Batch Processing**: Efficient batch processing for data transmission
- **Pipedream Integration**: Transmits data to configurable Pipedream endpoint
- **Performance Optimized**: Minimal system impact (< 5% CPU, < 100MB RAM)
- **User Session Aware**: Properly configured to monitor interactive desktop sessions

## Requirements

- **OS**: Windows 10/11 (x64)
- **Runtime**: .NET 8 Runtime
- **Database**: SQL Server Express LocalDB (automatically configured)
- **Privileges**: Administrator privileges for service installation
- **Account**: User account with password (for service to access desktop session)

## Quick Start

### Production Installation

**Option 1: Standard Installation (Recommended)**
```powershell
# 1. Clone and build
git clone https://github.com/bramburn/timetracker.git
cd timetracker/desktop-app

# 2. Build the application
dotnet publish TimeTracker.DesktopApp -c Release -r win-x64 --self-contained

# 3. Run the application
.\TimeTracker.DesktopApp.exe
```

**Option 2: Auto-start with Windows**
```powershell
# Run as Administrator
cd desktop-app
.\install-service-direct.ps1 install
```

### Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/bramburn/timetracker.git
   cd timetracker
   ```

2. **Install prerequisites:**
   ```powershell
   # Install .NET 8 SDK
   winget install Microsoft.DotNet.SDK.8

   # SQL Server Express LocalDB (optional - auto-configured)
   winget install Microsoft.SQLServer.2022.Express.LocalDB
   ```

3. **Build and test:**
   ```powershell
   # Restore dependencies
   dotnet restore

   # Build the solution
   dotnet build

   # Run tests
   dotnet test
   ```

4. **Development run:**
   ```powershell
   cd desktop-app

   # Build and run for development
   dotnet run --project TimeTracker.DesktopApp
   ```

## Application Management

### Available Commands

```powershell
cd desktop-app

# Build the application
dotnet build

# Run the application
dotnet run --project TimeTracker.DesktopApp

# Install auto-start
.\install-service-direct.ps1 install

# Remove auto-start
.\install-service-direct.ps1 uninstall
```

### System Tray Controls

The application runs in the system tray with the following controls:

- **Left-click**: Show status overlay
- **Right-click**: Open context menu with options:
  - Start Tracking
  - Pause Tracking
  - Stop Tracking
  - Settings
  - Status
  - Exit

### Monitoring and Logs

**Real-time Status:**
- Left-click the system tray icon to view the status overlay
- Shows current active window, tracking status, and Pipedream sync status

**Log Files:**
- **Application Logs**: `%APPDATA%\TimeTracker\Logs\TimeTracker.log`
- **Windows Event Log**: Application log (source: TimeTracker.DesktopApp)

**Check Recent Activity:**
```powershell
Get-Content "$env:APPDATA\TimeTracker\Logs\TimeTracker.log" -Tail 20
```

## Troubleshooting

### Common Issues

**1. Application not starting**
- **Cause**: Missing .NET 8 Runtime
- **Solution**: Install .NET 8 Runtime:
  ```powershell
  winget install Microsoft.DotNet.Runtime.8
  ```

**2. No system tray icon visible**
- **Cause**: Application crashed or failed to start
- **Solution**: Check application logs and Windows Event Viewer

**3. No data being sent to Pipedream**
- **Check**: Status overlay for sync status
- **Verify**: Pipedream endpoint URL in settings
- **Test**: Connection using the test button in settings

**4. Auto-start not working**
- **Solution**: Reinstall auto-start:
  ```powershell
  .\install-service-direct.ps1 uninstall
  .\install-service-direct.ps1 install
  ```

### Diagnostic Commands

```powershell
# Check if application is running
Get-Process TimeTracker.DesktopApp

# View recent logs
Get-Content "$env:APPDATA\TimeTracker\Logs\TimeTracker.log" -Tail 50

# Check Windows Event Log
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='TimeTracker.DesktopApp'} -MaxEvents 10
```

## Configuration

The application uses SQL Server Express LocalDB and Pipedream for data storage and transmission. Configuration is in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TimeTracker;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Pipedream": {
    "EndpointUrl": "https://eo2etvy0q1v9l11.m.pipedream.net",
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 5000
  },
  "BatchProcessor": {
    "IntervalMinutes": 1,
    "MaxBatchSize": 50
  },
  "ActivityLogger": {
    "MaxConcurrentSubmissions": 3,
    "ActivityTimeoutMs": 30000
  }
}
```

**Key Settings:**
- `EndpointUrl`: Pipedream webhook URL for data transmission
- `MaxRetryAttempts`: Number of retry attempts for failed transmissions
- `IntervalMinutes`: How often to process and send batches
- `MaxBatchSize`: Maximum records per batch transmission
- `ActivityTimeoutMs`: Timeout for detecting user inactivity

## Architecture

### Components
- **ActivityLogger**: Coordinates data collection and logging
- **OptimizedWindowMonitor**: Tracks active window changes using Windows hooks
- **GlobalHookInputMonitor**: Detects keyboard/mouse activity using global hooks
- **SqlServerDataAccess**: Manages database operations with batch processing
- **BatchProcessor**: Handles periodic data transmission to Pipedream
- **PipedreamClient**: Manages HTTP communication with the endpoint

### Data Flow
1. **Monitor** → Detect window/input changes
2. **Logger** → Store activity data locally
3. **Batch Processor** → Collect unsynced data every minute
4. **Pipedream Client** → Transmit data via HTTPS
5. **Cleanup** → Remove successfully transmitted data

## Project Structure

```
timetracker/
├── desktop-app/                          # Main application
│   ├── TimeTracker.DesktopApp/           # Core service application
│   ├── TimeTracker.DesktopApp.Tests/     # Unit tests
│   ├── install-service-direct.ps1        # PowerShell installer
│   └── install-service-user-fixed.bat    # User account installer
├── webserver/                            # Django web interface (future)
└── docs/                                 # Documentation
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass: `dotnet test`
6. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.