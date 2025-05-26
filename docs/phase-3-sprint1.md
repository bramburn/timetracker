Desktop App Transition Backlog
Introduction

This backlog details the comprehensive plan to re-architect the TimeTracker application from a Windows Service to a user-friendly desktop application with system tray integration. This transition aims to enhance user experience, provide direct control over tracking, and align the application's architecture with industry best practices for time-tracking software, as seen in tools like Hubstaff. The existing core logic for activity monitoring, data storage, and Pipedream submission will be integrated into the new desktop application framework.
User Stories

    User Story 1: System Tray Icon and Basic Interaction

        Description: As an end user, I want the TimeTracker application to run silently in the system tray, providing a visual indicator of its status and allowing me to perform basic actions like starting, pausing, or stopping time tracking directly from the tray icon's context menu.

        Actions to Undertake:

            Project Setup: Create a new C# Windows Forms or WPF project (preferring Windows Forms for simplicity and compatibility with existing Form usage in OptimizedInputMonitor).

            Main Application Context: Implement an ApplicationContext class to manage the lifecycle of the tray icon and ensure the application runs in the background.

            Tray Icon Creation: Add a NotifyIcon component to the application, setting its icon and initial tooltip.

            Context Menu: Design and implement a context menu for the NotifyIcon with options for "Start Tracking", "Pause Tracking", "Stop Tracking", "Settings", "View Status", and "Exit".

            Event Handling: Wire up click events for each context menu item to trigger corresponding actions in the application's core logic.

            Status Indication: Change the NotifyIcon's icon or tooltip text to visually represent the current tracking status (e.g., active, paused, inactive).

            Initial State: Configure the application to start minimized to the system tray on launch.

        References between Files:

            Program.cs (new desktop app entry point) will instantiate and run the ApplicationContext.

            TrayIconManager.cs (new) will manage the NotifyIcon and its context menu.

            TrayIconManager.cs will interact with ActivityLogger.cs (existing) to control tracking state (StartAsync, Stop, PauseTracking - new method).

            TrayIconManager.cs will query ActivityLogger.cs for status information (GetStatusInfo).

            OptimizedInputMonitor.cs (existing) and OptimizedWindowMonitor.cs (existing) will continue to feed data to ActivityLogger.cs.

        Acceptance Criteria:

            The application launches and displays a distinct icon in the Windows system tray.

            Right-clicking the tray icon displays a context menu with at least "Start Tracking", "Pause Tracking", "Stop Tracking", "Settings", "View Status", and "Exit" options.

            Clicking "Exit" gracefully shuts down the application and removes the tray icon.

            Clicking "Start Tracking" changes the tray icon's visual state to indicate active tracking.

            Clicking "Pause Tracking" changes the tray icon's visual state to indicate paused tracking.

            Clicking "Stop Tracking" changes the tray icon's visual state to indicate inactive tracking.

            The tooltip text of the tray icon accurately reflects the current tracking status (e.g., "TimeTracker: Active", "TimeTracker: Paused").

        Testing Plan:

            Test Case 1: Tray Icon Visibility and Context Menu

                Test Data: Clean Windows 10/11 installation, application installed.

                Expected Result: Application launches, tray icon is visible. Right-click displays all expected menu items.

                Testing Tool: Manual testing, UI automation (e.g., TestStack.White or FlaUI) for menu item presence.

            Test Case 2: Tracking State Changes via Tray Menu

                Test Data: Application running, initial state inactive.

                Expected Result: Clicking "Start Tracking" activates tracking and updates icon. Clicking "Pause Tracking" pauses tracking and updates icon. Clicking "Stop Tracking" stops tracking and updates icon.

                Testing Tool: Manual testing, unit tests for ActivityLogger's state changes.

            Test Case 3: Application Exit

                Test Data: Application running with tracking active.

                Expected Result: Clicking "Exit" closes the application, removes the tray icon, and ensures no lingering processes.

                Testing Tool: Manual testing, Task Manager to verify process termination.

    User Story 2: User Session Awareness and Auto-Start

        Description: As a user, I want the TimeTracker application to automatically start when I log into Windows and to accurately track my activity only when my session is active (not locked or disconnected), ensuring relevant data collection.

        Actions to Undertake:

            Auto-Start Implementation: Implement logic to add/remove a registry entry (e.g., HKCU\Software\Microsoft\Windows\CurrentVersion\Run) to enable/disable auto-start with Windows.

            Session Change Detection: Utilize Windows API (e.g., WTSRegisterSessionNotification and WM_WTSSESSION_CHANGE messages) to detect session lock/unlock, logoff, and remote desktop connect/disconnect events.

            Pause/Resume Tracking on Session Change: Implement logic within ActivityLogger to pause tracking when the session is locked or disconnected, and resume when it becomes active.

            Configuration UI for Auto-Start: Add a checkbox or similar control in the settings window to allow users to enable or disable auto-start.

        References between Files:

            Program.cs (new desktop app entry point) will handle initial auto-start logic.

            SessionMonitor.cs (new) will encapsulate Windows API calls for session change detection.

            SessionMonitor.cs will notify ActivityLogger.cs of session state changes.

            ActivityLogger.cs (existing, with new methods) will implement PauseTracking() and ResumeTracking() methods.

            SettingsWindow.xaml.cs (new) or SettingsForm.cs (new) will interact with auto-start settings.

            NativeMethods.cs (existing) will be updated with WTSRegisterSessionNotification and related P/Invoke declarations.

        Acceptance Criteria:

            If auto-start is enabled, the application launches automatically when the user logs into Windows.

            When the user locks their workstation, tracking pauses within 5 seconds.

            When the user unlocks their workstation, tracking resumes within 5 seconds.

            Tracking is not active when the user is logged off or disconnected from a remote session.

            A setting exists in the UI to enable/disable auto-start functionality.

        Testing Plan:

            Test Case 1: Auto-Start Functionality

                Test Data: Auto-start enabled in settings.

                Expected Result: Reboot machine, log in, application starts automatically and tray icon appears.

                Testing Tool: Manual testing, Registry Editor to verify Run key.

            Test Case 2: Session Lock/Unlock

                Test Data: Tracking active.

                Steps: 1. Lock workstation. 2. Wait 10 seconds. 3. Unlock workstation.

                Expected Result: Tracking pauses when locked, resumes when unlocked. Verify through logs or status display.

                Testing Tool: Manual testing, system event viewer (for session changes), application logs.

            Test Case 3: User Logoff/Logon

                Test Data: Tracking active.

                Steps: 1. Log off. 2. Log in.

                Expected Result: Application gracefully shuts down on logoff and restarts on logon, resuming tracking.

                Testing Tool: Manual testing, Task Manager to verify process termination and restart.

    User Story 3: Configuration Settings UI

        Description: As a user, I want a dedicated settings window where I can view and modify application configurations, such as the Pipedream endpoint URL and data submission intervals, to customize the application's behavior.

        Actions to Undertake:

            Settings Window Creation: Design and implement a new UI window (e.g., SettingsWindow.xaml for WPF or SettingsForm.cs for WinForms) to display and edit configuration values.

            Data Binding/Loading: Load current configuration values from appsettings.json (via IConfiguration) into the settings UI.

            Save Functionality: Implement logic to save updated settings back to appsettings.json (or a user-specific config file) and trigger a reload of relevant services (e.g., PipedreamClient).

            Pipedream Connection Test: Add a button to the settings UI to trigger a connection test to the configured Pipedream endpoint, displaying the result to the user.

            Validation: Implement input validation for settings (e.g., valid URL format, numeric ranges).

        References between Files:

            SettingsWindow.xaml.cs / SettingsForm.cs (new) will be the UI for settings.

            IConfiguration (existing .NET component) will be used to read settings.

            ConfigurationManager.cs (new) will handle saving settings to a user-specific file (e.g., user.config or appsettings.user.json).

            PipedreamClient.cs (existing) will be used for TestConnectionAsync() and GetConfigurationStatus().

            ActivityLogger.cs (existing) might need a method to trigger a configuration reload or restart of PipedreamClient.

        Acceptance Criteria:

            A "Settings" option is available in the tray icon's context menu.

            Clicking "Settings" opens a dedicated window displaying configurable parameters (e.g., Pipedream URL, batch interval, activity timeout).

            Users can modify these parameters and save changes.

            Saved changes persist across application restarts.

            A "Test Connection" button is available for the Pipedream endpoint, and its result is clearly displayed.

            Invalid input (e.g., malformed URL) is prevented or flagged with an error message.

        Testing Plan:

            Test Case 1: Settings Persistence

                Test Data: Modify Pipedream URL in settings, save, restart app.

                Expected Result: New Pipedream URL is loaded and reflected in the settings window and logs.

                Testing Tool: Manual testing, file system inspection (appsettings.json or user.config).

            Test Case 2: Pipedream Connection Test

                Test Data: Valid Pipedream URL, Invalid Pipedream URL.

                Expected Result: "Test Connection" button accurately reports success/failure based on URL validity.

                Testing Tool: Manual testing, mock HTTP server for PipedreamClient in unit tests.

            Test Case 3: Input Validation

                Test Data: Enter non-numeric value for interval, invalid URL.

                Expected Result: Application prevents saving or displays an error message without crashing.

                Testing Tool: Manual testing.

    User Story 4: Real-time Status Display

        Description: As a user, I want to see a quick, real-time overview of my current activity (e.g., active window, time since last input) and the application's operational status (e.g., Pipedream connection status, pending submissions) without opening the main settings window.

        Actions to Undertake:

            Quick Status Overlay: Implement a small, non-modal window or a custom NotifyIcon balloon tip that displays key status information when the tray icon is left-clicked.

            Data Refresh: Ensure the status information in the overlay refreshes periodically (e.g., every 5 seconds) or on demand.

            Display ActivityLogger.GetStatusInfo(): Parse and display the information provided by the existing ActivityLogger.GetStatusInfo() method.

        References between Files:

            TrayIconManager.cs (new) will be responsible for showing/hiding the status overlay.

            StatusOverlayWindow.xaml.cs (new) or StatusOverlayForm.cs (new) will be the UI for the overlay.

            ActivityLogger.cs (existing) will provide the GetStatusInfo() method.

            OptimizedWindowMonitor.cs (existing) and OptimizedInputMonitor.cs (existing) indirectly contribute data via ActivityLogger.

        Acceptance Criteria:

            Left-clicking the tray icon displays a small overlay or balloon tip.

            The overlay/tip shows the current active window title, time since last input, Pipedream connection status, and number of pending submissions.

            The displayed information updates automatically at regular intervals (e.g., every 5-10 seconds).

            The overlay/tip disappears when the user clicks away or after a short timeout.

        Testing Plan:

            Test Case 1: Status Overlay Content

                Test Data: Application running, user active in a specific application, Pipedream configured.

                Expected Result: Overlay shows correct active window title, time since last input, "Configured" Pipedream status, and "0" pending submissions (if no issues).

                Testing Tool: Manual testing, application logs.

            Test Case 2: Status Update Frequency

                Test Data: Application running, user changes active window multiple times.

                Expected Result: Overlay updates within 10 seconds to reflect new active window.

                Testing Tool: Manual testing, stopwatch.

