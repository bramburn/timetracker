# PRD: Desktop App MVP - UI, Tray Icon & Pipedream Heartbeat

## 1. Introduction

This document outlines the requirements for the Minimum Viable Product (MVP) of the TimeTracker desktop application. The primary goals for this MVP are to:
1.  Establish a basic desktop application UI.
2.  Implement system tray icon functionality, including minimizing to tray and exiting from the tray.
3.  Enable application exit via a standard menu bar option.
4.  Demonstrate background data transmission to a Pipedream endpoint with default messages, irrespective of UI visibility.

This MVP will validate the core desktop application lifecycle and its ability to perform background tasks.

## 2. Objectives

* **Feasibility of UI Display:** Successfully render and manage a basic desktop application window.
* **Tray Icon Mechanics:** Implement robust tray icon behavior for application minimization and exit.
* **Standard Exit Path:** Provide a conventional "File > Quit" option for application termination.
* **Background Data Transmission:** Ensure the application can periodically send predefined data to a Pipedream endpoint, even when the main window is not active or visible.

## 3. Target Users

* **Development Team:** To validate core application architecture and feasibility of planned features.
* **Stakeholders:** To demonstrate foundational desktop application capabilities.

## 4. Proposed Solution & Scope

The MVP will be a Windows desktop application (likely .NET C# based on existing context) with a simple main window.

**In Scope:**
* A main application window.
* System tray icon with a context menu (Right-click > Exit).
* Minimizing the main window to the tray icon when the window's close button (X) is clicked.
* A "File > Quit" menu option in the main window to exit the application.
* A background process/timer that sends a hardcoded JSON payload to a configurable Pipedream endpoint at regular intervals (e.g., every 5 minutes).
    * **Default Payload Example:**
        ```json
        {
          "timestamp": "YYYY-MM-DDTHH:mm:ssZ",
          "message": "Desktop App MVP Heartbeat",
          "appVersion": "0.1.0-mvp",
          "status": "active_background_sending"
        }
        ```

**Out of Scope for MVP:**
* User authentication.
* Actual activity tracking (mouse, keyboard, window events).
* Dynamic data payload for Pipedream (beyond the default message).
* Settings UI for Pipedream endpoint or sending interval.
* Auto-start with Windows.
* Advanced error handling or local data caching for Pipedream submissions.
* Installers or auto-updaters.

## 5. MVP Phases

### Phase 1: Core UI & Tray Implementation (Estimated: 3-4 Days)

**Goal:** Establish the basic application window, tray icon, and exit mechanisms.

**User Stories:**

* **US1.1: Display Main Application Window**
    * **As a** developer,
    * **I want** the application to launch and display a simple main window,
    * **So that** I can verify the basic UI rendering and application startup.
    * **Acceptance Criteria:**
        * [ ] Application launches without errors.
        * [ ] A main window with a title (e.g., "TimeTracker MVP") is visible.
        * [ ] The window can be moved and resized (standard window behavior).

* **US1.2: Minimize to Tray**
    * **As an** end-user,
    * **I want** the application to minimize to the system tray when I click the main window's close ('X') button,
    * **So that** the application continues running in the background without cluttering my taskbar.
    * **Acceptance Criteria:**
        * [ ] Clicking the main window's 'X' button hides the main window.
        * [ ] A TimeTracker icon appears in the system tray.
        * [ ] The application process continues to run.

* **US1.3: Restore from Tray**
    * **As an** end-user,
    * **I want** to be able to restore the main application window by clicking the tray icon,
    * **So that** I can interact with the UI again.
    * **Acceptance Criteria:**
        * [ ] Left-clicking (or double-clicking, TBD) the tray icon makes the main window visible again.
        * [ ] The tray icon remains.

* **US1.4: Exit from Tray Menu**
    * **As an** end-user,
    * **I want** to be able to completely close the application by right-clicking the tray icon and selecting "Quit" (or "Exit"),
    * **So that** I can terminate the application when needed.
    * **Acceptance Criteria:**
        * [ ] Right-clicking the tray icon displays a context menu.
        * [ ] The context menu contains an "Exit" or "Quit" option.
        * [ ] Selecting "Exit" or "Quit" completely terminates the application process.
        * [ ] The tray icon is removed.

* **US1.5: Exit from File Menu**
    * **As an** end-user,
    * **I want** to be able to completely close the application using a "File > Quit" (or "File > Exit") menu option in the main window,
    * **So that** I have a standard way to terminate the application from its UI.
    * **Acceptance Criteria:**
        * [ ] The main window has a menu bar with a "File" menu.
        * [ ] The "File" menu contains a "Quit" or "Exit" option.
        * [ ] Selecting this option completely terminates the application process.
        * [ ] If minimized to tray, the tray icon is also removed.

**Key Files/Modules (Illustrative):**
* `Program.cs`: Application entry point, main loop, potentially initial setup for DI.
* `MainForm.cs` (or `MainWindow.xaml` for WPF): Code-behind for the main application UI.
    * Handles window events (e.g., `FormClosing` for minimize to tray).
    * Implements the File > Quit menu logic.
* `TrayIconManager.cs`: Manages the `NotifyIcon` component, its context menu, and event handlers for tray interactions.
* `app.ico`: Icon file for the application and tray.

### Phase 2: Background Pipedream Heartbeat (Estimated: 2-3 Days)

**Goal:** Implement the functionality to send periodic, default messages to Pipedream.

**User Stories:**

* **US2.1: Periodic Data Sending**
    * **As a** developer,
    * **I want** the application to send a predefined JSON payload to a configured Pipedream endpoint every N minutes (e.g., 5 minutes) while the application is running,
    * **So that** I can verify the application's ability to perform background network tasks and confirm data reception at the endpoint.
    * **Acceptance Criteria:**
        * [ ] A background mechanism (e.g., `System.Threading.Timer` or a dedicated background service/thread) is implemented.
        * [ ] The mechanism triggers every N minutes (configurable, default 5 minutes).
        * [ ] On trigger, an HTTP POST request with the predefined JSON payload is sent to the Pipedream URL.
        * [ ] The Pipedream URL is configurable (e.g., via `appsettings.json`).
        * [ ] Basic logging indicates successful send or failure.

* **US2.2: Data Sending Independent of UI State**
    * **As a** developer,
    * **I want** the periodic data sending to Pipedream to continue functioning even if the main application window is hidden, minimized to tray, or not the active window,
    * **So that** I can ensure background tasks are resilient to UI state changes.
    * **Acceptance Criteria:**
        * [ ] Data is sent to Pipedream at the defined interval when the main window is visible and active.
        * [ ] Data is sent to Pipedream at the defined interval when the main window is minimized to the tray.
        * [ ] Data is sent to Pipedream at the defined interval when the main window is hidden (after 'X' is clicked).

**Key Files/Modules (Illustrative):**
* `PipedreamService.cs` (or similar): Encapsulates logic for constructing the payload and making HTTP requests to Pipedream.
    * Reads endpoint URL from configuration.
* `BackgroundHeartbeatService.cs` (or integrated into `Program.cs` or a main application service class): Manages the timer and invokes `PipedreamService` to send data.
* `appsettings.json`: Stores the Pipedream endpoint URL.
* `ActivityDataModel.cs` (or a simple DTO for the heartbeat): Defines the structure of the JSON payload.

### Phase 3: Integration & Basic Testing (Estimated: 1-2 Days)

**Goal:** Ensure all components work together smoothly and perform basic validation.

**Activities:**
* Integrate UI/Tray logic with the background sending mechanism.
* Perform end-to-end testing of all user stories.
* Verify Pipedream endpoint receives data as expected.
* Basic error checking and logging for Pipedream submissions (e.g., network errors, HTTP error codes).

**Acceptance Criteria:**
* [ ] All user stories from Phase 1 and Phase 2 are met.
* [ ] Application exits cleanly from all defined paths (File > Quit, Tray > Quit) without orphaned processes.
* [ ] Pipedream endpoint consistently receives the default messages when the app is running (visible or in tray).
* [ ] Logs show evidence of Pipedream send attempts and outcomes.

## 6. Technical Specifications

**Platform:** Windows Desktop
**Likely Technology Stack (based on context):** .NET (C#), Windows Forms or WPF.

**File Inventory (Illustrative - to be created/modified):**
* `Program.cs`: Main application entry, DI setup, application context.
* `MainForm.cs` / `MainWindow.xaml`: UI definition and primary UI logic.
* `TrayIconManager.cs`: System tray icon and context menu management.
* `PipedreamService.cs`: Logic for sending data to Pipedream.
* `BackgroundHeartbeatService.cs`: Timer and orchestration for periodic Pipedream sends.
* `appsettings.json`: Configuration file (for Pipedream URL, intervals).
* `app.ico`: Application icon.
* `ActivityDataModel.cs` (or similar for heartbeat payload).

**Architecture Diagram (High-Level):**


+---------------------+      +-----------------------+
|   MainForm / UI     |<---->|    TrayIconManager    |
| (File > Quit)       |      | (Right-click > Quit)  |
| (Window Close 'X')  |      | (Left-click > Show)   |
+---------------------+      +-----------------------+
|
| (App Lifecycle)
V
+---------------------+      +--------------------------+
| Application Core    |<---->| BackgroundHeartbeatService |
| (e.g., Program.cs   |      | (Timer, Orchestration)   |
|  or AppContext)     |      +--------------------------+
+---------------------+                  |
V
+---------------------+
|   PipedreamService  |
| (HTTP POST to       |
|  Pipedream Endpoint)|
+---------------------+
^
|
+---------------------+
|   appsettings.json  |
| (Pipedream URL)     |
+---------------------+


## 7. Acceptance Criteria (Overall MVP)

* The application successfully launches and displays its main UI.
* The main window can be minimized to a system tray icon by clicking the 'X' button.
* The application can be fully exited via a "File > Quit" menu option.
* The application can be fully exited by right-clicking the tray icon and selecting "Quit".
* The application periodically sends a predefined heartbeat message to the configured Pipedream endpoint.
* This background sending occurs regardless of whether the main UI window is visible, hidden, or minimized.
* The application exits cleanly without errors or orphaned processes.

## 8. Testing Plan (High-Level)

* **Manual Testing:**
    * Verify all UI interactions (window display, minimize, restore, File > Quit).
    * Verify all tray icon interactions (icon display, context menu, Quit from tray).
    * Monitor Pipedream endpoint to confirm receipt of heartbeat messages.
    * Test data sending with the UI visible, hidden, and minimized to tray.
    * Test application exit under various conditions.
* **Log Review:** Check application logs for errors and successful Pipedream submission messages.
* **Process Verification:** Use Task Manager to ensure the application process starts and terminates correctly.

## 9. Non-Functional Requirements (MVP Focus)

* **Stability:** Application should run without crashing during MVP defined operations.
* **Responsiveness (Basic):** UI should respond to basic user interactions (minimize, quit) without noticeable lag.
* **Resource Usage (Basic):** Application should not consume excessive CPU or memory when idle or performing background sending.

## 10. Milestones

* **Milestone 1 (End of Phase 1):** Functional UI with tray icon integration and all exit paths working.
* **Milestone 2 (End of Phase 2):** Background Pipedream heartbeat successfully sending data independently of UI state.
* **Milestone 3 (End of Phase 3):** MVP complete, integrated, and basic testing passed. All acceptance criteria met.

## 11. Risks and Mitigation

| Risk                                       | Mitigation Strategy                                                                 |
| :----------------------------------------- | :---------------------------------------------------------------------------------- |
| Tray icon does not behave as expected cross-platform (if future consideration) or on different Windows versions. | Focus on standard .NET `NotifyIcon` behavior; test on target Windows versions.         |
| Pipedream endpoint connectivity issues.    | Ensure correct URL in config; basic logging of HTTP status codes for diagnosis.     |
| Background timer/thread management errors. | Use standard .NET timer/threading mechanisms; keep logic simple for MVP.            |
| Difficulty integrating UI thread with background tasks. | Use appropriate synchronization mechanisms if needed (e.g., `Invoke` for UI updates from background threads), though MVP primarily sends data from background. |
| Application does not exit cleanly.         | Ensure all resources (timers, threads) are properly disposed of on exit.            |


