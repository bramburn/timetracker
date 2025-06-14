# **Desktop App Transition Backlog: Phase 1**

## **Introduction**

This backlog details the first phase of transitioning the TimeTracker application from a Windows Service to a user-interactive desktop application with a system tray icon. This phase focuses on establishing the core UI, tray icon functionality, and ensuring proper application exit paths. It also includes the initial implementation of a basic Pipedream heartbeat to validate background data transmission. This phase addresses the foundational requirements outlined in the initial PRD for the MVP.

## **User Stories**

* **User Story 1**: As an end-user, I want a basic desktop application UI with a system tray icon so that I can easily launch, minimize, and quit the application.  
  * **Description**: The application should launch with a main window that can be minimized to the Windows system tray. An icon representing the application should be visible in the system tray. The application should provide a clear way to exit completely, both from the main window (e.g., via a "File \> Quit" menu option) and from the system tray icon's context menu.  
  * **Actions to Undertake**:  
    1. **Refactor Program.cs**: Modify the entry point to initialize and run a Form or ApplicationContext for a desktop application instead of a WindowsService.  
    2. **Create MainForm.cs**: Develop a basic Windows Forms (or WPF) main window. This window should have a title and a simple structure.  
    3. **Implement MainForm Close Behavior**: Configure the FormClosing event handler in MainForm.cs to minimize the window to the system tray instead of closing it when the 'X' button is clicked.  
    4. **Create TrayIconManager.cs**: Implement a class to manage the System.Windows.Forms.NotifyIcon component. This includes setting the icon, tooltip text, and handling its lifecycle.  
    5. **Load Application Icon**: Ensure a suitable .ico file (app.ico) is available and loaded by the TrayIconManager for the system tray icon.  
    6. **Design Tray Icon Context Menu**: Create a ContextMenuStrip for the tray icon with at least an "Exit" option.  
    7. **Implement Tray Icon Exit Logic**: Add an event handler to the "Exit" menu item in the tray icon's context menu to cleanly terminate the application.  
    8. **Implement Main Window "Quit" Option**: Add a "File \> Quit" menu item to MainForm.cs that also cleanly terminates the application.  
    9. **Integrate TrayIconManager**: Instantiate and manage the TrayIconManager within the application's main lifecycle (e.g., in Program.cs or a custom ApplicationContext).  
  * **References between Files**:  
    * Program.cs → MainForm.cs (instantiation)  
    * Program.cs → TrayIconManager.cs (instantiation and lifecycle)  
    * MainForm.cs ↔ TrayIconManager.cs (minimize/restore functionality)  
    * TrayIconManager.cs ← app.ico (icon asset)  
  * **Acceptance Criteria**:  
    * The TimeTracker application launches and displays a main window.  
    * Clicking the 'X' button on the main window minimizes it to the system tray.  
    * A distinct TimeTracker icon is visible in the Windows system tray.  
    * Right-clicking the tray icon displays a context menu with an "Exit" option.  
    * Selecting "Exit" from the tray icon's context menu cleanly terminates the application process, and the tray icon disappears.  
    * The main window has a "File" menu with a "Quit" option.  
    * Selecting "Quit" from the "File" menu cleanly terminates the application.  
  * **Testing Plan**:  
    * **Test Case 1**: Main Window Launch and Minimize  
      * **Test Data**: Clean application launch.  
      * **Expected Result**: Main window appears. Clicking 'X' minimizes it to the tray, and the tray icon appears.  
      * **Testing Tool**: Manual observation.  
    * **Test Case 2**: Exit from Tray Icon  
      * **Test Data**: Application minimized to tray.  
      * **Expected Result**: Right-click tray icon, select "Exit". Application process terminates, tray icon disappears.  
      * **Testing Tool**: Manual observation, Windows Task Manager (to verify process termination).  
    * **Test Case 3**: Exit from Main Window Menu  
      * **Test Data**: Main window visible.  
      * **Expected Result**: Navigate to "File \> Quit". Application process terminates.  
      * **Testing Tool**: Manual observation, Windows Task Manager.  