Actions to Undertake (Consolidated)

    Project Restructuring and Service Removal:

        Discontinue the existing TimeTracker.DesktopApp Windows Service project. Its core logic will be integrated into a new desktop application.

        Create a new C# Desktop Application project (e.g., TimeTracker.DesktopApp.UI).

        Migrate existing core logic files (ActivityLogger.cs, BatchProcessor.cs, OptimizedWindowMonitor.cs, OptimizedInputMonitor.cs, PipedreamClient.cs, SqlServerDataAccess.cs, NativeMethods.cs, ActivityDataModel.cs, BackgroundTaskQueue.cs, Interfaces, Logging) from the former service project into this new desktop app project (or a shared library if preferred).

        Update the Program.cs file within the new desktop application to initialize the UI and run the application context, completely replacing the WindowsService host model.

        Remove the Microsoft.Extensions.Hosting.WindowsServices package reference from the new project, as it is no longer needed for a desktop application.

    System Tray Integration:

        Implement TrayIconManager.cs to handle NotifyIcon lifecycle, context menu creation, and event handling.

        Design and implement SettingsWindow.xaml (WPF) or SettingsForm.cs (WinForms) for configuration.

        Design and implement StatusOverlayWindow.xaml (WPF) or StatusOverlayForm.cs (WinForms) for quick status display.

    Session Management:

        Create SessionMonitor.cs to encapsulate Windows API calls for session change notifications.

        Modify ActivityLogger.cs to include PauseTracking() and ResumeTracking() methods, triggered by SessionMonitor.

        Update NativeMethods.cs with necessary P/Invoke for session notifications.

    Configuration Management:

        Implement ConfigurationManager.cs to handle reading from appsettings.json and writing to a user-specific configuration file (e.g., user.config or appsettings.user.json) for persistence of user-modified settings.

        Ensure IConfiguration is still used for initial application-wide settings.

    Dependency Injection Update:

        Adapt the existing DI setup (IServiceCollection) in the new Program.cs to properly inject dependencies into UI components (e.g., ActivityLogger, PipedreamClient into TrayIconManager and SettingsWindow).

    Error Handling & Logging:

        Ensure existing robust logging (FileLoggerProvider) is integrated into the desktop application.

        Implement user-friendly error messages and notifications for critical failures (e.g., Pipedream connection issues).

