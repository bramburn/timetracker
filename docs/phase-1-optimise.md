# **Backlog: Time Tracking Application Performance Optimization \- Phase 1**

## **Introduction**

This backlog details the first phase of implementing the performance optimizations outlined in the "Product Requirements Document: Time Tracking Application Performance Optimization" (Version 1.0). This phase focuses on replacing the existing polling and hook-based monitoring mechanisms with more efficient, event-driven, and asynchronous approaches. The goal is to establish a robust foundation for low-latency and resource-efficient activity tracking.

## **User Stories**

### **User Story 1: Event-Driven Window Monitoring**

* **Description**: As a user, I want the time tracking application to detect active window changes instantly and efficiently, without causing system overhead, so that my activity is accurately recorded without impacting system performance. This involves replacing the current 1000ms polling mechanism with a Win32 event hook.  
* **Actions to Undertake**:  
  1. **Create OptimizedWindowMonitor.cs**: Develop a new class OptimizedWindowMonitor that implements the IWindowMonitor interface.  
  2. **Implement SetWinEventHook**: Within OptimizedWindowMonitor, set up the SetWinEventHook API call to subscribe to EVENT\_SYSTEM\_FOREGROUND events. This will replace the existing Timer mechanism.  
  3. **Define WinEventDelegate**: Declare the WinEventDelegate and ensure it is kept alive (e.g., as a class field) to prevent garbage collection.  
  4. **Develop WinEventCallback**: Implement the WinEventCallback method to process the EVENT\_SYSTEM\_FOREGROUND events. This callback will be responsible for capturing the current window activity (hwnd) and invoking the WindowChanged event.  
  5. **Update CaptureWindowActivity**: Modify the existing CaptureWindowActivity logic (or create a new private method) to accept an IntPtr hwnd parameter, allowing it to retrieve details for the specific foreground window provided by the hook.  
  6. **Implement UnhookWinEvent**: Ensure that UnhookWinEvent is called in the Dispose method of OptimizedWindowMonitor to properly release the system hook.  
  7. **Update Program.cs**: Modify Program.cs to register OptimizedWindowMonitor in the Dependency Injection container instead of the original WindowMonitor.  
* **References between Files**:  
  * OptimizedWindowMonitor.cs (new) will interact with NativeMethods.cs for Win32 API declarations (SetWinEventHook, UnhookWinEvent, GetForegroundWindow, GetWindowTitle, GetWindowProcessId, GetProcessName).  
  * ActivityLogger.cs (existing, but will be modified in User Story 3\) will consume the WindowChanged event from the IWindowMonitor interface, now implemented by OptimizedWindowMonitor.  
  * IWindowMonitor.cs (existing) interface will be implemented by OptimizedWindowMonitor.cs.  
* **Acceptance Criteria**:  
  * The WindowMonitor component no longer uses a System.Threading.Timer for polling active windows.  
  * Changes in the foreground window are detected and reported by OptimizedWindowMonitor within 100ms of the actual window switch.  
  * The CPU usage attributed solely to the OptimizedWindowMonitor component (excluding logging and data processing) is less than 0.1 during continuous operation.  
  * The WindowChanged event is fired only when there is a significant change in the active window's title or process name, preventing redundant logging.  
  * Disposing OptimizedWindowMonitor correctly unhooks the WinEvent hook without errors or resource leaks.  
* **Testing Plan**:  
  * **Test Case 1.1**: Verify SetWinEventHook Installation and Event Firing.  
    * **Test Data**: N/A (simulated window changes).  
    * **Expected Result**: OptimizedWindowMonitor starts successfully, and its WindowChanged event is triggered when a foreground window change occurs (e.g., switching between applications).  
    * **Testing Tool**: NUnit (for unit tests), manual observation (for integration).  
  * **Test Case 1.2**: Measure CPU Usage of OptimizedWindowMonitor.  
    * **Test Data**: Application running for an extended period with varying window activity.  
    * **Expected Result**: CPU usage of the OptimizedWindowMonitor component remains consistently below 0.1.  
    * **Testing Tool**: Performance counters (e.g., Windows Performance Monitor, Visual Studio Profiler).  
  * **Test Case 1.3**: Verify Dispose Cleans Up Hook.  
    * **Test Data**: N/A.  
    * **Expected Result**: Calling Dispose on OptimizedWindowMonitor successfully unhooks the event, and no exceptions or memory leaks are observed.  
    * **Testing Tool**: NUnit, memory profiler.

