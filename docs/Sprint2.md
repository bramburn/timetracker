# Project Backlog: Sprints 2 & 3

### Introduction

This document provides a detailed backlog for two consecutive,
foundational sprints: Sprint 2, which focuses on implementing the
\"Minimize to Tray\" functionality, and Sprint 3, which establishes a
technical proof-of-concept for the core activity tracking mechanism.
Completing these sprints will deliver a desktop application that behaves
like a proper background utility and validates our technical approach
for the most critical feature: activity monitoring.

## **Sprint 2: Implemented \"Minimize to Tray\" Functionality**

### User Stories

- **User Story 2**: Background Application Persistence

  - **Description**: As a user, I want the application to hide to the
    system tray when I close the main window, so that it continues
    running in the background to track my activity without cluttering my
    taskbar. This \"out of sight, out of mind\" behavior is crucial for
    a utility that should be running at all times during my workday. The
    application should feel like a part of the operating system, not
    just another window to manage.

  - **Actions to Undertake**:

    1.  **Override closeEvent**: In app/TimeTrackerMainWindow.h, add the
        declaration for the closeEvent override: void
        closeEvent(QCloseEvent \*event) override;. You will also need to
        #include \<QCloseEvent\>.

    2.  **Implement Hide Logic**: In app/TimeTrackerMainWindow.cpp,
        implement the closeEvent function. Inside it, call the hide()
        method on the main window to make it invisible.

    3.  **Prevent Application Quit**: After hiding the window, call
        event-\>ignore() on the QCloseEvent object. This is the critical
        step that tells the Qt framework to disregard the close event
        and not terminate the application.

    4.  **Add User Notification**: After hiding, use the existing
        m_trayIcon to call showMessage() to display a brief,
        non-intrusive notification informing the user that the
        application is still running in the system tray. For example:
        m_trayIcon-\>showMessage(\"Time Tracker is Active\", \"The
        application continues to run in the background.\",
        QSystemTrayIcon::Information, 3000);.

  - **References between Files**:

    - app/TimeTrackerMainWindow.h and app/TimeTrackerMainWindow.cpp: The
      core logic for handling the window\'s close event will be
      implemented here, directly modifying the behavior of the
      TimeTrackerMainWindow class. It will require the \<QCloseEvent\>
      header.

  - **Acceptance Criteria**:

    1.  When the main application window is open, clicking the standard
        \'X\' (close) button in the title bar makes the window
        disappear.

    2.  After the window is hidden, the application\'s process
        (TimeTrackerApp.exe) remains active and visible in the Windows
        Task Manager.

    3.  The system tray icon remains visible and responsive after the
        main window is hidden.

    4.  Using the \"Show Window\" action from the tray icon\'s context
        menu (or double-clicking the icon) makes the main window
        reappear in its previous state.

    5.  Immediately after the window is hidden, a system tray
        notification bubble appears for approximately 3 seconds.

  - **Testing Plan**:

    - The testing will be performed manually on a built version of the
      application to ensure the user experience is as expected.

### List of Files being Created / Modified

- **File 1**: /app/TimeTrackerMainWindow.h (Modified)

  - **Purpose**: To add the declaration for the closeEvent override.

  - **Contents**: The TimeTrackerMainWindow class declaration will be
    updated to include void closeEvent(QCloseEvent \*event) override;
    and the necessary include for \<QCloseEvent\>.

  - **Relationships**: This header file defines the interface for the
    main window.

- **File 2**: /app/TimeTrackerMainWindow.cpp (Modified)

  - **Purpose**: To enhance the main window class with backgrounding
    capabilities.

  - **Contents**: The implementation of the closeEvent function will be
    added, containing the logic to hide the window and ignore the close
    event.

  - **Relationships**: This file contains the implementation for the
    TimeTrackerMainWindow class.

### Test Cases

- **Test Case 2.1**: Verify Minimize to Tray Functionality

  - **Test Data**: A running instance of the compiled
    TimeTrackerApp.exe.

  - **Expected Result**: Clicking the \'X\' button on the main window
    hides the window from the screen and the taskbar, but the
    application process continues to run. A tray notification appears
    for 2-3 seconds.

  - **Testing Tool**: Manual execution, Windows Task Manager.

- **Test Case 2.2**: Verify Window Restoration from Tray

  - **Test Data**: An instance of the application that has been
    minimized to the tray.

  - **Expected Result**: Right-clicking the tray icon and selecting
    \"Show Window\" restores the main window to the screen.
    Double-clicking the tray icon achieves the same result.

  - **Testing Tool**: Manual execution.

## **Sprint 3: Proof-of-Concept: Activity Logging to File**

### User Stories