References between Files

    Program.cs (New):

        Depends on: TrayIconManager, ActivityLogger, IConfiguration, IServiceCollection.

        Interacts with: Initializes and runs the main application context, setting up DI.

    TrayIconManager.cs (New):

        Depends on: ActivityLogger (for control and status), SettingsWindow (to open settings), StatusOverlayWindow (to open status).

        Interacts with: NotifyIcon (UI component), ContextMenuStrip (UI component).

    SettingsWindow.xaml.cs / SettingsForm.cs (New):

        Depends on: IConfiguration, ConfigurationManager (for saving), IPipedreamClient (for connection test).

        Interacts with: UI controls (textboxes, buttons, checkboxes).

    StatusOverlayWindow.xaml.cs / StatusOverlayForm.cs (New):

        Depends on: ActivityLogger (for GetStatusInfo).

        Interacts with: UI labels to display status.

    SessionMonitor.cs (New):

        Depends on: NativeMethods.cs (for Windows API calls).

        Interacts with: ActivityLogger (notifies of session changes).

    ConfigurationManager.cs (New):

        Depends on: System.IO (for file operations), System.Text.Json (for serialization).

        Interacts with: appsettings.json (read), user.config or appsettings.user.json (read/write).

    ActivityLogger.cs (Existing, Modified):

        Depends on: IDataAccess, IPipedreamClient, IWindowMonitor, IInputMonitor, BackgroundTaskQueue, IConfiguration, ILogger.

        Interacts with: TrayIconManager (provides status), SessionMonitor (receives pause/resume commands).

        Data Flow: Receives ActivityDataModel from monitors, sends to IDataAccess and IPipedreamClient.

    BatchProcessor.cs (Existing):

        Depends on: IDataAccess, IPipedreamClient, IConfiguration, ILogger.

        Interacts with: SqlServerDataAccess (gets unsynced data, marks as synced, deletes), PipedreamClient (submits data).

    SqlServerDataAccess.cs (Existing):

        Depends on: Microsoft.Data.SqlClient, IConfiguration, ILogger.

        Interacts with: SQL Server database.

    PipedreamClient.cs (Existing):

        Depends on: System.Net.Http, IConfiguration, ILogger.

        Interacts with: External Pipedream API endpoint.

    OptimizedWindowMonitor.cs (Existing):

        Depends on: NativeMethods.cs, IConfiguration, ILogger.

        Interacts with: Windows OS (via SetWinEventHook), ActivityLogger (notifies of window changes).

    OptimizedInputMonitor.cs (Existing):

        Depends on: NativeMethods.cs, IConfiguration, ILogger, System.Windows.Forms.Form (for HiddenInputWindow).

        Interacts with: Windows OS (via Raw Input API / GetLastInputInfo), ActivityLogger (notifies of input activity status).

    NativeMethods.cs (Existing, Modified):

        Depends on: None (P/Invoke declarations).

        Interacts with: Windows API. New declarations for session management.