### **User Story 2: Raw Input API for Input Monitoring with Basic Buffering**

* **Description**: As a user, I want the time tracking application to detect my keyboard and mouse activity with minimal latency and system impact, even during rapid input, so that my active status is accurately maintained without causing input lag. This involves replacing low-level hooks with the Raw Input API and introducing a basic buffered queue for event handling.  
* **Actions to Undertake**:  
  1. **Create OptimizedInputMonitor.cs**: Develop a new class OptimizedInputMonitor that implements the IInputMonitor interface.  
  2. **Implement HiddenInputWindow**: Create an internal Form class named HiddenInputWindow within OptimizedInputMonitor to serve as a message-only window for receiving Raw Input messages. Ensure it's not visible and does not appear in the taskbar.  
  3. **Register Raw Input Devices**: Use NativeMethods.RegisterRawInputDevices to register for keyboard (Usage=0x06) and mouse (Usage=0x02) raw input, targeting the Handle of the HiddenInputWindow.  
  4. **Handle WM\_INPUT in WndProc**: Override the WndProc method in HiddenInputWindow to capture WM\_INPUT messages. Inside WndProc, enqueue a simple InputEvent (containing at least a DateTime.UtcNow timestamp) into a BlockingCollection\<InputEvent\>.  
  5. **Create InputEvent Class**: Define a simple InputEvent class to encapsulate the necessary data (e.g., Timestamp, InputType).  
  6. **Implement Background Processing Thread**: Create a dedicated Thread (\_processingThread) that continuously consumes InputEvent objects from the BlockingCollection. Set its Priority to ThreadPriority.AboveNormal.  
  7. **Implement Basic Debouncing**: In the HandleInputEvent method (executed by the processing thread), introduce a basic debouncing mechanism (e.g., ignore subsequent events if they occur within 50ms of the last processed event) before updating \_lastInputTime and \_currentActivityStatus.  
  8. **Manage Activity Timeout**: Ensure the existing \_activityTimeoutTimer and CheckActivityTimeout logic correctly utilizes \_lastInputTime (updated by the processing thread) to determine ActivityStatus.Inactive.  
  9. **Proper Disposal**: Implement comprehensive Dispose methods for OptimizedInputMonitor, HiddenInputWindow, \_inputQueue, \_cts, and ensure the processing thread is gracefully stopped (\_cts.Cancel() and \_processingThread.Join()).  
  10. **Update Program.cs**: Modify Program.cs to register OptimizedInputMonitor in the Dependency Injection container instead of the original InputMonitor.  
* **References between Files**:  
  * OptimizedInputMonitor.cs (new) will interact with NativeMethods.cs for RegisterRawInputDevices and potentially GetLastInputInfo (if used as a fallback or for initial status). It will also use System.Collections.Concurrent.BlockingCollection and System.Threading.Tasks.  
  * ActivityLogger.cs (existing, but will be modified in User Story 3\) will consume the ActivityStatusChanged event from the IInputMonitor interface, now implemented by OptimizedInputMonitor.  
  * IInputMonitor.cs (existing) interface will be implemented by OptimizedInputMonitor.cs.  
  * System.Windows.Forms will be a dependency for HiddenInputWindow.  
* **Acceptance Criteria**:  
  * The InputMonitor component no longer uses SetWindowsHookEx for low-level keyboard or mouse hooks.  
  * Keyboard and mouse inputs are detected by OptimizedInputMonitor and reflected in ActivityStatus within 8ms of the input event.  
  * The CPU usage attributed solely to the OptimizedInputMonitor component (excluding logging and data processing) is consistently below 1.5 during continuous operation, even during rapid input bursts.  
  * No perceptible input lag or system slowdown is observed when the application is running, particularly during high-frequency input.  
  * The ActivityStatusChanged event is fired correctly when the user's activity status transitions between Active and Inactive.  
  * Disposing OptimizedInputMonitor correctly stops the processing thread and releases resources without errors.  