* **User Story 2**: As a system, I need to send a basic heartbeat payload to Pipedream periodically so that I can validate core network connectivity and background task execution.  
  * **Description**: The application must be able to perform a simple background task: sending a predefined, static message (e.g., "MVP connectivity test") to a configurable Pipedream endpoint at regular intervals (e.g., every 5 minutes). This task should run independently of the main UI's visibility (whether the window is open, minimized, or hidden).  
  * **Actions to Undertake**:  
    1. **Update PipedreamService.cs (rename to PipedreamClient.cs)**: Modify the existing service to be a client that can send a simple HTTP POST request with a JSON payload.  
    2. **Implement BatchProcessor.cs**: Create a new class responsible for scheduling and executing the periodic heartbeat. This class will use a System.Threading.Timer or similar mechanism.  
    3. **Define Heartbeat Payload**: For this phase, a simple JSON object like {"message": "TimeTracker MVP connectivity test"} will suffice.  
    4. **Configure Pipedream Endpoint URL**: Read the Pipedream endpoint URL from appsettings.json.  
    5. **Integrate BatchProcessor**: Instantiate and start the BatchProcessor during application startup (e.g., in Program.cs or TimeTrackerApplicationContext). Ensure it's properly stopped during application shutdown.  
    6. **Basic Logging**: Add logging statements to PipedreamClient.cs and BatchProcessor.cs to indicate when a heartbeat is sent and its success/failure status.  
  * **References between Files**:  
    * Program.cs → BatchProcessor.cs (instantiation and lifecycle)  
    * BatchProcessor.cs → PipedreamClient.cs (sends heartbeat)  
    * PipedreamClient.cs ← appsettings.json (reads endpoint URL)  
  * **Acceptance Criteria**:  
    * The application successfully sends a JSON payload containing {"message": "TimeTracker MVP connectivity test"} to the configured Pipedream endpoint.  
    * These payloads are sent periodically (e.g., every 5 minutes).  
    * The heartbeats are sent regardless of whether the main application window is visible, minimized, or hidden.  
    * Application logs show entries confirming the sending of the heartbeat and its outcome (success/failure).  
  * **Testing Plan**:  
    * **Test Case 1**: Periodic Heartbeat Transmission  
      * **Test Data**: A valid Pipedream endpoint URL configured in appsettings.json.  
      * **Expected Result**: Observe incoming requests on the Pipedream dashboard at regular intervals (e.g., every 5 minutes) with the specified JSON payload.  
      * **Testing Tool**: Pipedream dashboard, application logs.  
    * **Test Case 2**: Heartbeat Independence from UI  
      * **Test Data**: Run the application, then minimize the main window to the tray.  
      * **Expected Result**: Heartbeats continue to be sent to Pipedream at regular intervals, as confirmed by the Pipedream dashboard and application logs.  
      * **Testing Tool**: Manual observation, Pipedream dashboard, application logs.

## **Actions to Undertake (Consolidated)**

1. **Refactor Program.cs**:  
   * Change the application type from WinExe (if it was a service) to Exe for console output during development, or ensure it's configured as a desktop application.  
   * Remove AddWindowsService and any service-specific hosting configurations.  
   * Set up a standard desktop application entry point, likely using Application.Run(new TimeTrackerApplicationContext()) or directly running a MainForm.  
   * Configure dependency injection for PipedreamClient, BatchProcessor, TrayIconManager, and any other core components.  
2. **Create TimeTrackerApplicationContext.cs (Optional but Recommended)**:  
   * A custom ApplicationContext to manage the lifecycle of the main form and background services. This provides a central point for starting and stopping services.  
3. **Develop MainForm.cs**:  
   * Create a basic Windows Forms Form with a simple title.  
   * Add a MenuStrip with a "File" menu containing a "Quit" option.  
   * Implement the FormClosing event to hide the form and show the tray icon instead of exiting the application.  
4. **Create TrayIconManager.cs**:  
   * Implement System.Windows.Forms.NotifyIcon.  
   * Load app.ico as the icon.  
   * Create a ContextMenuStrip with a "Exit" ToolStripMenuItem.  
   * Handle the "Exit" menu item's Click event to call Application.Exit().  
   * Handle the MainForm's FormClosing event to make the NotifyIcon visible.  
5. **Update PipedreamClient.cs (formerly PipedreamService.cs)**:  
   * Rename the file and class if necessary.  
   * Ensure it has a method (e.g., SendHeartbeatAsync) that takes a simple string message or a predefined JSON object and sends it to the configured Pipedream endpoint.  
   * Add basic HttpClient usage for HTTP POST requests.  
6. **Create BatchProcessor.cs**:  
   * Implement IHostedService (if using generic host builder) or a simple class with Start() and Stop() methods.  
   * Use a System.Threading.Timer to trigger a periodic task.  
   * Inside the timer's callback, call PipedreamClient.SendHeartbeatAsync.  
7. **Configure appsettings.json**:  
   * Add a key for PipedreamEndpointUrl.  
8. **Create app.ico**:  
   * A simple icon file for the application's tray icon.  
9. **Implement Basic Logging**:  
   * Ensure a simple console or file logger is configured to output messages from PipedreamClient and BatchProcessor.