List of Files being Created

    File 1: TimeTracker.DesktopApp.csproj (New Project File)

        Purpose: Defines the new desktop application project, its dependencies, and build configurations.

        Contents: Project SDK (Microsoft.NET.Sdk.WindowsDesktop), target framework (net8.0-windows), UseWindowsForms or UseWPF set to true, references to core libraries and new UI components.

        Relationships: References existing core logic files (or a new shared library project).

    File 2: Program.cs (Modified/New for Desktop App)

        Purpose: The main entry point for the desktop application, responsible for setting up the application context, dependency injection, and running the UI loop.

        Contents: Main method, host builder configuration (removing AddWindowsService), service registration, and starting the ApplicationContext (for WinForms) or Application (for WPF).

        Relationships: Initializes TrayIconManager, ActivityLogger, and other services.

    File 3: TrayIconManager.cs

        Purpose: Manages the NotifyIcon (system tray icon) and its associated context menu, handling user interactions.

        Contents: Class definition, NotifyIcon instance, methods for showing/hiding the icon, creating the context menu, and handling menu item clicks. Event handlers for NotifyIcon events.

        Relationships: Interacts with ActivityLogger (via injected interface), SettingsWindow, StatusOverlayWindow.

    File 4: SettingsWindow.xaml (WPF) / SettingsForm.cs (WinForms)

        Purpose: Provides a user interface for configuring application settings.

        Contents: XAML/Designer definition for UI elements (textboxes, labels, buttons, checkboxes), code-behind for loading/saving settings and handling button clicks (e.g., "Save", "Test Pipedream Connection").

        Relationships: Interacts with IConfiguration, ConfigurationManager, IPipedreamClient.

    File 5: SettingsWindow.xaml.cs (WPF) / SettingsForm.Designer.cs & SettingsForm.cs (WinForms)

        Purpose: Code-behind for the settings window, containing logic for UI interactions and data handling.

        Contents: Event handlers for UI controls, methods to read/write configuration, and logic for testing Pipedream connection.

        Relationships: See SettingsWindow.xaml.

    File 6: StatusOverlayWindow.xaml (WPF) / StatusOverlayForm.cs (WinForms)

        Purpose: A small, non-modal window to display real-time activity status.

        Contents: XAML/Designer definition for UI elements (labels for active window, last input, Pipedream status, pending submissions), code-behind for updating these labels.

        Relationships: Receives data from ActivityLogger.

    File 7: StatusOverlayWindow.xaml.cs (WPF) / StatusOverlayForm.Designer.cs & StatusOverlayForm.cs (WinForms)

        Purpose: Code-behind for the status overlay window.

        Contents: Methods to update displayed information, potentially a timer for periodic refresh.

        Relationships: See StatusOverlayWindow.xaml.

    File 8: SessionMonitor.cs

        Purpose: Monitors Windows user session changes (lock/unlock, logon/logoff) and notifies relevant components.

        Contents: Implements WTSRegisterSessionNotification, WndProc override for message handling, events for session changes.

        Relationships: Uses NativeMethods.cs, notifies ActivityLogger.

    File 9: ConfigurationManager.cs

        Purpose: Provides methods for reading and writing application configuration, potentially handling user-specific overrides.

        Contents: Methods like LoadConfiguration(), SaveConfiguration(), potentially using System.Configuration or System.Text.Json to manage appsettings.json and a user-specific configuration file.

        Relationships: Used by SettingsWindow.

    File 10: app.manifest (New/Modified)

        Purpose: Specifies application manifest settings, including DPI awareness and UAC requirements.

        Contents: XML manifest for the desktop application.

        Relationships: Part of the project build.

