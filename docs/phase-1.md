Employee Activity Monitor - Phase 1 MVP Backlog
Introduction

This document provides a detailed backlog for Phase 1 (Minimum Viable Product - MVP) of the Employee Activity Monitor project. This phase focuses on developing a discreet Windows Service application that captures essential user activity data, stores it locally in an SQLite database, and transmits it to a configurable Pipedream endpoint for testing data submission mechanisms. Building the application as a Windows Service ensures robust background operation and system-level persistence.

The backlog is structured to provide clear user stories, actionable tasks, file relationships, a list of files to be created, acceptance criteria, a testing plan, identified assumptions and dependencies, and relevant non-functional requirements.
User Stories

    User Story 1: Silent Background Operation

        Description: As an IT Administrator, I want the Employee Activity Monitor application to install and run silently as a Windows Service with no mandatory user interaction post-installation, and start automatically when the user logs into Windows, so that employee work is not interrupted and the monitoring is seamless.

        Actions to Undertake:

            Project Setup: Initialize a new C# .NET 8 Windows Service application project.

            Startup Configuration: Implement logic to configure the Windows Service to start automatically (e.g., ServiceStartMode.Automatic).

            UI Suppression: Ensure the Windows Service runs without any visible user interface elements or pop-ups.

        References between Files:

            TimeTracker.DesktopApp.csproj: Defines project type and target framework (net8.0-windows), potentially including Microsoft.Extensions.Hosting.WindowsServices or similar for service hosting.

            Program.cs (or Service.cs): Contains the main entry point and logic for Windows Service execution and startup configuration.

            InstallerProject.wixproj (or similar): Defines the installation of the Windows Service.

        Acceptance Criteria:

            The application installs as a Windows Service without requiring user interaction beyond initial setup.

            The Windows Service starts automatically when the system boots or the user logs into Windows (depending on service configuration).

            No visible application window or icon appears in the taskbar or system tray.

            The Windows Service is visible and running in the Windows Services Manager.

        Testing Plan:

            Test Case 1: Verify Silent Startup as Service

                Test Data: Clean Windows 10/11 VM, installed application as a Windows Service.

                Expected Result: After rebooting, the TimeTracker.DesktopApp service is running in Windows Services Manager, and no UI is visible.

                Testing Tool: Manual observation, Windows Services Manager, Task Manager.

    User Story 2: User Identification

        Description: As the monitoring system, I want to automatically capture the current Windows username of the logged-in user so that all recorded activity can be accurately associated with the correct employee.

        Actions to Undertake:

            Username Capture: Implement a method to retrieve the current Windows username using System.Security.Principal.WindowsIdentity.GetCurrent().Name.

            Data Model Integration: Ensure the captured username is included in the activity data model.

        References between Files:

            ActivityDataModel.cs: Defines the structure for captured activity data, including a field for WindowsUsername.

            ActivityLogger.cs: Utilizes System.Security.Principal.WindowsIdentity to populate the WindowsUsername field in the ActivityDataModel before logging.

        Acceptance Criteria:

            The application successfully retrieves the correct Windows username.

            The captured username is consistently logged with each activity record.

        Testing Plan:

            Test Case 1: Verify Username Capture

                Test Data: Windows machine logged in as "TestUser1".

                Expected Result: Activity logs show "TestUser1" as the WindowsUsername for all entries.

                Testing Tool: Manual inspection of local SQLite database logs.

    User Story 3: Active Window Tracking

        Description: As the monitoring system, I want to continuously log the title of the active window and the name of the associated application process whenever the active window changes, so that insights into application usage can be gathered.

        Actions to Undertake:

            Win32 API Integration: Utilize Microsoft.Windows.CsWin32 NuGet package to generate P/Invoke declarations for GetForegroundWindow(), GetWindowText(), GetWindowTextLength(), and GetWindowThreadProcessId().

            Process Name Retrieval: Use System.Diagnostics.Process.GetProcessById() to get the process name from the process ID obtained via GetWindowThreadProcessId().

            Polling Mechanism: Implement a background thread or timer that periodically checks for changes in the active window.

            Change Detection: Compare the current active window information with the previously logged information and only log a new entry if a change is detected.

        References between Files:

            WindowMonitor.cs: Contains the logic for polling active windows, calling Win32 APIs, and detecting changes.

            PInvoke.cs (auto-generated by CsWin32): Provides the C# wrappers for Win32 API calls.

            ActivityDataModel.cs: Includes fields for ActiveWindowHandle, ActiveWindowTitle, and ApplicationProcessName.

            ActivityLogger.cs: Receives data from WindowMonitor.cs and prepares it for storage.

        Acceptance Criteria:

            When the active window changes, a new log entry is created.

            Each log entry accurately reflects the ActiveWindowTitle and ApplicationProcessName of the foreground application at the time of change.

            No duplicate entries are created if the active window remains unchanged.

        Testing Plan:

            Test Case 1: Active Window Change Detection

                Test Data: Open multiple applications (e.g., Chrome, Notepad, Word), switch between them.

                Expected Result: Log entries are created each time the active window changes, showing the correct title and process name for each application.

                Testing Tool: Manual application switching, local database log inspection.

    User Story 4: Basic Activity Detection (Binary)

        Description: As the monitoring system, I want to detect the presence of any keyboard input (key press) or mouse input (movement or click) within a short polling interval, and log the activity status (active/inactive), so that a binary indication of user engagement can be determined without logging specific input details.

        Actions to Undertake:

            Global Input Hooks: Implement global low-level keyboard (WH_KEYBOARD_LL) and mouse (WH_MOUSE_LL) hooks using SetWindowsHookEx() via CsWin32. Note: Global hooks from a Windows Service require careful handling of user sessions and desktop interaction. This will likely involve running the service under a specific user account or handling session changes.

            Input Event Handling: Create callback functions (LowLevelKeyboardProc, LowLevelMouseProc) to capture input events.

            Activity Status Update: On detection of any keyboard or mouse input, set an internal flag indicating "active".

            Polling for Inactivity: Periodically check the activity flag; if no input has been detected within a defined interval (e.g., 30 seconds), set the status to "inactive".

            Log Activity Status: Include the ActivityStatus (e.g., "Active" or "Inactive") in the activity data model.

        References between Files:

            InputMonitor.cs: Contains the logic for setting up and processing global input hooks, and determining activity status.

            PInvoke.cs (auto-generated by CsWin32): Provides the C# wrappers for SetWindowsHookEx and related hook structures.

            ActivityDataModel.cs: Includes a field for ActivityStatus.

            ActivityLogger.cs: Receives activity status from InputMonitor.cs and prepares it for storage.

        Acceptance Criteria:

            Any keyboard press or mouse movement/click correctly registers as "Active" status.

            After a configurable period of no input, the status correctly transitions to "Inactive".

            The ActivityStatus is accurately logged with activity records.

            No specific keystrokes or mouse coordinates are logged.

        Testing Plan:

            Test Case 1: Active State Verification

                Test Data: User actively typing and moving mouse.

                Expected Result: ActivityStatus in logs consistently shows "Active".

                Testing Tool: Manual user interaction, local database log inspection.

            Test Case 2: Inactive State Verification

                Test Data: User leaves computer idle for a period longer than the inactivity threshold.

                Expected Result: ActivityStatus in logs transitions to "Inactive" after the threshold.

                Testing Tool: Manual idle period, local database log inspection.

    User Story 5: Local Data Storage

        Description: As the monitoring system, I want all collected data (timestamp, Windows username, active window title, application name, activity status) to be stored reliably in a local SQLite database, serving as a primary record or backup, with a schema designed for efficient querying and future expansion, and data appended chronologically.

        Actions to Undertake:

            SQLite Integration: Add Microsoft.Data.Sqlite NuGet package to the project.

            Database Initialization: Implement logic to create the SQLite database file (TimeTracker.db) and its schema if it doesn't exist.

            Schema Definition: Define a database table (e.g., ActivityLogs) with columns for Timestamp (UTC), WindowsUsername, ActiveWindowTitle, ApplicationProcessName, and ActivityStatus.

            Data Insertion: Implement methods to insert new activity records into the ActivityLogs table.

            Chronological Appending: Ensure new records are always appended, implicitly ordered by timestamp.

        References between Files:

            SQLiteDataAccess.cs: Handles all interactions with the SQLite database (connection, schema creation, data insertion).

            ActivityDataModel.cs: Maps C# objects to database table rows.

            TimeTracker.db: The actual SQLite database file created on the local machine.

            ActivityLogger.cs: Orchestrates the saving of data to the SQLite database via SQLiteDataAccess.cs.

        Acceptance Criteria:

            A TimeTracker.db file is created locally upon first run.

            The ActivityLogs table exists with the correct schema.

            All collected data points are successfully written to the ActivityLogs table.

            Data is appended chronologically.

            No data loss occurs during normal operation.

        Testing Plan:

            Test Case 1: Database Creation and Schema Verification

                Test Data: Fresh installation.

                Expected Result: TimeTracker.db file exists, and its schema matches the defined structure.

                Testing Tool: SQLite Browser/CLI, manual file system check.

            Test Case 2: Data Persistence

                Test Data: Run application, generate activity, close application, restart application.

                Expected Result: Previously logged data is still present in the database after restart.

                Testing Tool: Local database log inspection.

    User Story 6: Data Submission to Pipedream Endpoint (Testing)

        Description: As the monitoring system, I want to transmit the captured activity data (timestamp, Windows username, active window title, application name, activity status) as a JSON payload to a configurable Pipedream HTTP endpoint, primarily for testing the data submission mechanism, and handle potential network errors gracefully.

        Actions to Undertake:

            HTTP Client Setup: Use System.Net.Http.HttpClient for sending data.

            JSON Serialization: Serialize ActivityDataModel objects into JSON format using System.Text.Json.

            Endpoint Configuration: Implement a mechanism to read the Pipedream endpoint URL from a configuration file (e.g., appsettings.json).

            Data Transmission Logic: Create a method to send the JSON payload via an HTTP POST request to the configured URL.

            Error Handling & Retry: Implement try-catch blocks for network errors, log errors, and implement a basic retry mechanism (e.g., exponential backoff) if the endpoint is unavailable, without stopping local storage.

        References between Files:

            PipedreamClient.cs: Encapsulates HTTP client logic, JSON serialization, and error handling for Pipedream submission.

            AppSettings.json: Stores the configurable Pipedream endpoint URL.

            ActivityDataModel.cs: The object being serialized to JSON.

            ActivityLogger.cs: Calls PipedreamClient.cs to submit data after local storage.

        Acceptance Criteria:

            Captured data is successfully sent to the configured Pipedream endpoint.

            The data received by Pipedream is in valid JSON format.

            The application continues to store data locally even if Pipedream submission fails.

            Network errors are logged, and the application attempts to retry submission.

        Testing Plan:

            Test Case 1: Successful Data Submission

                Test Data: Valid Pipedream endpoint URL configured, network active.

                Expected Result: Pipedream endpoint receives JSON payloads matching the activity data.

                Testing Tool: Pipedream inspection, application logs.

            Test Case 2: Network Error Handling

                Test Data: Invalid Pipedream URL, or Pipedream endpoint temporarily down, network active.

                Expected Result: Application logs network errors, continues local storage, and attempts retries.

                Testing Tool: Simulate network issues (e.g., block URL in firewall), application logs, local database inspection.

    User Story 7: Installation Package

        Description: As an IT Administrator, I want a simple installer package (e.g., MSI, setup.exe) for the application that requires administrative privileges, so that it can be easily deployed on Windows machines across the organization.

        Actions to Undertake:

            Installer Project Creation: Create an installer project (e.g., using WiX Toolset, or a Visual Studio Installer Project if available for .NET 8) specifically for Windows Services.

            File Inclusion: Configure the installer to include all necessary application binaries, configuration files, and dependencies.

            Privilege Requirement: Ensure the installer explicitly requires administrative privileges for execution.

            Service Installation & Startup Configuration: Configure the installer to install the application as a Windows Service and set it up for automatic startup.

        References between Files:

            InstallerProject.wixproj (or similar .vdproj for VS Installer): Defines the installer package structure and rules for Windows Service installation.

            TimeTracker.DesktopApp.exe: The main executable to be packaged as a service.

            appsettings.json: Configuration file to be deployed.

        Acceptance Criteria:

            An executable installer package (.msi or .exe) is generated.

            Running the installer prompts for administrative privileges.

            The application is successfully installed as a Windows Service in the specified directory.

            The Windows Service starts automatically after installation and system boot.

        Testing Plan:

            Test Case 1: Standard Installation of Service

                Test Data: Clean Windows 10/11 VM.

                Expected Result: Installer runs, prompts for admin, installs successfully, the service appears in Services Manager and starts automatically, and the application functions as expected post-installation.

                Testing Tool: Manual installation, Windows Services Manager, system reboot.

            Test Case 2: Uninstallation of Service

                Test Data: Installed application.

                Expected Result: Application service is completely removed from the system, including files and registry entries.

                Testing Tool: Control Panel > Programs and Features, Windows Services Manager, manual file system/registry checks.