## **References between Files (Detailed)**

* Program.cs  
  * **Uses**: TimeTrackerApplicationContext (or MainForm), PipedreamClient, BatchProcessor, TrayIconManager, Microsoft.Extensions.Hosting (for DI setup).  
  * **Relationship**: Main entry point, sets up the application's host, configures services, and starts the desktop application lifecycle.  
* TimeTrackerApplicationContext.cs (if created)  
  * **Uses**: MainForm, TrayIconManager, BatchProcessor.  
  * **Relationship**: Manages the overall lifecycle of the desktop application, including showing the main form, managing the tray icon, and starting/stopping background services.  
* MainForm.cs  
  * **Uses**: Application.Exit().  
  * **Relationship**: The primary UI window. Its FormClosing event will interact with TrayIconManager to hide the window and show the tray icon.  
* TrayIconManager.cs  
  * **Uses**: System.Windows.Forms.NotifyIcon, System.Windows.Forms.ContextMenuStrip, System.Windows.Forms.ToolStripMenuItem, System.Drawing.Icon, Application.Exit().  
  * **Relationship**: Manages the system tray icon, its context menu, and handles the application exit command from the tray.  
* PipedreamClient.cs  
  * **Uses**: System.Net.Http.HttpClient, Microsoft.Extensions.Configuration (to read PipedreamEndpointUrl).  
  * **Relationship**: Responsible for all HTTP communication with the Pipedream endpoint. Used by BatchProcessor.  
* BatchProcessor.cs  
  * **Uses**: System.Threading.Timer, PipedreamClient, Microsoft.Extensions.Configuration (for interval).  
  * **Relationship**: Schedules and executes the periodic heartbeat task, relying on PipedreamClient to send the data.  
* appsettings.json  
  * **Relationship**: Provides configuration values, specifically the PipedreamEndpointUrl for PipedreamClient and potentially the BatchInterval for BatchProcessor.  
* app.ico  
  * **Relationship**: The visual asset used by TrayIconManager for the system tray icon.

## **List of Files being Created**

* **File 1**: Program.cs (Modified)  
  * **Purpose**: The main entry point of the application, responsible for setting up the application host, dependency injection, and initiating the desktop application's lifecycle.  
  * **Contents**: Updated Main method to configure and run a desktop application, including services like PipedreamClient, BatchProcessor, and TrayIconManager.  
  * **Relationships**: Orchestrates the startup and shutdown of MainForm, TrayIconManager, and background services.  
* **File 2**: TimeTrackerApplicationContext.cs (New)  
  * **Purpose**: A custom ApplicationContext to manage the overall lifecycle of the desktop application, ensuring proper startup and shutdown of all components.  
  * **Contents**: Constructor to initialize MainForm, TrayIconManager, and background services. Overrides OnMainFormClosed and Dispose to handle graceful shutdown.  
  * **Relationships**: Manages instances of MainForm, TrayIconManager, BatchProcessor, and other services.  
* **File 3**: MainForm.cs (New)  
  * **Purpose**: Defines the main graphical user interface window of the TimeTracker application.  
  * **Contents**: A basic Windows Forms Form with a MenuStrip (containing "File" \> "Quit"). Implements FormClosing event to minimize to tray.  
  * **Relationships**: Interacts with TrayIconManager to control its visibility and Application.Exit() for termination.  
* **File 4**: TrayIconManager.cs (New)  
  * **Purpose**: Manages the application's icon in the Windows system tray and its associated context menu.  
  * **Contents**: Encapsulates System.Windows.Forms.NotifyIcon, sets its icon and tooltip, defines ContextMenuStrip with "Exit" option, and handles click events.  
  * **Relationships**: Uses app.ico. Interacts with MainForm to hide/show and Application.Exit() for termination.  
* **File 5**: PipedreamClient.cs (Modified/Renamed from PipedreamService.cs)  
  * **Purpose**: Handles all network communication with the Pipedream endpoint.  
  * **Contents**: Contains methods for sending HTTP POST requests with JSON payloads, including a SendHeartbeatAsync method. Uses HttpClient.  
  * **Relationships**: Used by BatchProcessor. Reads PipedreamEndpointUrl from appsettings.json.  
* **File 6**: BatchProcessor.cs (New)  
  * **Purpose**: Responsible for scheduling and executing periodic background tasks, specifically sending heartbeats to Pipedream.  
  * **Contents**: Implements a System.Threading.Timer to trigger a callback function at a defined interval. Calls PipedreamClient to send data.  
  * **Relationships**: Depends on PipedreamClient. Reads configuration from appsettings.json.  
