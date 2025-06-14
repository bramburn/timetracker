# **Desktop App Transition Backlog: Phase 2**

## **Introduction**

This backlog outlines Phase 2 of the TimeTracker desktop application project. Building upon the foundational UI and basic heartbeat established in Phase 1, this phase focuses on enhancing the Pipedream data submission to include actual activity data, ensuring robust and graceful application exits, and introducing a basic user interface for configuring the Pipedream endpoint. The goal is to move towards a more functional and configurable MVP.

## **User Stories**

* **User Story 4**: As a system, I want the Pipedream heartbeat to include basic activity data (active window title, process name, and activity status) so that I can start collecting meaningful usage insights.  
  * **Description**: The application's periodic data submissions to the Pipedream endpoint should evolve from simple "MVP connectivity test" messages to include actual, basic activity data captured by the application's monitoring components. This includes the currently active window's title, its associated process name, and the user's activity status (active/inactive). This will enable initial analysis of user engagement patterns.  
  * **Actions to Undertake**:  
    1. **Modify ActivityLogger.cs**: Ensure that when LogActivityAsync is called (triggered by WindowChanged or ActivityStatusChanged events), the ActivityDataModel passed to IDataAccess.InsertActivityAsync contains the relevant ActiveWindowTitle, ApplicationProcessName, and ActivityStatus.  
    2. **Update BatchProcessor.cs**: Ensure the ProcessBatchAsync method correctly retrieves ActivityDataModel objects from IDataAccess.GetUnsyncedActivitiesAsync.  
    3. **Update PipedreamClient.cs**: Modify SubmitBatchDataAsync to correctly serialize a list of ActivityDataModel objects into a JSON array, ensuring all relevant fields (Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus) are included in the payload sent to Pipedream. Remove any hardcoded "MVP connectivity test" messages.  
    4. **Verify ActivityDataModel.cs**: Confirm that ActivityDataModel has all necessary properties for ActiveWindowTitle, ApplicationProcessName, and ActivityStatus, and that it is correctly configured for JSON serialization.  
  * **References between Files**:  
    * ActivityLogger.cs → IDataAccess.cs (inserts ActivityDataModel)  
    * BatchProcessor.cs → IDataAccess.cs (fetches ActivityDataModel batches)  
    * BatchProcessor.cs → IPipedreamClient.cs (submits ActivityDataModel batches)  
    * PipedreamClient.cs ↔ ActivityDataModel.cs (serializes/deserializes activity data)  
    * OptimizedWindowMonitor.cs → ActivityLogger.cs (provides ActivityDataModel)  
    * OptimizedInputMonitor.cs → ActivityLogger.cs (provides ActivityStatus)  
  * **Acceptance Criteria**:  
    * Each payload sent to Pipedream by the BatchProcessor contains a JSON array of activity records.  
    * Each activity record in the payload includes timestamp, windowsUsername, activeWindowTitle, applicationProcessName, and activityStatus.  
    * The values for activeWindowTitle, applicationProcessName, and activityStatus accurately reflect the user's actual activity at the time of recording.  
    * Application logs confirm that activity data, not just a generic message, is being prepared for submission.  
  * **Testing Plan**:  
    * **Test Case 1**: Pipedream Payload Content Verification  
      * **Test Data**: Application running, user performing various activities (switching windows, typing, idling).  
      * **Expected Result**: Pipedream endpoint receives payloads containing multiple activity records. Each record has correct timestamp, windowsUsername, activeWindowTitle, applicationProcessName, and activityStatus values.  
      * **Testing Tool**: Pipedream dashboard (to inspect received JSON), application logs (to verify data before sending).  
    * **Test Case 2**: Activity Data Accuracy  
      * **Test Data**: Specific sequence of actions (e.g., open Notepad, type, switch to Chrome, idle).  
      * **Expected Result**: Activity records in Pipedream accurately reflect the sequence of window titles, process names, and activity statuses.  
      * **Testing Tool**: Manual observation, Pipedream dashboard, application logs.  