Acceptance Criteria (Consolidated)

    Application Launch & Tray Icon:

        The application launches successfully and displays a distinct icon in the Windows system tray.

        The tray icon's tooltip accurately reflects the current tracking status (e.g., "TimeTracker: Active", "TimeTracker: Paused", "TimeTracker: Inactive").

    Tray Icon Context Menu:

        Right-clicking the tray icon displays a context menu with at least "Start Tracking", "Pause Tracking", "Stop Tracking", "Settings", "View Status", and "Exit" options.

        Each menu item is enabled/disabled appropriately based on the current application state (e.g., "Start Tracking" is disabled if already active).

    Tracking Control:

        Clicking "Start Tracking" initiates activity monitoring and data logging.

        Clicking "Pause Tracking" temporarily suspends activity monitoring and data logging.

        Clicking "Stop Tracking" fully halts activity monitoring and data logging.

        The application's internal state (as reflected by ActivityLogger) correctly transitions between active, paused, and inactive.

    Auto-Start with Windows:

        A user-configurable option exists in the settings to enable/disable automatic application launch on Windows login.

        If auto-start is enabled, the application launches automatically and minimized to the tray when the user logs in.

    Session Awareness:

        When the user locks their workstation, activity tracking pauses within 5 seconds.

        When the user unlocks their workstation, activity tracking resumes within 5 seconds.

        Activity is not logged while the workstation is locked or the user is logged off/disconnected.

    Settings Management UI:

        Clicking "Settings" from the tray menu opens a dedicated configuration window.

        The settings window displays current values for configurable parameters (e.g., Pipedream endpoint URL, batch interval, activity timeout).

        Users can modify these settings and save changes.

        Saved settings persist across application restarts.

        Input validation is performed on settings fields, preventing invalid values and providing user feedback.

    Pipedream Connection Test:

        The settings window includes a "Test Connection" button for the Pipedream endpoint.

        Clicking this button attempts a connection, and the result (success/failure) is displayed to the user.

    Real-time Status Overlay:

        Left-clicking the tray icon displays a small, non-modal overlay or balloon tip.

        The overlay displays the current active window title, time since last input, Pipedream connection status, and the number of pending data submissions.

        The information in the overlay updates automatically at regular intervals (e.g., every 5-10 seconds).

        The overlay/tip disappears when the user clicks away or after a short timeout.

    Graceful Shutdown:

        Clicking "Exit" from the tray menu or closing the main application window (if visible) gracefully shuts down all monitoring components, background tasks, and ensures no lingering processes.