Actions to Undertake (Consolidated)

    Project Initialization:

        Create a monorepo structure (timetracker-monorepo, desktop-app, webserver, shared).

        Initialize Git repository.

        Create a C# .NET 8 Windows Service application project (TimeTracker.DesktopApp) inside desktop-app.

        Add a root solution file (timetracker.sln) and link the desktop app project.

        Add .gitignore and README.md.

    Core Application Development:

        Implement background execution logic as a Windows Service.

        Develop startup mechanism for auto-launch as a Windows Service.

        Implement Windows username capture.

        Develop active window monitoring logic (polling, Win32 API calls).

        Implement basic activity detection using global low-level keyboard and mouse hooks, considering service session isolation.

    Data Management:

        Integrate Microsoft.Data.Sqlite NuGet package.

        Define and implement SQLite database schema for ActivityLogs.

        Develop data insertion logic for SQLite, ensuring chronological appending.

    Network Communication (Testing):

        Integrate System.Net.Http.HttpClient.

        Implement JSON serialization for activity data.

        Develop logic for sending JSON payloads to a configurable Pipedream HTTP endpoint.

        Implement robust error handling and retry mechanisms for network failures.

    Configuration:

        Set up appsettings.json for configurable parameters like Pipedream URL.

    Deployment:

        Create an installer project (e.g., WiX Toolset) to package the application as a Windows Service.

        Configure the installer to require administrative privileges and set up auto-startup for the service.

    Performance Optimization:

        Continuously monitor and optimize CPU and memory usage during development.

        Ensure efficient API calls and background processing.