* **User Story 5**: As an end-user, I want the application to always exit cleanly and gracefully from all interaction points (main window, tray icon, session logoff) so that I don't have orphaned processes or data corruption.  
  * **Description**: Regardless of how the user chooses to terminate the application (via the main window's "File \> Quit", the tray icon's "Exit", or a Windows session logoff), the application should ensure all its components are properly shut down, resources are released, and no background processes are left running. This prevents resource leaks and ensures data integrity.  
  * **Actions to Undertake**:  
    1. **Review TimeTrackerApplicationContext.cs Dispose() method**: Ensure all services (ActivityLogger, BatchProcessor, TrayIconManager, SessionMonitor) are stopped and disposed in the correct order (reverse order of startup).  
    2. **Implement Stop() and Dispose() in all services**: Verify that ActivityLogger, BatchProcessor, TrayIconManager, SessionMonitor, OptimizedWindowMonitor, OptimizedInputMonitor, PipedreamClient, SqlServerDataAccess, and BackgroundTaskQueue have robust Stop() and Dispose() implementations that release unmanaged resources (hooks, timers, threads, database connections, HTTP clients).  
    3. **Handle Cancellation Tokens**: Ensure long-running background tasks (e.g., in BackgroundTaskQueue processing, BatchProcessor timer loop) respect CancellationTokens to allow for prompt termination.  
    4. **Error Handling for Shutdown**: Add try-catch blocks around disposal logic to prevent one component's failure from blocking others during shutdown.  
    5. **Final Batch Processing on Exit**: In BatchProcessor.StopAsync, ensure any remaining unsynced records are attempted to be submitted one last time before full shutdown.  
  * **References between Files**:  
    * TimeTrackerApplicationContext.cs ↔ All core services (ActivityLogger, BatchProcessor, TrayIconManager, SessionMonitor).  
    * All services (ActivityLogger, BatchProcessor, OptimizedWindowMonitor, OptimizedInputMonitor, PipedreamClient, SqlServerDataAccess, BackgroundTaskQueue, TrayIconManager, SessionMonitor) ↔ Their respective Dispose() and Stop() methods.  
  * **Acceptance Criteria**:  
    * When the application is exited via "File \> Quit", its process terminates within 2 seconds.  
    * When the application is exited via "Tray Icon \> Exit", its process terminates within 2 seconds.  
    * When the user logs off Windows, the application process terminates cleanly.  
    * No TimeTracker.DesktopApp.exe processes are observed in Task Manager after any exit scenario.  
    * Application logs show "disposed successfully" messages for all core services during shutdown.  
    * No unhandled exceptions occur during application exit.  
  * **Testing Plan**:  
    * **Test Case 1**: Clean Exit from Main Window  
      * **Test Data**: Application running, main window visible.  
      * **Expected Result**: Click "File \> Quit", process terminates quickly.  
      * **Testing Tool**: Manual, Task Manager (Processes tab).  
    * **Test Case 2**: Clean Exit from Tray Icon  
      * **Test Data**: Application running, main window minimized to tray.  
      * **Expected Result**: Right-click tray icon, select "Exit", process terminates quickly.  
      * **Testing Tool**: Manual, Task Manager.  
    * **Test Case 3**: Clean Exit on User Logoff  
      * **Test Data**: Application running (visible or minimized).  
      * **Expected Result**: User logs off Windows, process terminates before logoff completes.  
      * **Testing Tool**: Manual logoff, Task Manager (verify process is gone before logoff).  
    * **Test Case 4**: Resource Release Verification  
      * **Test Data**: Repeated start/stop cycles, or long-running session.  
      * **Expected Result**: Memory and CPU usage return to baseline after exit; no open file handles or network connections remain.  
      * **Testing Tool**: Task Manager (Performance tab), Process Explorer (for handles/DLLs).  
