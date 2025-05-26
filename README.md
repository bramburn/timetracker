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

**Option 1: User Account Service (Recommended)**
```powershell
# 1. Clone and build
git clone https://github.com/bramburn/timetracker.git
cd timetracker/desktop-app

# 2. Build the application
dotnet publish TimeTracker.DesktopApp -c Release -r win-x64 --self-contained

# 3. Install as service running under your user account
.\install-service-user-fixed.bat

# 4. Configure service account manually:
# - Open services.msc as Administrator
# - Find "Internal Employee Activity Monitor"
# - Right-click -> Properties -> Log On tab
# - Select "This account" and enter your Windows credentials
# - Start the service
```

**Option 2: PowerShell Installation**
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

4. **Development installation:**
   ```powershell
   cd desktop-app

   # Build and install for development
   .\install-service-direct.ps1 build
   .\install-service-direct.ps1 install

   # Monitor the service
   .\install-service-direct.ps1 monitor
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

## Service Management

### Available Commands

```powershell
cd desktop-app

# Build the application
.\install-service-direct.ps1 build

# Install the service
.\install-service-direct.ps1 install

# Start/Stop/Restart service
.\install-service-direct.ps1 start
.\install-service-direct.ps1 stop
.\install-service-direct.ps1 restart

# Monitor service in real-time
.\install-service-direct.ps1 monitor

# Check service status
.\install-service-direct.ps1 status

# Uninstall service
.\install-service-direct.ps1 uninstall
```

### Monitoring and Logs

**Real-time Monitoring:**
```powershell
.\install-service-direct.ps1 monitor
```

**Log Files:**
- **Service Logs**: `C:\ProgramData\TimeTracker\Logs\TimeTracker.log`
- **Windows Event Log**: Application log (source: TimeTracker.DesktopApp)

**Check Recent Activity:**
```powershell
Get-Content "C:\ProgramData\TimeTracker\Logs\TimeTracker.log" -Tail 20
```

## Troubleshooting

### Common Issues

**1. Service shows "Inactive" status with "Time since last input: Never"**
- **Cause**: Service running as SYSTEM account cannot access user desktop
- **Solution**: Use user account installation method:
  ```powershell
  .\install-service-user-fixed.bat
  # Then configure service account in services.msc
  ```

**2. Service fails to start**
- **Cause**: User account needs password or "Log on as a service" rights
- **Solution**:
  1. Open `services.msc` as Administrator
  2. Find "Internal Employee Activity Monitor"
  3. Properties → Log On tab → Configure user account and password

**3. No data being sent to Pipedream**
- **Check**: Service logs for errors
- **Verify**: Pipedream endpoint URL in `appsettings.json`
- **Test**: Connection using the monitor command

**4. Service not visible in Services console**
- **Solution**: Uninstall and reinstall:
  ```powershell
  .\install-service-direct.ps1 uninstall
  .\install-service-direct.ps1 install
  ```

### Diagnostic Commands

```powershell
# Check service status
Get-Service "TimeTracker.DesktopApp"

# View recent logs
Get-Content "C:\ProgramData\TimeTracker\Logs\TimeTracker.log" -Tail 50

# Check Windows Event Log
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='TimeTracker.DesktopApp'} -MaxEvents 10
```

## Data Collection & Privacy

### What Data is Collected
- **Window Information**: Active window title and application process name
- **Activity Status**: User activity state (Active/Inactive) based on input detection
- **User Context**: Windows username and machine name
- **Timestamps**: When activities occur
- **Session Data**: Batch IDs for tracking data transmission

### What is NOT Collected
- **No Keystroke Logging**: Individual keystrokes are never recorded
- **No Screen Content**: Screenshots or screen content are not captured
- **No Personal Files**: File contents or personal documents are not accessed
- **No Network Traffic**: Other network activity is not monitored

### Data Storage & Transmission
- **Local Storage**: SQL Server Express LocalDB with encrypted connections
- **Transmission**: HTTPS to configured Pipedream endpoint
- **Batch Processing**: Data sent in batches every 1 minute (configurable)
- **Cleanup**: Successfully transmitted data is automatically deleted locally

### Security Features
- **Encrypted Database Connections**: `TrustServerCertificate=True` in connection string
- **HTTPS Transmission**: All data sent over secure connections
- **Local Data Protection**: Database files protected by Windows file system security
- **Minimal Data Retention**: Data deleted after successful transmission

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