References between Files

    timetracker-monorepo/ (Root):

        timetracker.sln: Links desktop-app/TimeTracker.DesktopApp/TimeTracker.DesktopApp.csproj.

        .gitignore: Controls version control for all sub-folders.

        README.md: Provides overall project documentation.

    desktop-app/TimeTracker.DesktopApp/:

        TimeTracker.DesktopApp.csproj: References Microsoft.Data.Sqlite, Microsoft.Windows.CsWin32, System.Net.Http (built-in), potentially Microsoft.Extensions.Hosting.WindowsServices.

        Program.cs (or Service.cs): Orchestrates Windows Service startup, calls WindowMonitor, InputMonitor, ActivityLogger.

        ActivityDataModel.cs: Defines the structure of data passed between WindowMonitor, InputMonitor, ActivityLogger, SQLiteDataAccess, and PipedreamClient.

        WindowMonitor.cs: Calls PInvoke.cs for Win32 APIs, sends data to ActivityLogger.

        InputMonitor.cs: Calls PInvoke.cs for Win32 APIs, sends data to ActivityLogger.

        PInvoke.cs (auto-generated): Provides C# declarations for Windows API functions (e.g., GetForegroundWindow, SetWindowsHookEx).

        ActivityLogger.cs: Receives data from WindowMonitor and InputMonitor, sends data to SQLiteDataAccess and PipedreamClient.

        SQLiteDataAccess.cs: Interacts with TimeTracker.db file, uses Microsoft.Data.Sqlite library.

        PipedreamClient.cs: Uses System.Net.Http.HttpClient, reads appsettings.json, serializes ActivityDataModel to JSON.

        appsettings.json: Read by PipedreamClient.cs for endpoint URL.

    desktop-app/InstallerProject/ (e.g., WiX project):

        InstallerProject.wixproj: References TimeTracker.DesktopApp.exe, appsettings.json for packaging and Windows Service installation.

        Generates TimeTrackerInstaller.msi (or similar).

    External Dependencies:

        Windows OS (for Win32 APIs, Registry, Windows Services).

        .NET 8 SDK.

        Pipedream HTTP endpoint (for testing data submission).