* **Testing Plan**:  
  * **Test Case 2.1**: Verify Raw Input Device Registration and Queueing.  
    * **Test Data**: N/A (simulated keyboard/mouse input).  
    * **Expected Result**: OptimizedInputMonitor starts successfully, raw input devices are registered, and InputEvent objects are correctly enqueued into the BlockingCollection upon keyboard/mouse activity.  
    * **Testing Tool**: NUnit, debugger.  
  * **Test Case 2.2**: Measure Input Latency and CPU Usage during High-Frequency Input.  
    * **Test Data**: Automated script simulating rapid keyboard presses and mouse movements.  
    * **Expected Result**: Input latency remains ≤8ms, and CPU usage remains ≤1.5.  
    * **Testing Tool**: Performance profiler, custom latency measurement tool.  
  * **Test Case 2.3**: Verify Activity Status Transitions.  
    * **Test Data**: Periods of active input followed by periods of no input (idle).  
    * **Expected Result**: ActivityStatusChanged event fires correctly, transitioning between Active and Inactive based on ActivityTimeoutMs.  
    * **Testing Tool**: NUnit, manual observation.  
  * **Test Case 2.4**: Verify Debouncing Logic.  
    * **Test Data**: Rapid, closely spaced input events (e.g., multiple key presses within 50ms).  
    * **Expected Result**: Only one ActivityStatusChanged event (or a minimal number based on debouncing) is triggered for a burst of input within the debounce threshold.  
    * **Testing Tool**: NUnit, custom event logger.

### **User Story 3: Integrate Optimized Monitors into ActivityLogger**

* **Description**: As a system administrator, I want the time tracking application to use the newly optimized window and input monitoring components, so that the overall system performance benefits are realized for accurate activity tracking.  
* **Actions to Undertake**:  
  1. **Update ActivityLogger Constructor**: Modify the constructor of the existing ActivityLogger class to accept instances of IWindowMonitor and IInputMonitor (which will be resolved as OptimizedWindowMonitor and OptimizedInputMonitor via Dependency Injection).  
  2. **Verify Event Subscriptions**: Ensure that ActivityLogger correctly subscribes to the WindowChanged event from IWindowMonitor and ActivityStatusChanged event from IInputMonitor.  
  3. **Verify LogActivityAsync Trigger**: Confirm that LogActivityAsync is correctly invoked by the event handlers (OnWindowChanged, OnActivityStatusChanged) based on events from the new monitors.  
  4. **Review StartAsync and Stop**: Ensure ActivityLogger.StartAsync() and ActivityLogger.Stop() correctly call Start() and Stop() on the injected IWindowMonitor and IInputMonitor instances.  
  5. **Review GetStatusInfo**: Verify GetStatusInfo correctly retrieves status from the injected IInputMonitor and IPipedreamClient.  
* **References between Files**:  
  * Program.cs (modified) will be responsible for setting up the Dependency Injection container to provide OptimizedWindowMonitor and OptimizedInputMonitor when ActivityLogger is constructed.  
  * ActivityLogger.cs (modified) will depend on IWindowMonitor.cs and IInputMonitor.cs interfaces.  
  * OptimizedWindowMonitor.cs and OptimizedInputMonitor.cs will be the concrete implementations provided to ActivityLogger.  
* **Acceptance Criteria**:  
  * The Time Tracking application successfully starts and runs as a Windows Service, utilizing the OptimizedWindowMonitor and OptimizedInputMonitor components.  
  * Activity data (including window changes and activity status) is continuously and accurately logged to the local SQLite database based on events received from the new monitoring components.  
  * No runtime errors, crashes, or unhandled exceptions occur during the startup, continuous operation, or shutdown of the application due to the integration of the new monitors.  
  * The ActivityLogger's status information (GetStatusInfo) accurately reflects the state reported by the new monitors.  
