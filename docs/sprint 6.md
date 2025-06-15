# **Project Backlog: Sprint 6 \- Active Application Tracking**

Date: June 14, 2025  
Version: 1.0

## **1\. Introduction**

This document provides a detailed backlog for Sprint 6 of the Time Tracker application project. The primary goal of this sprint is to implement active application tracking, as specified in the Product Requirements Document (PRD) under feature F-04. This functionality will enable the application to identify and log the name and window title of the application the user is actively working in. This builds upon the foundational tracking mechanisms established in previous sprints.

## **2\. User Stories**

### **User Story 1: Track Active Application Usage**

* **ID**: US-002  
* **Title**: As a Manager, I want to know which applications and window titles an employee is actively using, so I can understand how their time is allocated across different tasks and software.  
* **Description**: The desktop application must periodically identify the foreground application that the user is interacting with. It needs to capture both the name of the executable (e.g., WINWORD.EXE) and the title of the active window (e.g., "Project Proposal \- Microsoft Word"). This information should be logged locally with a timestamp for later transmission to the server. The process must be efficient and run seamlessly in the background.  
* **Priority**: High

## **3\. Actions to Undertake**

To complete User Story US-002, the following technical tasks must be performed:

1. **Modify TimeTrackerMainWindow.h:**  
   * Include necessary Windows API headers: \<windows.h\>, \<string\>, \<vector\>.  
   * Declare a new QTimer\* m\_appTrackerTimer member for periodic application checking.  
   * Declare a private slot trackActiveApplication() to contain the tracking logic.  
   * Declare member variables to store the last known window title and process name to avoid redundant logging: QString m\_lastWindowTitle; and QString m\_lastProcessName;.  
2. **Modify TimeTrackerMainWindow.cpp:**  
   * Include additional headers: \<Psapi.h\>.  
   * In the constructor TimeTrackerMainWindow::TimeTrackerMainWindow():  
     * Initialize the new timer: m\_appTrackerTimer \= new QTimer(this);.  
     * Connect the timer's timeout() signal to the trackActiveApplication() slot.  
     * Set a reasonable interval for the timer (e.g., 5 \* 1000 for 5 seconds).  
     * Start the timer: m\_appTrackerTimer-\>start().  
   * Implement the trackActiveApplication() slot:  
     * Get the handle to the foreground window using GetForegroundWindow().  
     * Get the window title using GetWindowTextW().  
     * Get the process ID (PID) from the window handle using GetWindowThreadProcessId().  
     * Open the process with OpenProcess() to get a process handle.  
     * Get the executable's full path name using QueryFullProcessImageNameW().  
     * Extract just the executable name from the full path.  
     * Compare the current window title and process name with the last stored values. If they are different, log the new information to activity\_log.txt and update the member variables.  
     * Ensure all Windows handles (HWND, process handles) are closed or released properly.  
   * In the destructor TimeTrackerMainWindow::\~TimeTrackerMainWindow():  
     * Stop the new timer: m\_appTrackerTimer-\>stop().  
3. **Modify app/CMakeLists.txt:**  
   * Ensure the Psapi.lib library is linked to the target executable. This is required for QueryFullProcessImageNameW. This should be added to the target\_link\_libraries section for the WIN32 platform.

## **4\. References between Files**

* **app/TimeTrackerMainWindow.cpp** will implement the logic defined in **app/TimeTrackerMainWindow.h**.  
* **app/TimeTrackerMainWindow.h** will declare the new timer and slot, and will include Windows-specific headers.  
* **app/main.cpp** remains the entry point and will instantiate the modified TimeTrackerMainWindow.  
* **app/CMakeLists.txt** is critical as it must link User32.lib (for window functions) and Psapi.lib (for process functions) to the final executable.

## **5\. List of Files being Created/Modified**

* **File 1**: app/TimeTrackerMainWindow.h (Modified)  
  * **Purpose**: To declare the components needed for active application tracking.  
  * **Contents**: Adds a new QTimer\*, a private slot trackActiveApplication(), and string members to hold the last tracked state.  
  * **Relationships**: Included by TimeTrackerMainWindow.cpp.  