List of Files being Created

    File 1: timetracker-monorepo/.gitignore

        Purpose: Specifies intentionally untracked files that Git should ignore.

        Contents: Standard .NET and Visual Studio ignore patterns, plus bin/, obj/, TimeTracker.db, *.suo, *.user, *.vs/, node_modules/ (for future webserver).

        Relationships: Applies to all files within the monorepo.

    File 2: timetracker-monorepo/README.md

        Purpose: Provides an overview of the project, setup instructions, and repository structure.

        Contents: Project description, monorepo layout, build instructions, how to run, key features.

        Relationships: Top-level documentation for the entire repository.

    File 3: timetracker-monorepo/timetracker.sln

        Purpose: Visual Studio Solution file to organize and build all C# projects within the monorepo.

        Contents: References to TimeTracker.DesktopApp.csproj.

        Relationships: Parent solution for TimeTracker.DesktopApp.csproj.

    File 4: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/TimeTracker.DesktopApp.csproj

        Purpose: C# project file for the desktop monitoring application, built as a Windows Service.

        Contents: Project SDK (Microsoft.NET.Sdk), target framework (net8.0-windows), output type (Exe or WinExe for a headless service), package references (Microsoft.Data.Sqlite, Microsoft.Windows.CsWin32, potentially Microsoft.Extensions.Hosting.WindowsServices).

        Relationships: Part of timetracker.sln, depends on all other C# source files in the TimeTracker.DesktopApp directory.

    File 5: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/Program.cs

        Purpose: Entry point of the desktop application, serving as the Windows Service host.

        Contents: Main method, Windows Service hosting logic, initialization of monitors and loggers.

        Relationships: Calls WindowMonitor, InputMonitor, ActivityLogger.

    File 6: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/ActivityDataModel.cs

        Purpose: Defines the data structure for captured activity records.

        Contents: C# class with properties for Timestamp (DateTime), WindowsUsername (string), ActiveWindowTitle (string), ApplicationProcessName (string), ActivityStatus (enum/string: "Active", "Inactive").

        Relationships: Used by WindowMonitor, InputMonitor, ActivityLogger, SQLiteDataAccess, PipedreamClient.

    File 7: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/WindowMonitor.cs

        Purpose: Handles active window tracking.

        Contents: Logic for polling GetForegroundWindow, extracting window title and process name, and detecting changes.

        Relationships: Calls methods in PInvoke.cs, sends ActivityDataModel to ActivityLogger.

    File 8: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/InputMonitor.cs

        Purpose: Manages global keyboard and mouse input hooks for activity detection.

        Contents: Logic for SetWindowsHookEx, LowLevelKeyboardProc, LowLevelMouseProc callbacks, and determining activity status.

        Note: This component will need to consider the complexities of running global hooks from a Windows Service, potentially requiring the service to interact with the user's desktop session.

        Relationships: Calls methods in PInvoke.cs, sends ActivityDataModel (with activity status) to ActivityLogger.

    File 9: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/PInvoke.cs

        Purpose: Auto-generated C# wrappers for Windows API functions.

        Contents: [DllImport] declarations for GetForegroundWindow, GetWindowText, GetWindowTextLength, GetWindowThreadProcessId, SetWindowsHookEx, CallNextHookEx, UnhookWindowsHookEx, etc.

        Relationships: Used by WindowMonitor.cs and InputMonitor.cs.

    File 10: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/ActivityLogger.cs

        Purpose: Centralized logging component that orchestrates data storage and submission.

        Contents: Methods to receive ActivityDataModel objects and pass them to SQLiteDataAccess and PipedreamClient.

        Relationships: Receives data from WindowMonitor and InputMonitor, calls SQLiteDataAccess and PipedreamClient.

    File 11: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/SQLiteDataAccess.cs

        Purpose: Handles all interactions with the local SQLite database.

        Contents: Methods for creating the database file and schema, inserting ActivityDataModel records, and managing database connections.

        Relationships: Interacts with TimeTracker.db, uses Microsoft.Data.Sqlite library.

    File 12: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/PipedreamClient.cs

        Purpose: Manages data transmission to the Pipedream endpoint.

        Contents: Methods for serializing ActivityDataModel to JSON, sending HTTP POST requests, and handling network errors/retries.

        Relationships: Uses System.Net.Http.HttpClient, reads appsettings.json, receives ActivityDataModel for serialization.

    File 13: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/appsettings.json

        Purpose: Configuration file for application settings.

        Contents: JSON key-value pairs, including the PipedreamEndpointUrl.

        Relationships: Read by PipedreamClient.cs.

    File 14: timetracker-monorepo/desktop-app/TimeTracker.DesktopApp/TimeTracker.db

        Purpose: Local SQLite database file for storing activity logs.

        Contents: SQLite database containing the ActivityLogs table.

        Relationships: Managed by SQLiteDataAccess.cs.

    File 15: timetracker-monorepo/desktop-app/InstallerProject/InstallerProject.wixproj (or similar for VS Installer)

        Purpose: Project file for creating the Windows installer package, specifically for the Windows Service.

        Contents: XML definitions for installer properties, files to include, shortcuts, registry entries for auto-startup, and administrative privilege requirements, with specific configurations for installing and managing a Windows Service.

        Relationships: Packages the compiled TimeTracker.DesktopApp.exe and appsettings.json.

    File 16: timetracker-monorepo/desktop-app/InstallerProject/TimeTrackerInstaller.msi (or .exe)

        Purpose: The final executable installer package for the application, designed to install the Windows Service.

        Contents: Compiled installer logic and packaged application files.

        Relationships: Deploys the application as a Windows Service to target Windows machines.