* **Testing Plan**:  
  * **Test Case 3.1**: End-to-End Application Startup and Operation.  
    * **Test Data**: Standard user activity (browsing, typing, idle periods).  
    * **Expected Result**: Application starts without errors, logs activity to the database, and shows correct status in GetStatusInfo. No perceptible lag or high CPU usage.  
    * **Testing Tool**: Manual testing, system logs, database inspection, Windows Task Manager.  
  * **Test Case 3.2**: Verify Event Flow from New Monitors to Logger.  
    * **Test Data**: Simulate specific window changes and input activity.  
    * **Expected Result**: ActivityLogger's OnWindowChanged and OnActivityStatusChanged methods are invoked as expected, and LogActivityAsync is called with correct data.  
    * **Testing Tool**: Integration tests with mocked IDataAccess and IPipedreamClient, debugger.  
  * **Test Case 3.3**: Application Shutdown Gracefulness.  
    * **Test Data**: N/A.  
    * **Expected Result**: Application shuts down cleanly, and all monitoring components are disposed without errors or hanging processes.  
    * **Testing Tool**: Manual testing, system logs.

## **Actions to Undertake (Consolidated)**

1. **Develop OptimizedWindowMonitor.cs**:  
   * Implement IWindowMonitor interface.  
   * Replace Timer with SetWinEventHook for EVENT\_SYSTEM\_FOREGROUND.  
   * Define and manage WinEventDelegate lifecycle.  
   * Implement WinEventCallback to capture window activity and invoke WindowChanged.  
   * Adapt CaptureWindowActivity to use IntPtr hwnd.  
   * Implement UnhookWinEvent in Dispose.  
2. **Develop OptimizedInputMonitor.cs**:  
   * Implement IInputMonitor interface.  
   * Create nested HiddenInputWindow : Form for Raw Input message reception.  
   * Implement NativeMethods.RegisterRawInputDevices targeting HiddenInputWindow.Handle.  
   * Override HiddenInputWindow.WndProc to enqueue InputEvent objects into BlockingCollection.  
   * Define InputEvent class.  
   * Initialize and start a high-priority background \_processingThread to consume events.  
   * Implement basic debouncing logic in HandleInputEvent.  
   * Ensure \_activityTimeoutTimer and CheckActivityTimeout use the processed \_lastInputTime.  
   * Implement comprehensive Dispose logic for all resources.  
3. **Modify ActivityLogger.cs**:  
   * Update constructor to accept IWindowMonitor and IInputMonitor.  
   * Verify existing event subscriptions work with the new interfaces.  
   * Ensure StartAsync and Stop correctly interact with the new monitors.  
4. **Modify Program.cs**:  
   * Update DI registration to use OptimizedWindowMonitor and OptimizedInputMonitor.  
5. **Update appsettings.json**:  
   * Add/verify ActivityTimeoutMs configuration.

## **References between Files**

* **OptimizedWindowMonitor.cs**:  
  * **Depends on**: NativeMethods.cs (for Win32 API calls), IWindowMonitor.cs (implements).  
  * **Used by**: ActivityLogger.cs (consumes WindowChanged event), Program.cs (registers).  
* **OptimizedInputMonitor.cs**:  
  * **Depends on**: NativeMethods.cs (for Win32 API calls), IInputMonitor.cs (implements), System.Windows.Forms (for HiddenInputWindow), System.Collections.Concurrent.BlockingCollection.  
  * **Used by**: ActivityLogger.cs (consumes ActivityStatusChanged event), Program.cs (registers).  
* **ActivityLogger.cs**:  
  * **Depends on**: IWindowMonitor.cs, IInputMonitor.cs, IDataAccess.cs, IPipedreamClient.cs.  
  * **Uses**: OptimizedWindowMonitor and OptimizedInputMonitor (via DI).  
* **Program.cs**:  
  * **Depends on**: OptimizedWindowMonitor.cs, OptimizedInputMonitor.cs, ActivityLogger.cs (for DI setup).  
* **NativeMethods.cs**:  
  * **Used by**: OptimizedWindowMonitor.cs, OptimizedInputMonitor.cs.  
  * **Modifications**: Potentially add RegisterRawInputDevices and UnhookRawInputDevices (if needed for explicit unregistration), GetRawInputData (if parsing raw input data is required beyond just detection).  
* **appsettings.json**:  
  * **Configures**: ActivityTimeoutMs.

## **List of Files being Created**