- **User Story 3**: Core Tracking Mechanism Validation

  - **Description**: As a developer, I need to implement a basic,
    system-wide hook for keyboard and mouse events that logs activity to
    a local file, so that we can validate our technical approach for
    activity tracking before building more complex features. This sprint
    is a technical spike, designed to de-risk the project by proving our
    ability to interface with the native Windows API for system-wide
    data capture.

  - **Actions to Undertake**:

    1.  **Include Windows Headers**: Add #include \<windows.h\> and
        #include \<fstream\> to app/TimeTrackerMainWindow.cpp to access
        the necessary Win32 API functions and C++ file streams.

    2.  **Implement Hook Procedures**: In app/TimeTrackerMainWindow.cpp,
        create two static, global callback functions: LRESULT CALLBACK
        LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam)
        and LRESULT CALLBACK LowLevelMouseProc(int nCode, WPARAM wParam,
        LPARAM lParam).

    3.  **Implement File Logging**: Inside the callback functions, check
        if nCode == HC_ACTION. If true, open a text file
        (activity_log.txt) in append mode. Write a new line for each
        event, including a timestamp and event type (e.g., \"KEY_DOWN\",
        \"MOUSE_MOVE\"). Ensure the function returns
        CallNextHookEx(NULL, nCode, wParam, lParam) to pass the event
        along the hook chain.

    4.  **Set the Hooks**: In the TimeTrackerMainWindow constructor,
        call SetWindowsHookExW for both WH_KEYBOARD_LL and WH_MOUSE_LL,
        passing pointers to the callback functions. Store the returned
        hook handles (HHOOK) in private member variables of the
        TimeTrackerMainWindow class.

    5.  **Unhook on Exit**: In the TimeTrackerMainWindow destructor,
        call UnhookWindowsHookEx for both stored handles to cleanly
        release them and prevent system instability.

  - **References between Files**:

    - app/TimeTrackerMainWindow.cpp will now directly interface with the
      Windows API for event hooking.

    - A new file, activity_log.txt, will be generated by the application
      at runtime in the build output directory.

### List of Files being Created / Modified

- **File 1**: /app/TimeTrackerMainWindow.h (Modified)

  - **Purpose**: To add member variables to store the Windows hook
    handles.

  - **Contents**: The class definition will be updated to include HHOOK
    m_keyboardHook = nullptr; and HHOOK m_mouseHook = nullptr;. It will
    also need the destructor declaration \~TimeTrackerMainWindow();.

  - **Relationships**: Defines the class members.

- **File 2**: /app/TimeTrackerMainWindow.cpp (Modified)

  - **Purpose**: To add the low-level Windows hooks and file logging
    logic.

  - **Contents**: Will be updated to include the Win32 API header, the
    hook callback functions, the SetWindowsHookExW calls in the
    constructor, and the UnhookWindowsHookEx calls in the destructor.

  - **Relationships**: This file will now have a direct dependency on
    the Windows user32 library.

- **File 3**: activity_log.txt (Created at Runtime)

  - **Purpose**: To serve as a simple, human-readable output to verify
    that the event hooking mechanism is working correctly.

  - **Contents**: A series of timestamped log entries, with each entry
    corresponding to a detected keyboard or mouse event.

  - **Relationships**: This file is the direct output of the new
    functionality being added in this sprint.

### Acceptance Criteria

1.  While the application is running (either with the main window
    visible or hidden in the tray), a file named activity_log.txt is
    present in the executable\'s directory.

2.  Typing on the keyboard anywhere in the operating system appends new
    lines to activity_log.txt.

3.  Moving or clicking the mouse anywhere in the operating system
    appends new lines to activity_log.txt.

4.  The main application UI remains responsive and does not freeze while
    events are being logged.

5.  Exiting the application cleanly unhooks the listeners and stops all
    file logging.

### Testing Plan

- The testing for this proof-of-concept will involve manual user
  interaction and verification of the output log file.

### Test Cases

- **Test Case 3.1**: Verify Log File Creation and Content

  - **Test Data**: User-generated keyboard and mouse activity across
    different applications (e.g., Notepad, web browser).

  - **Expected Result**: activity_log.txt is created. After performing
    actions like typing \"hello world\" and clicking the mouse five
    times, the file contains corresponding timestamped entries.

  - **Testing Tool**: Manual execution, a text editor (e.g., Notepad++)
    to view the log file.

### Assumptions and Dependencies

- **Assumptions**:

  - The application will be developed and tested on a Windows operating
    system, as the SetWindowsHookExW API is Windows-specific.

- **Dependencies**:

  - The successful completion of Sprint 2 is required.

  - The build environment must have access to the Windows Platform SDK
    headers (which is standard with the Visual Studio C++ workload).

### Non-Functional Requirements

- **Stability**: The event hooking mechanism must not crash the
  application or the operating system. It should handle being loaded and
  unloaded cleanly when the application starts and exits.

- **Low Intrusion**: The proof-of-concept\'s file I/O should be
  lightweight enough not to cause noticeable system lag, although
  performance optimization is deferred to a later sprint.

### Conclusion

By the end of Sprint 3, the project will have a desktop application that
not only operates correctly as a background tray utility but has also
proven its ability to capture the fundamental user inputs required for
time tracking. This provides a solid and de-risked foundation to proceed
with refining the tracking logic and building the server communication
pipeline in subsequent sprints.