Testing Plan (Consolidated)

    Overall Strategy: A combination of manual testing for UI/UX, unit tests for individual components, and integration tests for component interactions and end-to-end flows.

    Testing Environment: Windows 10/11 desktop environment, potentially with a mock Pipedream endpoint for controlled testing.

    Test Case 1: Application Installation and First Launch

        Test Data: Clean Windows VM, installer for the desktop app.

        Expected Result: Application installs successfully, launches to system tray, icon is visible.

        Testing Tool: Manual testing.

    Test Case 2: Tray Icon Context Menu Interaction

        Test Data: Application running, initial state inactive.

        Expected Result: Right-clicking tray icon displays all menu items. Clicking "Start Tracking" changes icon and tooltip. Clicking "Pause Tracking" changes icon and tooltip. Clicking "Stop Tracking" changes icon and tooltip.

        Testing Tool: Manual testing, UI automation (e.g., TestStack.White, FlaUI) to verify menu items and states.

    Test Case 3: Auto-Start Functionality

        Test Data: Auto-start enabled via settings.

        Expected Result: Reboot machine, log in, application starts automatically and tray icon appears. Disable auto-start, reboot, app does not start.

        Testing Tool: Manual testing, Windows Registry Editor (HKCU\Software\Microsoft\Windows\CurrentVersion\Run) for verification.

    Test Case 4: Session Lock/Unlock Behavior

        Test Data: Application tracking actively.

        Steps:

            Start tracking.

            Lock workstation (Win + L).

            Wait 10 seconds.

            Unlock workstation.

        Expected Result: Tracking status changes to "Paused" (or similar) when locked, and returns to "Active" when unlocked. No activity data is logged during the locked period.

        Testing Tool: Manual testing, application logs (to verify pause/resume events and data gaps).

    Test Case 5: Settings Persistence

        Test Data: Modify Pipedream URL to a test URL, change batch interval.

        Steps:

            Open settings window.

            Change Pipedream URL and batch interval.

            Save settings.

            Close and restart the application.

            Re-open settings.

        Expected Result: The modified settings are displayed correctly in the settings window.

        Testing Tool: Manual testing, inspecting the user configuration file (e.g., appsettings.user.json or user.config).

    Test Case 6: Pipedream Connection Test

        Test Data:

            Valid Pipedream endpoint URL (e.g., a mock API).

            Invalid Pipedream endpoint URL (e.g., "http://invalid-url.com").

            Empty Pipedream endpoint URL.

        Expected Result:

            Valid URL: "Connection successful" message.

            Invalid URL: "Connection failed" message with relevant error details.

            Empty URL: "Pipedream endpoint not configured" message.

        Testing Tool: Manual testing, unit tests for PipedreamClient with mocked HttpClient.

    Test Case 7: Real-time Status Overlay Accuracy

        Test Data: Application running, user actively using different applications (e.g., Notepad, Chrome).

        Steps:

            Left-click tray icon to open status overlay.

            Switch between applications.

            Leave computer idle for > activity timeout.

        Expected Result: Overlay updates to show correct active window title and process name. Time since last input updates. Activity status changes to "Inactive" after idle timeout.

        Testing Tool: Manual testing, stopwatch, comparison with actual system state.

    Test Case 8: Graceful Shutdown

        Test Data: Application tracking actively, with some pending data in the queue.

        Expected Result: Clicking "Exit" (or closing the main window) results in all components stopping cleanly, pending data being processed/saved if possible, and the application process terminating without errors.

        Testing Tool: Manual testing, Task Manager to verify process termination, application logs for shutdown messages.