1. **File 1**: desktop-app/TimeTracker.DesktopApp/OptimizedWindowMonitor.cs  
   * **Purpose**: Implements event-driven active window monitoring using SetWinEventHook.  
   * **Contents**: Class definition for OptimizedWindowMonitor, Win32 API imports, WinEventDelegate, WinEventCallback implementation, Start/Stop/Dispose methods, and integration with existing window capture logic.  
   * **Relationships**: Implements IWindowMonitor, uses NativeMethods.  
2. **File 2**: desktop-app/TimeTracker.DesktopApp/OptimizedInputMonitor.cs  
   * **Purpose**: Implements low-latency input monitoring using Raw Input API and a buffered processing queue.  
   * **Contents**: Class definition for OptimizedInputMonitor, nested HiddenInputWindow class, Win32 API imports for Raw Input, BlockingCollection for input events, dedicated processing thread logic, InputEvent helper class, debouncing logic, and Start/Stop/Dispose methods.  
   * **Relationships**: Implements IInputMonitor, uses NativeMethods, depends on System.Windows.Forms.  
3. **File 3**: desktop-app/TimeTracker.DesktopApp/InputEvent.cs (or nested within OptimizedInputMonitor.cs)  
   * **Purpose**: A simple data model to represent a captured input event for the buffered queue.  
   * **Contents**: Properties for Timestamp and InputType.  
   * **Relationships**: Used by OptimizedInputMonitor.  
4. **File 4**: desktop-app/TimeTracker.DesktopApp/NativeMethods.cs (Modification)  
   * **Purpose**: To add necessary P/Invoke declarations for RegisterRawInputDevices and other Raw Input related functions if not already present.  
   * **Contents**: Addition of RAWINPUTDEVICE struct, RegisterRawInputDevices declaration.  
   * **Relationships**: Used by OptimizedInputMonitor.  
5. **File 5**: desktop-app/TimeTracker.DesktopApp/Program.cs (Modification)  
   * **Purpose**: To update the Dependency Injection container to use the new OptimizedWindowMonitor and OptimizedInputMonitor implementations.  
   * **Contents**: Changes in RegisterServices method to replace old monitor registrations with new ones.  
   * **Relationships**: Configures OptimizedWindowMonitor and OptimizedInputMonitor for ActivityLogger.  
6. **File 6**: desktop-app/TimeTracker.DesktopApp/ActivityLogger.cs (Modification)  
   * **Purpose**: To update its constructor to accept the new IWindowMonitor and IInputMonitor interfaces.  
   * **Contents**: Constructor signature change, ensuring existing event subscriptions are compatible.  
   * **Relationships**: Consumes events from IWindowMonitor and IInputMonitor.  
7. **File 7**: desktop-app/TimeTracker.DesktopApp/appsettings.json (Modification)  
   * **Purpose**: To update configuration settings relevant to the new monitoring logic.  
   * **Contents**: Ensure ActivityTimeoutMs is correctly defined.  
   * **Relationships**: Provides configuration to OptimizedInputMonitor.

## **Acceptance Criteria (Consolidated)**

* **Functional Correctness**:  
  * The application starts and runs as a Windows Service without errors.  
  * Active window changes are detected immediately and accurately.  
  * Keyboard and mouse activity is detected with high precision and low latency.  
  * User activity status (Active/Inactive) is correctly determined based on input and configured timeout.  
  * Activity data is consistently logged to the local database based on detected changes.  
* **Performance**:  
  * Input Latency: 99th percentile input event processing time ≤8ms.  
  * CPU Usage: Peak CPU usage during input bursts ≤4. Average CPU usage during continuous operation ≤1.5.  
  * No perceptible input lag or UI freezes.  
* **Reliability**:  
  * No crashes or unhandled exceptions under normal or high-load conditions.  
  * Graceful shutdown and resource release.

## **Testing Plan (Consolidated)**

**Methodology**: A combination of unit tests, integration tests, and manual end-to-end testing will be employed. Performance profiling will be crucial.  
**Tools**:

* **Unit/Integration Testing**: NUnit, Moq (for mocking dependencies).  
* **Performance Profiling**: Visual Studio Profiler, Windows Performance Monitor (Perfmon).  
* **System Monitoring**: Windows Task Manager, Process Explorer.

