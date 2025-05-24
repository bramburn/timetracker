# TimeTracker - Employee Activity Monitor

## Overview

TimeTracker is an Employee Activity Monitor designed to capture essential user activity data on Windows systems. This Phase 1 MVP focuses on developing a discreet Windows Service application that monitors user activity, stores data locally in SQLite, and transmits it to a configurable Pipedream endpoint for testing.

## Project Structure

```
timetracker/
├── desktop-app/                    # C# Windows Service Application
│   └── TimeTracker.DesktopApp/     # Main desktop application project
├── webserver/                      # Django web server (future phases)
├── docs/                          # Documentation
│   └── phase-1.md                 # Phase 1 requirements and specifications
├── timetracker.sln                # Visual Studio solution file
├── .gitignore                     # Git ignore patterns
└── README.md                      # This file
```

## Phase 1 Features

### Core Functionality
- **Silent Windows Service Operation**: Runs as a background Windows Service with no UI
- **User Identification**: Automatically captures Windows username
- **Active Window Tracking**: Monitors foreground window changes and application usage
- **Activity Detection**: Binary active/inactive status based on keyboard/mouse input
- **Local Data Storage**: Persistent SQLite database for activity logs
- **Data Submission**: HTTP POST to configurable Pipedream endpoint for testing

### Key Components
- **WindowMonitor**: Tracks active window changes using Win32 APIs
- **InputMonitor**: Detects user input activity via global hooks
- **ActivityLogger**: Central orchestrator for data collection and storage
- **SQLiteDataAccess**: Database operations and schema management
- **PipedreamClient**: HTTP client for data submission with retry logic

## Requirements

### Development Environment
- .NET 8 SDK
- Visual Studio 2022 (or Rider/VS Code with C# Dev Kit)
- Windows 10/11 (x64)

### Key Dependencies
- Microsoft.Extensions.Hosting.WindowsServices
- Microsoft.Data.Sqlite
- Microsoft.Windows.CsWin32
- System.Text.Json (built-in)

## Getting Started

### Building the Application

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd timetracker
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

### Configuration

1. Update `desktop-app/TimeTracker.DesktopApp/appsettings.json` with your Pipedream endpoint URL
2. Configure any other settings as needed

### Running for Development

```bash
cd desktop-app/TimeTracker.DesktopApp
dotnet run
```

### Installing as Windows Service

#### Prerequisites
- Windows 10/11 (x64)
- .NET 8 Runtime (or SDK for development)
- Administrator privileges

#### Quick Installation
1. **Install WiX Toolset** (if building from source):
   ```powershell
   dotnet tool install --global wix
   ```

2. **Build the installer**:
   ```powershell
   cd desktop-app
   .\build-installer-simple.ps1
   ```

3. **Install the service** (requires Administrator):
   ```powershell
   .\install-service.ps1
   ```

> **Note**: For detailed deployment instructions, see [DEPLOYMENT.md](desktop-app/DEPLOYMENT.md)

#### Alternative: Use Pre-built MSI
If available, download the MSI installer and run:
```powershell
.\install-service.ps1 -MsiPath "path\to\TimeTrackerInstaller.msi"
```

#### Service Management
- **Service Name**: `TimeTracker.DesktopApp`
- **Display Name**: `Internal Employee Activity Monitor`
- **Auto-start**: Yes (starts with Windows)
- **Account**: LocalSystem

#### Uninstallation
```powershell
.\install-service.ps1 -Uninstall
```

## Data Collection

### Activity Data Model
- **Timestamp**: UTC timestamp of the activity
- **WindowsUsername**: Current Windows user
- **ActiveWindowTitle**: Title of the foreground window
- **ApplicationProcessName**: Name of the active application process
- **ActivityStatus**: Binary status ("Active" or "Inactive")

### Local Storage
- SQLite database (`TimeTracker.db`) created in application directory
- `ActivityLogs` table with chronological data appending
- Persistent storage ensures no data loss during network outages

### Data Submission
- JSON payload sent to configured Pipedream endpoint
- Robust error handling with retry mechanisms
- Continues local storage even if submission fails

## Performance Requirements

- **CPU Usage**: < 5% average
- **Memory Usage**: < 100MB
- **Minimal System Impact**: Optimized polling and efficient API calls

## Security & Privacy

- **No Keystroke Logging**: Only binary activity detection, no actual key content
- **HTTPS Transmission**: Encrypted data in transit
- **Local Database Protection**: Secure file location and service account permissions
- **Data Minimization**: Collects only necessary activity metadata

## Testing

### Unit Tests
- Individual component testing with mocked dependencies
- Database operations testing
- JSON serialization validation

### Integration Tests
- End-to-end data flow testing
- Pipedream submission testing
- Local storage persistence testing

### System Tests
- Windows Service installation and operation
- Performance impact validation
- Network error handling
- Service start/stop/restart scenarios

## Development Status

- ✅ Phase 1 Requirements Analysis
- 🚧 Core Application Development (In Progress)
- ⏳ Windows Service Implementation
- ⏳ Monitoring Components
- ⏳ Data Persistence
- ⏳ Network Communication
- ⏳ Testing & Validation
- ⏳ Installer Package (Future Phase)

## Contributing

Please refer to the project documentation in the `docs/` directory for detailed requirements and specifications.

## License

[License information to be added]