Assumptions and Dependencies

    Operating System: Windows 10 (version 1809 or newer) or Windows 11.

    .NET Runtime: .NET 8.0 Desktop Runtime must be installed on the target machine.

    UI Framework: Windows Forms or WPF (decision to be made, but backlog assumes one will be chosen and consistently applied).

    Existing Core Logic: The current ActivityLogger, OptimizedWindowMonitor, OptimizedInputMonitor, BatchProcessor, PipedreamClient, and SqlServerDataAccess components are stable and will be directly integrated or slightly adapted.

    Configuration File: appsettings.json will continue to be used for default application settings, and a user-specific configuration file (e.g., appsettings.user.json or user.config) will be used for user-modifiable settings.

    Database: SQL Server LocalDB or a configured SQL Server instance is available for SqlServerDataAccess.

    Pipedream Endpoint: A functional Pipedream endpoint or a mock endpoint is available for data submission testing.

    Admin Privileges (Installer): The installer might require elevated privileges to set up auto-start for all users (if implemented that way) or to register for global input hooks (though OptimizedInputMonitor uses Raw Input which is more flexible). For per-user auto-start, admin privileges are not strictly required.

Non-Functional Requirements

    Performance:

        CPU Usage: The application should consume less than 2% CPU when idle and less than 5% CPU during active tracking (excluding data submission bursts).

        Memory Usage: Memory footprint should remain below 100 MB when idle.

        Responsiveness: UI interactions (tray menu, settings window) should be instantaneous (< 100 ms response time).

    Reliability:

        The application must run continuously without crashing or freezing.

        Data collection should be robust against temporary network outages or Pipedream endpoint unavailability (existing retry logic in PipedreamClient will be leveraged).

        Graceful shutdown should ensure no data loss for pending activities.

    Usability:

        Intuitive UI: The system tray icon and context menu should be easy to understand and navigate.

        Clear Status: The visual status indicators and status overlay should clearly communicate the application's state and user activity.

        User-Friendly Configuration: Settings should be clearly labeled and easy to modify.

    Security:

        Data Protection: Locally stored activity data must be secured (e.g., using Windows Data Protection API - DPAPI for sensitive config, though activity data itself might not be considered sensitive enough for encryption).

        Credential Storage: Any API keys or sensitive Pipedream credentials should be stored securely (e.g., Windows Credential Manager) if direct user input is required.

        Least Privilege: The application should run with the minimum necessary permissions.

    Maintainability:

        Codebase should follow C# best practices, SOLID principles, and be well-commented.

        Modular design should facilitate easy updates and feature additions.

    Portability:

        The application should be deployable as a single executable (if PublishSingleFile is enabled in .csproj) or a self-contained deployment.

        Installation process should be straightforward.

    Accessibility:

        Basic accessibility features for UI elements (e.g., keyboard navigation for context menu, screen reader compatibility for text labels).

Conclusion

This detailed backlog provides a clear roadmap for transforming the TimeTracker application into a user-centric desktop experience. By prioritizing system tray interaction, robust session awareness, and intuitive configuration, the application will meet modern user expectations while retaining its powerful background tracking capabilities. The outlined actions, file relationships, acceptance criteria, and testing plans will guide the development team through a successful transition, ensuring a reliable, performant, and user-friendly product.