* **File 2**: app/TimeTrackerMainWindow.cpp (Modified)  
  * **Purpose**: To implement the logic for polling the active window and logging its details.  
  * **Contents**: Adds the timer initialization and the implementation of trackActiveApplication() using Windows API calls.  
  * **Relationships**: Includes TimeTrackerMainWindow.h and system headers like \<Psapi.h\>.  
* **File 3**: app/CMakeLists.txt (Modified)  
  * **Purpose**: To link against the necessary Windows platform libraries.  
  * **Contents**: Adds Psapi.lib to the target\_link\_libraries command.  
  * **Relationships**: Controls the final linking stage of the build process.

## **6\. Acceptance Criteria**

For User Story US-002 to be marked as complete, the following criteria must be satisfied:

1. The application must log the active application's details to activity\_log.txt at a regular interval (e.g., every 5 seconds).  
2. The log entry must contain a timestamp, the process name (e.g., chrome.exe), and the full window title.  
3. A new log entry for the active application should only be created when the foreground window or its title changes.  
4. The tracking functionality must not cause any noticeable performance degradation or UI freezing.  
5. The tracking must continue to function correctly when the application's main window is hidden and it is running in the system tray.  
6. If no window is in the foreground (e.g., the user is focused on the desktop), this state should be handled gracefully without errors.

## **7\. Testing Plan**

The following plan will be used to validate the implementation.

### **7.1. Unit & Integration Testing**

* **Test Case 1**: Verify Application Switching  
  * **Description**: Ensure that changing the active application is correctly logged.  
  * **Test Steps**: 1\. Open Notepad and type. 2\. Switch to a web browser and navigate to a site. 3\. Switch to another application like File Explorer. 4\. Inspect activity\_log.txt.  
  * **Expected Result**: The log file contains distinct entries for notepad.exe, chrome.exe (or other browser), and explorer.exe, along with their respective window titles.  
  * **Testing Tool**: Manual application switching / Log file review.  
* **Test Case 2**: Verify Window Title Change  
  * **Description**: Ensure that changing the title of the active window is logged.  
  * **Test Steps**: 1\. In a web browser, navigate from one website to another (which changes the tab/window title). 2\. In an editor, open different documents. 3\. Inspect activity\_log.txt.  
  * **Expected Result**: New log entries appear when the window title changes, even if the process name remains the same.  
  * **Testing Tool**: Manual user interaction / Log file review.  
* **Test Case 3**: Verify No Redundant Logging  
  * **Description**: Ensure that logs are not spammed if the active window remains the same.  
  * **Test Steps**: 1\. Keep a single application window active and in the foreground for 1 minute. 2\. Inspect activity\_log.txt.  
  * **Expected Result**: Only one log entry for the active application should be created at the beginning of the period. No further entries for that same application and title should appear.  
  * **Testing Tool**: Manual verification / Log file review.

### **7.2. User Acceptance Testing (UAT)**

* **Test Case 4**: Background Operation and Performance  
  * **Description**: Test that the application remains responsive while tracking is active.  
  * **Test Steps**: 1\. Let the application run in the background (minimized to tray) for an extended period. 2\. Restore the window and interact with the UI.  
  * **Expected Result**: The application UI is responsive, and system performance is not noticeably impacted.  
  * **Testing Tool**: Manual user interaction / Task Manager for resource monitoring.

## **8\. Assumptions and Dependencies**

* **Assumption 1**: The target platform is Windows, as the implementation relies heavily on the Windows API (User32.lib, Psapi.lib).  
* **Dependency 1**: The project is built using Qt6 (Core, Widgets, Gui modules).  
* **Dependency 2**: The build system is CMake, and the compiler is MSVC.  
* **Dependency 3**: The necessary Windows Platform SDK is installed and accessible to the compiler/linker.

## **9\. Non-Functional Requirements**

* **Performance**: The polling mechanism for checking the active window should be lightweight. The interval should be configured to balance responsiveness with CPU usage (a 3-5 second interval is recommended).  
* **Reliability**: The application must handle cases where window or process information cannot be retrieved (e.g., due to security restrictions on system processes) without crashing.  
* **Accuracy**: The tracking should correctly identify the application that has user input focus.

## **10\. Conclusion**

This backlog outlines the necessary steps to implement active application tracking. This feature is a critical component of the core data collection engine. Successful completion of this sprint will significantly enhance the application's monitoring capabilities, paving the way for data transmission and server-side analysis in subsequent sprints.