* **User Story 6**: As an administrator, I want a basic settings user interface to configure the Pipedream endpoint URL so that I can easily point the application to different Pipedream instances without manual file editing.  
  * **Description**: The application should provide a user-friendly settings dialog accessible from the tray icon. This dialog will allow the administrator to view and modify the Pipedream endpoint URL. It should also include a "Test Connection" button to verify the entered URL's validity and reachability. The changes should be persisted and loaded correctly on subsequent application launches.  
  * **Actions to Undertake**:  
    1. **Update SettingsForm.cs**:  
       * Ensure the PipedreamEndpointUrl text box is present and correctly bound to ConfigurationManager.  
       * Implement the "Test Connection" button's click handler, which uses PipedreamClient.TestConnectionAsync() to validate the URL and display status.  
       * Implement "Save" and "Cancel" buttons to persist or discard changes via ConfigurationManager.SaveConfigurationAsync().  
       * Ensure LoadCurrentSettings() in SettingsForm correctly populates the UI from ConfigurationManager.LoadConfiguration().  
    2. **Update ConfigurationManager.cs**:  
       * Ensure LoadConfiguration() and SaveConfigurationAsync() handle the PipedreamEndpointUrl property.  
       * Implement logic to read from appsettings.json (default) and user-settings.json (user override).  
    3. **Update PipedreamClient.cs**:  
       * Ensure TestConnectionAsync() is robust and provides meaningful feedback.  
       * Ensure PipedreamClient uses the URL provided by ConfigurationManager.  
    4. **Integrate SettingsForm with TrayIconManager**: Ensure the "Settings" menu item in TrayIconManager correctly opens an instance of SettingsForm.  
  * **References between Files**:  
    * SettingsForm.cs ↔ ConfigurationManager.cs (loads/saves settings)  
    * SettingsForm.cs ↔ PipedreamClient.cs (tests connection)  
    * TrayIconManager.cs → SettingsForm.cs (opens settings dialog)  
    * ConfigurationManager.cs ← appsettings.json (default endpoint)  
    * ConfigurationManager.cs ↔ %APPDATA%\\TimeTracker\\user-settings.json (user-specific endpoint)  
  * **Acceptance Criteria**:  
    * A "Settings" option is available in the tray icon's context menu.  
    * Clicking "Settings" opens a modal dialog with an input field for "Pipedream Endpoint URL".  
    * The URL field is pre-populated with the currently configured endpoint.  
    * A "Test Connection" button is present and, when clicked, attempts to connect to the entered URL and displays a success/failure message.  
    * Changes to the URL are saved upon clicking "Save" and persist across application restarts.  
    * Clicking "Cancel" discards changes.  
    * Invalid URLs (e.g., malformed HTTP) are validated client-side before attempting connection.  
  * **Testing Plan**:  
    * **Test Case 1**: Open and View Settings  
      * **Test Data**: Application running.  
      * **Expected Result**: "Settings" menu item opens dialog, current Pipedream URL is displayed.  
      * **Testing Tool**: Manual observation.  
    * **Test Case 2**: Test Pipedream Connection (Success)  
      * **Test Data**: Valid, reachable Pipedream URL entered.  
      * **Expected Result**: Click "Test Connection", status changes to "Success".  
      * **Testing Tool**: Manual, Pipedream dashboard (to ensure test payload is received).  
    * **Test Case 3**: Test Pipedream Connection (Failure)  
      * **Test Data**: Invalid or unreachable Pipedream URL entered (e.g., http://localhost:12345).  
      * **Expected Result**: Click "Test Connection", status changes to "Failed" or "Error".  
      * **Testing Tool**: Manual.  
    * **Test Case 4**: Save and Persist Settings  
      * **Test Data**: Change Pipedream URL, click "Save".  
      * **Expected Result**: Restart application, new URL is loaded and used for heartbeats.  
      * **Testing Tool**: Manual, Pipedream dashboard, application logs.  
    * **Test Case 5**: Cancel Settings Changes  
      * **Test Data**: Change Pipedream URL, click "Cancel".  
      * **Expected Result**: Restart application, original URL is loaded and used.  
      * **Testing Tool**: Manual.

## **Actions to Undertake (Consolidated)**

1. **Refactor Program.cs and TimeTrackerApplicationContext.cs**:  
   * Ensure proper dependency injection setup for all components.  
   * Handle graceful shutdown of all services in TimeTrackerApplicationContext.Dispose().  
2. **Develop MainForm.cs**:  
   * Implement "File \> Quit" menu option.  
   * Implement FormClosing event to minimize to tray instead of closing.  
   * Add logic to restore the window from minimized state.  
3. **Create TrayIconManager.cs**:  
   * Implement System.Windows.Forms.NotifyIcon.  
   * Load app.ico for the tray icon.  
   * Set up ContextMenuStrip with "Start Tracking", "Pause Tracking", "Stop Tracking", "Settings", "View Status", and "Exit" options.  
   * Handle MouseClick events for left-click (show status overlay) and right-click (show context menu).  
   * Implement logic to update icon tooltip and appearance based on tracking status.  
4. **Develop StatusOverlayForm.cs**:  
   * Create a borderless, TopMost form for quick status display.  
   * Add labels to show active window, last input time, Pipedream status, and pending submissions.  
   * Implement a timer for periodic UI updates.  
   * Implement auto-hide logic (e.g., on Deactivate or MouseLeave).  
5. **Develop SessionMonitor.cs**:  
   * Create a hidden Form to receive WM\_WTSSESSION\_CHANGE messages.  
   * Use WTSRegisterSessionNotification and WTSUnRegisterSessionNotification.  
   * Map WTS\_SESSION\_LOCK, WTS\_SESSION\_UNLOCK, WTS\_SESSION\_LOGOFF, WTS\_REMOTE\_CONNECT, WTS\_REMOTE\_DISCONNECT to SessionState enum.  
   * Notify ActivityLogger to pause/resume/stop tracking.  
6. **Update ActivityLogger.cs**:  
   * Add PauseTracking() and ResumeTracking() methods.  
   * Modify StartAsync() and Stop() to be callable multiple times and handle internal state (\_isStarted, \_isPaused).  
   * Ensure LogActivityAsync correctly captures and stores ActiveWindowTitle, ApplicationProcessName, and ActivityStatus from monitors.  
   * Update GetStatusInfo() to include \_backgroundTaskQueue.Count for pending submissions.  
   * Ensure all resources are released during disposal.  
7. **Update ConfigurationManager.cs**:  
   * Add AutoStartWithWindows property to UserConfiguration.  
   * Implement logic to read/write to HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run for auto-start.  
   * Ensure user settings (including PipedreamEndpointUrl) are saved to %APPDATA%\\TimeTracker\\user-settings.json.  
8. **Update SettingsForm.cs**:  
   * Add a checkbox for "Start with Windows".  
   * Add an input field for "Pipedream Endpoint URL".  
   * Add a "Test Connection" button that calls PipedreamClient.TestConnectionAsync().  
   * Bind UI controls to ConfigurationManager for loading and saving settings.  
9. **Update PipedreamClient.cs**:  
   * Ensure SubmitBatchDataAsync serializes a list of full ActivityDataModel objects (not just a generic message).  
   * Implement TestConnectionAsync method for validating the endpoint.  
   * Ensure PipedreamClient uses the URL provided by ConfigurationManager.  
   * Ensure all resources are released during disposal.  
10. **Update BatchProcessor.cs**:  
    * Ensure it fetches actual ActivityDataModel batches from IDataAccess.  
    * Call \_pipedreamClient.SubmitBatchDataAsync with the fetched records.  
    * Handle marking records as synced and deleting them from local storage after successful submission.  
    * Ensure StopAsync attempts a final batch submission and gracefully shuts down.  
    * Ensure all resources are released during disposal.  
11. **Update IDataAccess.cs and SqlServerDataAccess.cs**:  
    * Ensure GetUnsyncedActivitiesAsync, MarkActivitiesAsSyncedAsync, and DeleteSyncedRecordsByBatchIdAsync are correctly implemented to support batch processing of full ActivityDataModel objects.  
    * Ensure SqlServerDataAccess disposes its timer and semaphore correctly.  
12. **Update ActivityDataModel.cs**:  
    * Ensure IsSynced and BatchId properties are present for tracking submission status and properly handled during serialization/deserialization.  
13. **Create app.ico**: Ensure a suitable icon file is available and embedded/copied to the output directory.

## **References between Files (Detailed)**

* Program.cs  
  * **Uses**: TimeTrackerApplicationContext, ActivityLogger, TrayIconManager, SessionMonitor, BatchProcessor, ConfigurationManager, IDataAccess, IPipedreamClient, IWindowMonitor, IInputMonitor, BackgroundTaskQueue, FileLoggerProvider.  
  * **Relationship**: Main entry point, sets up DI container, manages overall application lifecycle.  
* TimeTrackerApplicationContext.cs  
  * **Uses**: ActivityLogger, TrayIconManager, SessionMonitor, BatchProcessor.  
  * **Relationship**: Manages the lifetime of the desktop application, starts and stops core services, acts as the Application.Run context.  
* MainForm.cs (or equivalent main window)  
  * **Uses**: Application.Exit(), potentially TrayIconManager (to hide/show itself).  
  * **Relationship**: Primary user interface, handles window close events to minimize to tray, provides "File \> Quit" functionality.  
* TrayIconManager.cs  
  * **Uses**: System.Windows.Forms.NotifyIcon, ContextMenuStrip, ToolStripMenuItem, System.Windows.Forms.MessageBox, ActivityLogger, SettingsForm, StatusOverlayForm, System.Drawing.Icon, System.Windows.Forms.SystemIcons.  
  * **Relationship**: Manages tray icon, its menu, and dispatches user actions to ActivityLogger, opens SettingsForm and StatusOverlayForm.  
* StatusOverlayForm.cs  
  * **Uses**: ActivityLogger, System.Windows.Forms.Timer, Screen.  
  * **Relationship**: Displays real-time status information obtained from ActivityLogger.  
* SessionMonitor.cs  
  * **Uses**: NativeMethods.cs (WTSRegisterSessionNotification, WTSUnRegisterSessionNotification, WM\_WTSSESSION\_CHANGE), ActivityLogger.  
  * **Relationship**: Monitors Windows session changes and instructs ActivityLogger to pause/resume/stop tracking.  
* ActivityLogger.cs  
  * **Uses**: IDataAccess, IPipedreamClient, IWindowMonitor, IInputMonitor, BackgroundTaskQueue, IConfiguration.  
  * **Relationship**: Central orchestrator, subscribes to window and input monitor events, logs data to IDataAccess, queues submissions to BackgroundTaskQueue for IPipedreamClient. Manages tracking state (active/paused).  
* ConfigurationManager.cs  
  * **Uses**: System.Text.Json, Microsoft.Extensions.Logging, Environment.GetFolderPath, Path.Combine, Microsoft.Win32.Registry (for auto-start).  
  * **Relationship**: Loads and saves application settings from appsettings.json and user-specific user-settings.json. Manages auto-start registry entry.  
* SettingsForm.cs  
  * **Uses**: ConfigurationManager, System.Windows.Forms.MessageBox, System.Net.Http.HttpClient (for test connection).  
  * **Relationship**: Provides UI for users to configure application settings, interacts with ConfigurationManager to persist changes.  
* PipedreamClient.cs  
  * **Uses**: System.Net.Http.HttpClient, System.Text.Json, IConfiguration.  
  * **Relationship**: Handles HTTP POST requests to the Pipedream endpoint, including JSON serialization and retry logic.  
* BatchProcessor.cs  
  * **Uses**: IDataAccess, IPipedreamClient, IConfiguration, System.Threading.Timer.  
  * **Relationship**: Periodically fetches unsynced records from IDataAccess, marks them, and submits them in batches to IPipedreamClient.  
* IDataAccess.cs  
  * **Implemented by**: SqlServerDataAccess.cs.  
  * **Relationship**: Defines the contract for data storage and retrieval, including operations for unsynced records and batch management.  
* SqlServerDataAccess.cs  
  * **Uses**: Microsoft.Data.SqlClient, System.Collections.Concurrent.ConcurrentQueue, System.Threading.Timer, System.Threading.SemaphoreSlim, System.Data.DataTable.  
  * **Relationship**: Concrete implementation of IDataAccess for SQL Server, handles batch inserts and management of synced/unsynced records.  
* IWindowMonitor.cs, IInputMonitor.cs  
  * **Implemented by**: OptimizedWindowMonitor.cs, OptimizedInputMonitor.cs.  
  * **Relationship**: Define contracts for monitoring window changes and input activity.  
* OptimizedWindowMonitor.cs  
  * **Uses**: NativeMethods.cs (SetWinEventHook, UnhookWinEvent, EVENT\_SYSTEM\_FOREGROUND, GetForegroundWindow, GetWindowTitle, GetProcessName, GetWindowProcessId), ActivityDataModel.  
  * **Relationship**: Event-driven window monitoring, reports changes to ActivityLogger.  
* OptimizedInputMonitor.cs  
  * **Uses**: NativeMethods.cs (RegisterRawInputDevices, GetLastInputInfo, WM\_INPUT), InputEvent, BackgroundTaskQueue.  
  * **Relationship**: Monitors keyboard/mouse input using Raw Input API (with GetLastInputInfo fallback), reports activity status to ActivityLogger.  
* BackgroundTaskQueue.cs  
  * **Uses**: System.Collections.Concurrent.BlockingCollection.  
  * **Relationship**: Generic producer-consumer queue for offloading background work items, used by ActivityLogger for Pipedream submissions.  
* NativeMethods.cs  
  * **Relationship**: Contains P/Invoke declarations for all necessary Windows API functions (user32.dll, wtsapi32.dll, kernel32.dll).  
* ActivityDataModel.cs  
  * **Relationship**: Data transfer object used across ActivityLogger, IDataAccess, IPipedreamClient, OptimizedWindowMonitor.  
* app.ico  
  * **Relationship**: Used by TrayIconManager.  
* appsettings.json  
  * **Relationship**: Read by ConfigurationManager.  
* user-settings.json (New)  
  * **Purpose**: Stores user-specific configuration settings that override appsettings.json.  
  * **Contents**: JSON file containing AutoStartWithWindows, PipedreamEndpointUrl, ActivityTimeoutMs, BatchIntervalMs, etc.  
  * **Relationships**: Written to and read from by ConfigurationManager.

## **List of Files being Created**

* **File 1**: Program.cs (Modified)  
  * **Purpose**: Application entry point and overall setup of the desktop application, including dependency injection and application lifecycle management.  
  * **Contents**: Main method, DI container configuration, logging setup, TimeTrackerApplicationContext instantiation and execution. Removal of Windows Service hosting.  
  * **Relationships**: Depends on almost all other core application files for service registration and lifecycle.  
* **File 2**: MainForm.cs (New/Modified)  
  * **Purpose**: Defines the main user interface window of the application.  
  * **Contents**: Windows Forms Form class, UI controls (e.g., menu strip), event handlers for window close (FormClosing to minimize), and "File \> Quit" menu item.  
  * **Relationships**: Interacts with TrayIconManager to hide/show and Application.Exit() for quitting.  
* **File 3**: TrayIconManager.cs (New/Modified)  
  * **Purpose**: Manages the system tray icon, its context menu, and user interactions with it.  
  * **Contents**: NotifyIcon instance, ContextMenuStrip definition, event handlers for menu items and icon clicks, logic to update icon appearance based on tracking status.  
  * **Relationships**: Uses ActivityLogger for tracking control, SettingsForm and StatusOverlayForm for UI display, and app.ico for the icon.  
* **File 4**: StatusOverlayForm.cs (New)  
  * **Purpose**: Provides a small, temporary overlay window to display real-time application status.  
  * **Contents**: A Form with labels for status information, timers for refresh and auto-hide, and logic to position itself near the tray.  
  * **Relationships**: Obtains status information from ActivityLogger.  
* **File 5**: SessionMonitor.cs (New/Modified)  
  * **Purpose**: Monitors Windows user session changes (lock, unlock, logoff) to control activity tracking.  
  * **Contents**: A hidden Form to receive WM\_WTSSESSION\_CHANGE messages, WTSRegisterSessionNotification calls, and logic to map session events to ActivityLogger actions.  
  * **Relationships**: Depends on NativeMethods.cs for Windows API calls and interacts with ActivityLogger.  
* **File 6**: ActivityLogger.cs (Modified)  
  * **Purpose**: Central orchestrator for activity logging, coordinating data collection, storage, and submission.  
  * **Contents**: Methods for StartAsync, Stop, PauseTracking, ResumeTracking, event handlers for WindowMonitor and InputMonitor, logic to enqueue data for BackgroundTaskQueue. Enhanced to ensure full ActivityDataModel is logged.  
  * **Relationships**: Depends on IDataAccess, IPipedreamClient, IWindowMonitor, IInputMonitor, BackgroundTaskQueue.  
* **File 7**: ConfigurationManager.cs (Modified)  
  * **Purpose**: Manages application configuration settings, including auto-start and user preferences.  
  * **Contents**: Methods to load/save settings from appsettings.json and user-specific JSON files, and to manage the Windows Registry entry for auto-start.  
  * **Relationships**: Used by SettingsForm for UI interaction and by other services to retrieve configuration.  
* **File 8**: SettingsForm.cs (Modified)  
  * **Purpose**: Provides a user interface for configuring application settings.  
  * **Contents**: Windows Forms Form with various input controls (textboxes, checkboxes, numeric up/down), event handlers for saving, canceling, and testing Pipedream connection. Now includes explicit Pipedream URL input and test connection button.  
  * **Relationships**: Interacts with ConfigurationManager to persist settings and PipedreamClient for connection testing.  
* **File 9**: PipedreamClient.cs (Modified)  
  * **Purpose**: Handles communication with the Pipedream endpoint.  
  * **Contents**: HttpClient for HTTP requests, JSON serialization logic, retry mechanism, and methods for SubmitActivityDataAsync, SubmitBatchDataAsync, and TestConnectionAsync. Now specifically handles serialization of full ActivityDataModel objects.  
  * **Relationships**: Used by BatchProcessor and SettingsForm.  
* **File 10**: BatchProcessor.cs (Modified)  
  * **Purpose**: Manages the periodic batching and submission of activity data to Pipedream.  
  * **Contents**: System.Threading.Timer for scheduling, logic to fetch unsynced records from IDataAccess, mark them as synced, and submit them via IPipedreamClient. Now fetches and processes full ActivityDataModel batches.  
  * **Relationships**: Depends on IDataAccess and IPipedreamClient.  
* **File 11**: IDataAccess.cs (Existing/Modified)  
  * **Purpose**: Interface defining data access operations.  
  * **Contents**: Method signatures for InsertActivityAsync, GetRecentActivitiesAsync, GetActivityCountAsync, GetUnsyncedActivitiesAsync, MarkActivitiesAsSyncedAsync, DeleteSyncedRecordsByBatchIdAsync.  
  * **Relationships**: Implemented by SqlServerDataAccess.cs, used by ActivityLogger and BatchProcessor.  
* **File 12**: SqlServerDataAccess.cs (Existing/Modified)  
  * **Purpose**: Concrete implementation of IDataAccess using SQL Server.  
  * **Contents**: Logic for database initialization, enqueuing activities for batch inserts, SqlBulkCopy for performance, and methods to manage synced/unsynced records.  
  * **Relationships**: Implements IDataAccess.  
* **File 13**: IWindowMonitor.cs, IInputMonitor.cs (Existing)  
  * **Purpose**: Interfaces for window and input monitoring.  
  * **Contents**: Method signatures for starting, stopping, and getting current activity/status.  
  * **Relationships**: Implemented by OptimizedWindowMonitor.cs and OptimizedInputMonitor.cs, used by ActivityLogger.  
* **File 14**: OptimizedWindowMonitor.cs (Existing)  
  * **Purpose**: Monitors active window changes using Windows API hooks.  
  * **Contents**: SetWinEventHook implementation, WinEventCallback to process events, logic to capture window title and process name.  
  * **Relationships**: Implements IWindowMonitor, depends on NativeMethods.cs.  
* **File 15**: OptimizedInputMonitor.cs (Existing)  
  * **Purpose**: Monitors keyboard and mouse input using Raw Input API or GetLastInputInfo.  
  * **Contents**: Raw Input device registration, fallback polling logic, BackgroundTaskQueue for processing, debouncing logic.  
  * **Relationships**: Implements IInputMonitor, depends on NativeMethods.cs and BackgroundTaskQueue.  
* **File 16**: BackgroundTaskQueue.cs (Existing)  
  * **Purpose**: A generic producer-consumer queue for asynchronous background work.  
  * **Contents**: BlockingCollection to manage work items, methods to enqueue and dequeue tasks.  
  * **Relationships**: Used by ActivityLogger and OptimizedInputMonitor.  
* **File 17**: NativeMethods.cs (Existing)  
  * **Purpose**: Provides P/Invoke declarations for Windows API functions.  
  * **Contents**: DllImport attributes for various user32.dll, wtsapi32.dll, kernel32.dll functions and related structures/constants.  
  * **Relationships**: Depended upon by OptimizedWindowMonitor, OptimizedInputMonitor, SessionMonitor.  
* **File 18**: ActivityDataModel.cs (Existing/Modified)  
  * **Purpose**: Data transfer object for captured activity records.  
  * **Contents**: Properties for Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus, ActiveWindowHandle. IsSynced and BatchId properties are confirmed and handled.  
  * **Relationships**: Used across ActivityLogger, IDataAccess, IPipedreamClient, OptimizedWindowMonitor.  
* **File 19**: app.ico (New)  
  * **Purpose**: Application icon displayed in the system tray and potentially the main window.  
  * **Contents**: A standard .ico file.  
  * **Relationships**: Used by TrayIconManager.  
* **File 20**: appsettings.json (Modified)  
  * **Purpose**: Stores application configuration settings.  
  * **Contents**: JSON structure including TimeTracker:PipedreamEndpointUrl, TimeTracker:ActivityTimeoutMs, TimeTracker:BatchIntervalMs, TimeTracker:MaxConcurrentSubmissions, TimeTracker:RetryAttempts, etc.  
  * **Relationships**: Read by ConfigurationManager.  
* **File 21**: user-settings.json (New)  
  * **Purpose**: Stores user-specific configuration settings that override appsettings.json defaults.  
  * **Contents**: A JSON file created and managed by ConfigurationManager in the user's %APPDATA%\\TimeTracker directory. Contains PipedreamEndpointUrl, AutoStartWithWindows, etc.  
  * **Relationships**: Written to and read from by ConfigurationManager.

## **Acceptance Criteria (Overall Phase 2\)**

* The application's periodic Pipedream submissions include comprehensive activity data (window title, process name, activity status) for each record.  
* The application consistently exits cleanly and gracefully from all user-initiated and system-triggered termination points (main window, tray icon, session logoff).  
* A functional "Settings" dialog is accessible from the tray icon, allowing administrators to view, modify, and test the Pipedream endpoint URL.  
* Changes made in the settings dialog are correctly persisted and loaded on subsequent application launches.  
* The application maintains low CPU and memory usage when idle or performing background tasks.  
* All components and services are properly disposed of during application shutdown, preventing resource leaks.

## **Testing Plan (Overall Phase 2\)**

* **Manual Testing**:  
  * **Pipedream Data Content**:  
    * Run the application, perform various actions (open different apps, type, idle).  
    * Monitor the configured Pipedream endpoint to confirm that received payloads contain accurate activeWindowTitle, applicationProcessName, and activityStatus for each activity record.  
    * Verify that timestamp and windowsUsername are also correctly populated.  
  * **Graceful Exit Scenarios**:  
    * Execute "File \> Quit" from the main window and verify immediate process termination via Task Manager.  
    * Minimize to tray, then execute "Exit" from the tray icon context menu and verify immediate process termination.  
    * Log off Windows while the application is running and verify that the process is no longer active after logoff.  
    * Perform a system restart and verify clean shutdown and startup.  
  * **Settings UI Functionality**:  
    * Access the "Settings" dialog from the tray icon.  
    * Verify the current Pipedream Endpoint URL is displayed correctly.  
    * Enter a valid Pipedream URL, click "Test Connection", and confirm "Success" status. Verify a test payload is received at the endpoint.  
    * Enter an invalid URL (e.g., http://invalid-url), click "Test Connection", and confirm "Failed" or "Error" status.  
    * Change the Pipedream URL, click "Save", then restart the application and verify the new URL is active for heartbeats.  
    * Change the Pipedream URL, click "Cancel", then restart the application and verify the original URL is still active.  
* **Log Review**:  
  * Examine TimeTracker.log for:  
    * Confirmation of ActivityDataModel serialization and submission (e.g., "Submitting batch data to Pipedream: X records").  
    * Messages indicating successful disposal of all services during shutdown (e.g., "TrayIconManager disposed successfully", "ActivityLogger disposed successfully").  
    * Detailed logs for Pipedream connection tests (success/failure).  
    * Any unexpected errors or warnings during runtime or shutdown.  
* **Process Verification**:  
  * Use Windows Task Manager or Process Explorer to continuously monitor TimeTracker.DesktopApp.exe's CPU and memory usage to ensure it adheres to the non-functional requirements.  
  * Verify no orphaned processes, open file handles, or network connections remain after application termination.  
* **Automated Testing (Refinement)**:  
  * Develop unit tests for ActivityLogger to ensure correct data enrichment and queuing.  
  * Enhance integration tests for BatchProcessor to verify it correctly fetches, marks, and submits full ActivityDataModel batches.  
  * Add unit/integration tests for SettingsForm and ConfigurationManager to validate saving, loading, and auto-start registry interactions.  
  * Create end-to-end automated tests for application startup and clean shutdown using UI automation tools (e.g., TestStack.White, WinAppDriver).

## **Assumptions and Dependencies**

* **Operating System**: Windows 10 or newer.  
* **.NET Runtime**: .NET 8.0 Desktop Runtime is installed on the target machine.  
* **User Permissions**: The application runs under a user account with sufficient permissions to:  
  * Create and write to %APPDATA%\\TimeTracker and %CommonApplicationData%\\TimeTracker\\Logs.  
  * Create/modify registry entries in HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run.  
  * Install and receive Windows API events (SetWinEventHook, Raw Input, WTSRegisterSessionNotification).  
* **Network Connectivity**: Assumed to be generally available for Pipedream submissions.  
* **Pipedream Endpoint**: A valid and accessible Pipedream endpoint URL is provided in appsettings.json or configured by the user via the settings UI.  
* **SQL Server LocalDB**: Assumed to be available for SqlServerDataAccess to connect to (or a full SQL Server instance if configured).  
* **Existing Components**: All components from Phase 1 are stable and correctly integrated.

## **Non-Functional Requirements**

1. **Performance**:  
   * **CPU Usage**: The application should consume less than 2% CPU when idle and minimized to the tray, even with activity data collection.  
   * **Memory Usage**: Memory footprint should remain minimal, ideally under 50MB when idle, and not exhibit significant growth over long periods of operation.  
2. **Security**:  
   * Pipedream endpoint URL, while configurable via UI, will still be stored in plain text in user-settings.json for this phase. Encryption will be considered in future phases.  
3. **Usability**:  
   * **Settings Form**: The settings form should be intuitive, responsive, and provide clear feedback on connection tests.  
   * **Status Overlay**: Continues to be concise, readable, and non-intrusive.  
4. **Stability**:  
   * The application must run continuously without crashing or freezing during typical usage, including network interruptions and session state changes.  
   * All exit paths must be robust and prevent orphaned processes or data corruption.  
5. **Compatibility**:  
   * Windows 10+ only.  
6. **Install Size**:  
   * The deployed application size should remain under 15MB, considering the added UI for settings.

## **Conclusion**

Phase 2 marks a significant step towards a fully functional TimeTracker desktop application. By integrating real activity data into the Pipedream heartbeat, ensuring robust application exits, and providing a user-configurable settings interface, we are building a more valuable and manageable tool. This phase prioritizes stability and user control, laying the groundwork for more advanced features in subsequent iterations.