Acceptance Criteria (Consolidated)

    Installation & Startup:

        The application installs silently (post-admin prompt) as a Windows Service.

        The Windows Service starts automatically on system boot (or user login, depending on service configuration).

        No user interface is visible during operation.

        The Windows Service is visible and running in the Windows Services Manager.

    Data Capture:

        The correct Windows username is captured and logged for each activity record.

        Active window titles and associated application process names are captured accurately when the foreground window changes.

        Binary activity status (active/inactive) is correctly determined and logged based on keyboard/mouse input presence.

        Timestamps are recorded in UTC.

    Local Storage:

        All captured data points are persistently stored in the local SQLite database.

        The SQLite database schema is correctly defined and supports chronological data appending.

        Data integrity is maintained across application restarts.

    Data Submission (Testing):

        Captured data is successfully transmitted as a JSON payload to the configured Pipedream HTTP endpoint.

        The JSON payload is correctly formatted and contains all required data points.

        The application handles network errors gracefully, logging failures and attempting retries without interrupting local data storage.

    Performance:

        The client application's average CPU usage remains below 5%.

        The client application's memory usage remains below 100MB.

Testing Plan

The testing plan for Phase 1 MVP will involve a combination of unit testing, integration testing, and system testing.

    Unit Testing:

        Scope: Individual components like WindowMonitor (mocking Win32 API calls), InputMonitor (mocking hook callbacks), SQLiteDataAccess (mocking database interactions), PipedreamClient (mocking HTTP responses).

        Methodology: Use a unit testing framework (e.g., NUnit, xUnit) to verify the logic of each class in isolation.

        Tools: NUnit/xUnit, Moq (for mocking dependencies).

        Test Case 1: WindowMonitor - Active Window Change

            Test Data: Mock GetForegroundWindow to return different handles, mock GetWindowText and GetWindowThreadProcessId to return corresponding data.

            Expected Result: WindowMonitor correctly identifies and reports window changes.

            Testing Tool: NUnit.

        Test Case 2: SQLiteDataAccess - Data Insertion

            Test Data: ActivityDataModel object.

            Expected Result: Data is correctly inserted into a mock/in-memory SQLite database.

            Testing Tool: NUnit.

        Test Case 3: PipedreamClient - JSON Serialization

            Test Data: ActivityDataModel object.

            Expected Result: Object is serialized into a valid JSON string with correct fields.

            Testing Tool: NUnit.

    Integration Testing:

        Scope: Verify the interaction between multiple components (e.g., WindowMonitor with ActivityLogger and SQLiteDataAccess).

        Methodology: Run tests that involve actual data flow between connected components.

        Tools: NUnit/xUnit, actual SQLite database file, local web server (e.g., a simple Python Flask app) to simulate Pipedream.

        Test Case 1: End-to-End Local Logging

            Test Data: Simulate user activity (programmatically or manually).

            Expected Result: Activity data flows from monitors, through logger, and is correctly stored in the local SQLite database.

            Testing Tool: NUnit, SQLite Browser/CLI.

        Test Case 2: Pipedream Submission Integration

            Test Data: Valid Pipedream endpoint URL, generated activity data.

            Expected Result: Data is successfully sent to Pipedream, and Pipedream logs show correct JSON.

            Testing Tool: Pipedream inspection, application logs.

    System Testing (Manual & Automated):

        Scope: Validate the entire application's functionality on target Windows environments, specifically as a Windows Service.

        Methodology: Install the application via the generated installer, simulate real-world usage, and monitor system behavior.

        Tools: Windows Task Manager, Process Explorer, Performance Monitor, SQLite Browser/CLI, Pipedream logs, network monitoring tools (e.g., Wireshark for HTTPS traffic confirmation), Windows Services Manager.

        Test Case 1: Full Installation & Auto-Startup of Service

            Test Data: Clean Windows 10/11 VM.

            Expected Result: Installer runs successfully, the Windows Service appears in Services Manager and starts automatically on system boot, and the application functions as expected post-installation.

            Testing Tool: Manual installation, Windows Services Manager, Task Manager, system reboot.

        Test Case 2: Performance Impact

            Test Data: Application running as a service for extended periods (e.g., 8 hours) during normal user activity.

            Expected Result: CPU usage consistently below 5%, RAM usage below 100MB.

            Testing Tool: Windows Performance Monitor, Task Manager.

        Test Case 3: Robustness (Network Outage)

            Test Data: Disconnect network cable or block Pipedream URL via firewall while the service is running.

            Expected Result: Application continues to log data locally, logs errors for Pipedream submission, and attempts retries when network is restored.

            Testing Tool: Manual network manipulation, application logs, local database inspection.

        Test Case 4: Service Start/Stop/Restart

            Test Data: Manually start, stop, and restart the TimeTracker.DesktopApp service via Services Manager.

            Expected Result: The service responds correctly to start, stop, and restart commands, and resumes activity monitoring after restart.

            Testing Tool: Windows Services Manager, local database log inspection.