* **File 7**: app.ico (New)  
  * **Purpose**: The icon file for the TimeTracker application, displayed in the system tray.  
  * **Contents**: A standard Windows icon file.  
  * **Relationships**: Used by TrayIconManager.  
* **File 8**: appsettings.json (Modified)  
  * **Purpose**: Stores application configuration settings.  
  * **Contents**: JSON structure including PipedreamEndpointUrl and potentially BatchIntervalMinutes.  
  * **Relationships**: Read by PipedreamClient and BatchProcessor via Microsoft.Extensions.Configuration.

## **Acceptance Criteria (Overall Phase 1\)**

* The TimeTracker application successfully launches and displays its main UI window.  
* The main window can be minimized to a system tray icon by clicking the 'X' button.  
* A distinct TimeTracker icon is visible in the Windows system tray.  
* The application can be fully exited via a "File \> Quit" menu option in the main window.  
* The application can be fully exited by right-clicking the tray icon and selecting "Exit".  
* The application periodically sends a predefined heartbeat message ("TimeTracker MVP connectivity test") to the configured Pipedream endpoint.  
* This background sending occurs regardless of whether the main UI window is visible, hidden, or minimized.  
* The application process terminates cleanly from all defined exit paths without orphaned processes.

## **Testing Plan (Overall Phase 1\)**

* **Manual Testing**:  
  * **UI Launch and Minimize**: Launch the application. Verify the main window appears. Click the 'X' button and confirm the window minimizes to the system tray, and the tray icon is visible.  
  * **Exit from Main Window**: Restore the main window. Click "File \> Quit". Verify the application process terminates cleanly (check Task Manager).  
  * **Exit from Tray Icon**: Launch the application and minimize to tray. Right-click the tray icon, select "Exit". Verify the application process terminates cleanly.  
  * **Pipedream Heartbeat**: Configure a test Pipedream endpoint URL in appsettings.json. Run the application. Monitor the Pipedream dashboard for incoming requests. Verify that a request with the "TimeTracker MVP connectivity test" message is received every 5 minutes (or configured interval). Repeat this test with the main window visible, minimized, and restored to confirm independence from UI state.  
* **Log Review**:  
  * Examine the application logs (console or file) for messages indicating:  
    * Application startup and shutdown.  
    * PipedreamClient sending heartbeats (success/failure).  
    * BatchProcessor scheduling and executing tasks.  
* **Process Verification**:  
  * Use Windows Task Manager (or Process Explorer) to:  
    * Verify that TimeTracker.DesktopApp.exe (or similar process name) starts when the application is launched.  
    * Confirm that the process terminates completely after any exit method (File \> Quit, Tray \> Exit).  
    * Monitor CPU and memory usage to ensure it remains low when idle.

## **Assumptions and Dependencies**

* **Operating System**: Windows 10 or newer.  
* **Development Environment**: Visual Studio with .NET 8.0 SDK installed.  
* **.NET Runtime**: .NET 8.0 Desktop Runtime is installed on the target machine for deployment.  
* **User Permissions**: The application runs under a user account with sufficient permissions to:  
  * Display a system tray icon.  
  * Make outbound HTTP requests.  
  * Read from its own appsettings.json file.  
* **Network Connectivity**: Assumed to be generally available for Pipedream submissions.  
* **Pipedream Endpoint**: A valid and accessible Pipedream endpoint URL is provided in appsettings.json.  
* **Existing Components**: The existing PipedreamService.cs (to be refactored to PipedreamClient.cs) is available and can be adapted.

## **Non-Functional Requirements**

1. **Performance**:  
   * **CPU Usage**: The application should consume less than 1% CPU when idle and minimized to the tray.  
   * **Memory Usage**: Memory footprint should be minimal, ideally under 30MB when idle.  
2. **Stability**:  
   * The application should launch and terminate reliably without crashing.  
   * Background tasks (heartbeat) should run without causing application instability.  
3. **Usability**:  
   * The system tray icon should be clearly visible and recognizable.  
   * The context menu from the tray icon should be responsive.  
4. **Compatibility**:  
   * Windows 10+ only.  
5. **Install Size**:  
   * The deployed application size should remain under 10MB.

## **Conclusion**

This detailed backlog for Phase 1 establishes the fundamental elements required for the TimeTracker desktop application. By focusing on core UI, tray icon functionality, clean exit paths, and a basic Pipedream heartbeat, we aim to validate the essential architectural components and user interaction flows. Successful completion of this phase will provide a stable foundation for integrating more complex activity tracking and configuration features in subsequent phases.