# Project Backlog: Sprint 1 - Foundational Window and Tray Setup

### Introduction

This document provides a detailed backlog for Sprint 1 of the Time
Tracker application. The primary goal of this sprint is to solidify the
C++ project\'s build configuration and implement the initial user-facing
components. This involves fixing any lingering setup issues from Sprint
0, ensuring the CMake configuration is robust and repeatable, and
creating a main application window with a functional system tray icon.
This sprint will result in a stable, runnable desktop application that
serves as the foundation for all future tracking features.

### User Stories

- **User Story 1**: Stable Application Foundation

  - **Description**: As a developer, I want a reliably buildable C++/Qt6
    application that launches a main window and provides a system tray
    icon, so that we have a stable foundation for implementing the core
    tracking features in subsequent sprints.

  - **Actions to Undertake**:

    1.  **Verify and Correct CMake Configuration**: Review
        CMakeLists.txt and CMakePresets.json to ensure they are
        correctly configured for a Windows environment using Visual
        Studio 2022, Qt6, and vcpkg. Ensure Qt\'s automatic meta-object
        tools (MOC, UIC, RCC) are enabled.

    2.  **Implement Main Application Window**: Create a QMainWindow that
        serves as the primary interface. For this sprint, it will
        display static welcome and status text.

    3.  **Implement System Tray Icon**: Create a QSystemTrayIcon that is
        visible in the Windows system tray as soon as the application
        launches.

    4.  **Create Tray Icon Context Menu**: Implement a right-click
        context menu for the tray icon that provides two essential
        actions: \"Show Window\" and \"Exit\".

    5.  **Implement Tray Icon Interactivity**: Connect the context menu
        actions to application logic, allowing the user to show the main
        window or quit the application. Handle double-click events on
        the tray icon to also show the main window.

  - **References between Files**:

    - app/CMakeLists.txt will be configured by the settings in
      app/CMakePresets.json.

    - app/CMakeLists.txt will link the Qt6 libraries (Core, Gui,
      Widgets) to the TimeTrackerApp executable.

    - The executable target in app/CMakeLists.txt will be built from the
      source file app/main.cpp.

    - app/main.cpp will include headers from the Qt6 framework (e.g.,
      \<QApplication\>, \<QMainWindow\>, \<QSystemTrayIcon\>).

  - **Acceptance Criteria**:

    1.  The project in the /app directory builds successfully without
        errors using the vcpkg-qt-debug CMake build preset.

    2.  On launch, the executable (TimeTrackerApp.exe) displays a main
        window with the title \"Time Tracker Application\".

    3.  A system tray icon appears in the Windows system tray.

    4.  Right-clicking the tray icon displays a context menu with \"Show
        Window\" and \"Exit\" options.

    5.  Selecting \"Exit\" from the context menu terminates the
        application process.

    6.  If the main window is visible, double-clicking the tray icon or
        selecting \"Show Window\" from the menu keeps the window visible
        and brings it to the foreground.

  - **Testing Plan**:

    - The testing approach will involve build verification and manual
      functional testing to validate the acceptance criteria.

### List of Files being Created / Modified

- **File 1**: /app/CMakeLists.txt (Modified)

  - **Purpose**: To define the build process for the C++ desktop
    application.

  - **Contents**: This file will be reviewed and updated to ensure it
    correctly finds the Qt6 package, enables CMAKE_AUTOMOC,
    CMAKE_AUTOUIC, and CMAKE_AUTORCC for seamless Qt integration, and
    correctly links the necessary Qt libraries to the executable.

  - **Relationships**: Consumes settings from CMakePresets.json and uses
    main.cpp as a source.

- **File 2**: /app/main.cpp (Modified)

  - **Purpose**: To serve as the main entry point and define the core
    logic for the application\'s main window and system tray
    functionality.

  - **Contents**: Will contain the main function and the
    TimeTrackerMainWindow class, which inherits from QMainWindow. This
    class will handle the setup of the UI elements and the system tray
    icon, along with its associated menus and signals/slots.

  - **Relationships**: Includes Qt headers. Is compiled into the
    TimeTrackerApp executable.

- **File 3**: README.md (Created)

  - **Purpose**: To provide clear, step-by-step instructions for setting
    up the development environment.

  - **Contents**: Will detail the required software (Visual Studio 2022,
    Qt6, vcpkg, .NET SDK, Node.js), installation paths assumed in the
    configuration files, and the commands needed to build and run each
    part of the monorepo.

  - **Relationships**: A top-level project document.

### Test Cases

- **Test Case 1**: Successful Debug Build

  - **Test Data**: The source code in the /app directory.

  - **Expected Result**: Executing cmake \--build \--preset
    vcpkg-qt-debug from the /app directory successfully compiles the
    project and produces TimeTrackerApp.exe in the app/build/bin/Debug
    directory.

  - **Testing Tool**: Windows Terminal / Command Prompt, CMake.

- **Test Case 2**: Application Launch and UI Verification

  - **Test Data**: The compiled TimeTrackerApp.exe.

  - **Expected Result**: Running the executable opens a 400x300 window
    with the correct title and text. A system tray icon is
    simultaneously created.

  - **Testing Tool**: Manual execution by a tester.

- **Test Case 3**: System Tray Exit Functionality

  - **Test Data**: A running instance of the application.

  - **Expected Result**: Right-clicking the tray icon and selecting
    \"Exit\" causes the application window to close and the process to
    terminate, which can be verified in the Task Manager.

  - **Testing Tool**: Manual execution by a tester, Windows Task
    Manager.

- **Test Case 4**: System Tray Show Window Functionality

  - **Test Data**: A running instance of the application. (Note:
    Minimizing/hiding the window will be implemented in Sprint 2. This
    test case verifies the action works on an already visible window).

  - **Expected Result**: With the window visible, right-clicking the
    tray icon and selecting \"Show Window\" keeps the window visible and
    brings it to the foreground. Double-clicking the tray icon has the
    same effect.

  - **Testing Tool**: Manual execution by a tester.

### Assumptions and Dependencies

- **Assumptions**:

  - The developer has administrative privileges to install software.

  - The hardcoded paths in CMakePresets.json (C:/vcpkg/ and
    C:/Qt/6.9.0/msvc2022_64) are correct for the developer\'s machine.
    The README.md will state this assumption clearly.

- **Dependencies**:

  - Git, Visual Studio 2022 (with C++ Desktop workload), CMake, vcpkg,
    and Qt6 must be installed and configured correctly before this
    sprint can be successfully tested.

### Non-Functional Requirements

- **Performance**: The application should launch quickly (under 3
  seconds on a standard development machine) and have minimal idle CPU
  and memory usage.

- **Code Quality**: The C++ code should be clean, well-commented, and
  follow modern C++17 best practices. CMake scripts should be organized
  and readable.

- **Developer Experience**: The build process should be straightforward
  and executable with a single command, as defined in the CMake preset.

### Conclusion

Completing Sprint 1 will validate the entire toolchain and project setup
for the C++ application. It provides the team with a stable, tangible
artifact and the confidence to proceed with implementing the more
complex tracking logic in the following sprints.