**Test Cases**:

* **Test Case 1**: Application Startup and Continuous Operation.  
  * **Test Data**: Standard system environment, various applications open, periods of active use and idle time.  
  * **Expected Result**: Application starts successfully, runs as a background service, logs activity, and exhibits low CPU/memory usage. No input lag.  
  * **Testing Tool**: Manual testing, Task Manager, system logs.  
* **Test Case 2**: Window Change Detection Accuracy and Latency.  
  * **Test Data**: Rapid switching between different applications (e.g., browser, IDE, text editor).  
  * **Expected Result**: New window titles and process names are logged immediately after switching. Latency for detection is minimal (\<100ms).  
  * **Testing Tool**: Unit tests for OptimizedWindowMonitor, manual observation, custom logging.  
* **Test Case 3**: Input Activity Detection and Status Transitions.  
  * **Test Data**: Simulate bursts of keyboard typing and mouse movements, followed by periods of complete inactivity.  
  * **Expected Result**: ActivityStatus transitions to Active immediately upon input and to Inactive precisely after ActivityTimeoutMs of no input. No input lag is observed during high-frequency input.  
  * **Testing Tool**: Unit tests for OptimizedInputMonitor, automated input simulation, performance profiler.  
* **Test Case 4**: Resource Consumption Under Load.  
  * **Test Data**: Extended periods of high user activity (e.g., continuous typing, mouse movements) and high system CPU load from other applications.  
  * **Expected Result**: Application's CPU usage remains within specified KPIs (≤4 peak, ≤1.5 average). Memory footprint remains stable.  
  * **Testing Tool**: Performance profiler, Windows Performance Monitor.  
* **Test Case 5**: Graceful Shutdown.  
  * **Test Data**: Stopping the Windows service.  
  * **Expected Result**: Application shuts down cleanly, all hooks are uninstalled, threads are terminated, and resources are released without errors.  
  * **Testing Tool**: Manual testing, system logs, Process Explorer.

## **Assumptions and Dependencies**

* **Operating System**: The application is developed for and will run on Windows. Win32 API calls are specific to Windows.  
* **.NET Runtime**: .NET 8.0 runtime is available on the target system.  
* **Windows Forms for Hidden Window**: The use of System.Windows.Forms.Form for the hidden window is acceptable within the Windows Service context.  
* **Administrator Privileges**: The application (as a Windows Service) will have sufficient privileges to install WinEvent hooks and register Raw Input devices.  
* **P/Invoke Declarations**: All necessary Win32 API P/Invoke declarations in NativeMethods.cs will be correctly defined and accessible.  
* **Existing ActivityDataModel**: The ActivityDataModel and ActivityStatus enum are stable and meet the data requirements.  
* **Existing IDataAccess and IPipedreamClient**: The interfaces for data access and Pipedream client remain stable, and their implementations (even if not optimized in this phase) are functional.  
* **Logging Infrastructure**: The Microsoft.Extensions.Logging infrastructure is correctly set up and functional.

## **Non-Functional Requirements**

* **Performance**: As detailed in Section 5 (KPIs).  
* **Resource Efficiency**: Minimal CPU and memory footprint, especially during idle periods.  
* **Stability**: The application must be robust and not crash or interfere with system operations, even under adverse conditions (e.g., high CPU load, network issues).  
* **Reliability**: Activity data must be consistently captured and stored locally.  
* **Maintainability**: Code should be clean, well-commented, and follow C\# best practices for easy future modifications.  
* **Security**: No sensitive user input content should be captured or logged by the monitoring components. The application should adhere to the principle of least privilege.  
* **Usability (Indirect)**: The application should operate seamlessly in the background without the user noticing its presence through performance degradation or input lag.

## **Conclusion**

This backlog provides a detailed plan for the first phase of performance optimization, focusing on critical improvements to window and input monitoring. By implementing event-driven and asynchronous mechanisms, we aim to significantly enhance the application's responsiveness, reduce system impact, and lay a solid foundation for subsequent optimization phases. Successful completion of this phase will directly address the primary performance bottlenecks identified in the PRD, leading to a more efficient and user-friendly time tracking experience.