Assumptions and Dependencies

    Assumptions:

        The target operating system is Windows 10 or Windows 11 (x64).

        Users will have administrative privileges for installation of the Windows Service.

        A Pipedream account and a configurable HTTP endpoint will be available for testing data submission.

        Legal and HR review of the data points and monitoring methods (including Pipedream submission) will be completed and approved before development begins.

        Employee acceptance will be managed through clear communication and transparency as per company policy.

        Windows Service interaction with user desktop (for active window and input hooks) will be managed appropriately (e.g., running the service under a specific user account or handling session changes).

    Dependencies:

        .NET 8 SDK: Required for development and runtime.

        Visual Studio 2022 (or Rider/VS Code with C# Dev Kit): Development environment.

        Microsoft.Data.Sqlite NuGet package: For SQLite database interaction.

        Microsoft.Windows.CsWin32 NuGet package: For simplified Win32 API interop.

        WiX Toolset (or similar installer creation tool): For building the MSI/setup package for a Windows Service.

        Windows API calls: Reliance on user32.dll, kernel32.dll for system monitoring.

        Pipedream: A third-party service used only for testing the data submission mechanism in Phase 1. No sensitive production data will be sent to Pipedream.

        Microsoft.Extensions.Hosting.WindowsServices (Recommended): For easier implementation of .NET Core/5+ Windows Services.

Non-Functional Requirements

    Performance:

        The client application (running as a Windows Service) must have minimal impact on system performance, with average CPU usage consistently below 5% and memory usage below 100MB.

        Input hooks must not introduce noticeable latency or slowdowns to user interaction.

    Reliability:

        The client application (as a Windows Service) should be stable and recover gracefully from errors or system restarts (e.g., by automatically restarting and continuing local data collection).

        Data collection should be consistent and continuous during active user sessions.

        The data submission to Pipedream (MVP) should not critically fail the application if the endpoint is unavailable; local storage must continue.

    Security:

        Data in transit (client to Pipedream) must be encrypted (HTTPS).

        Data at rest (local SQLite database) should be protected from unauthorized access (e.g., by storing the database file in a secure location and managing service account permissions).

        The client application (as a Windows Service) should be difficult to tamper with or disable by non-administrative users.

        Crucially, the system will never log the actual content of keystrokes.

    Installability:

        The client application must be easy to deploy via a simple installer package, specifically designed for Windows Services, potentially supporting silent installation methods for enterprise environments.

    Maintainability:

        Code should be well-documented, modular, and follow C#/.NET best practices.

        Configuration settings (like the Pipedream URL) should be easily adjustable without recompilation.

    Privacy & Ethical Considerations:

        The system adheres to the principle of data minimization, collecting only necessary data.

        Transparency with employees regarding data collection is crucial, as per company policy.

Conclusion

This detailed backlog outlines the requirements, tasks, and considerations for Phase 1 of the Employee Activity Monitor MVP, with a key decision to build the desktop application as a Windows Service. This approach ensures robust background operation, system-level persistence, and enhanced reliability. By focusing on core background monitoring, local data persistence, and test data submission, this phase lays a solid foundation for future enhancements. Adherence to the defined user stories, acceptance criteria, and testing plans will ensure a robust and reliable initial release, while careful attention to non-functional requirements will guarantee a performant, secure, and maintainable application.