# Project Backlog: Sprint 5 - Screenshot Capture Functionality

Date: June 14, 2025

Version: 1.0

## 1. Introduction

This document provides a detailed backlog for Sprint 5 of the Time
Tracker application project. The focus of this sprint is to implement
automatic screenshot capture functionality, a core feature outlined in
the Product Requirements Document (PRD). This backlog is based on the
requirements from 2025-06-14 prd.md and the technical implementation
details found in s4g.md and repomix-output2.md. The successful
completion of this sprint will result in the application\'s ability to
periodically capture the user\'s screen and save the images locally.

## 2. User Stories

### User Story 1: Automatic Screenshot Capture

- **ID**: US-001

- **Title**: As a Manager, I want the Time Tracker application to
  automatically capture screenshots of an employee\'s screen so that I
  can have a visual record of their work activity for productivity
  monitoring.

- **Description**: The desktop application needs to periodically capture
  a full-screen image of the primary monitor. These screenshots should
  be saved locally to a designated folder in a common image format
  (e.g., JPEG). The capture process must run in the background without
  interrupting the user\'s workflow and should be configurable.

- **Priority**: High

## 3. Actions to Undertake

The following actions are required to complete User Story US-001:

1.  **Modify TimeTrackerMainWindow.h:**

    - Include necessary headers: \<QTimer\>, \<QScreen\>,
      \<QGuiApplication\>, \<QStandardPaths\>, \<QDir\>.

    - Declare a QTimer\* m_screenshotTimer member to trigger captures.

    - Declare a QString m_screenshotDirectory member to store the save
      path.

    - Declare a private slot captureScreenshot() to handle the capture
      logic.

    - Declare a private method setupScreenshotDirectory() to initialize
      the save location.

2.  **Modify TimeTrackerMainWindow.cpp:**

    - In the constructor TimeTrackerMainWindow::TimeTrackerMainWindow():

      - Call setupScreenshotDirectory().

      - Initialize m_screenshotTimer (new QTimer(this)).

      - Connect the timer\'s timeout() signal to the captureScreenshot()
        slot.

      - Set the timer interval (e.g., 10 seconds for testing, 10 minutes
        for production).

      - Start the timer using m_screenshotTimer-\>start().

    - Implement setupScreenshotDirectory():

      - Use
        QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation)
        to get a standard application data path.

      - Create a \"screenshots\" subdirectory within this path using
        QDir.

      - Add debug logging (qDebug()) to confirm directory creation or
        existence.

    - Implement captureScreenshot():

      - Get the primary screen using QGuiApplication::primaryScreen().

      - Capture the screen using primaryScreen-\>grabWindow(0).

      - Generate a unique filename using a timestamp
        (QDateTime::currentDateTime().toString(\"yyyyMMdd_hhmmss\")).

      - Construct the full file path using
        QDir(m_screenshotDirectory).filePath(filename).

      - Save the captured QPixmap to the file path as a JPEG with a
        specified quality (e.g., 85%).

      - Add debug logging for success or failure of the save operation.

    - In the destructor
      TimeTrackerMainWindow::\~TimeTrackerMainWindow():

      - Stop the timer using m_screenshotTimer-\>stop() to prevent
        issues on close.

3.  **Modify app/CMakeLists.txt:**

    - Ensure Qt6::Gui is included in the find_package and
      target_link_libraries directives to support QScreen and QPixmap.

    - Link the Gdi32.lib library for Windows GDI operations, which
      grabWindow relies on.

## 4. References between Files

- **app/TimeTrackerMainWindow.cpp** will depend on
  **app/TimeTrackerMainWindow.h** for class and member declarations.

- **app/TimeTrackerMainWindow.h** will include Qt headers (\<QTimer\>,
  \<QScreen\>, etc.) for its member types.

- **app/main.cpp** creates an instance of TimeTrackerMainWindow,
  indirectly depending on the new changes.

- **app/CMakeLists.txt** links the necessary Qt6 and system libraries
  that are used by the source files. The screenshot functionality
  specifically requires Qt6::Gui and Gdi32.lib.

## 5. List of Files being Created/Modified

- **File 1**: app/TimeTrackerMainWindow.h (Modified)

  - **Purpose**: To declare the necessary members and slots for handling
    screenshot timing and capture.

  - **Contents**: Adds QTimer\*, QString, a private slot, and a private
    helper function declaration.

  - **Relationships**: Included by app/TimeTrackerMainWindow.cpp and
    app/main.cpp.

- **File 2**: app/TimeTrackerMainWindow.cpp (Modified)

  - **Purpose**: To implement the core logic for initializing the timer,
    creating the storage directory, capturing the screen, and saving the
    screenshot.

  - **Contents**: Adds implementation for the constructor, destructor,
    setupScreenshotDirectory(), and captureScreenshot().

  - **Relationships**: Includes app/TimeTrackerMainWindow.h. Depends on
    Qt Core and Gui modules.

- **File 3**: app/CMakeLists.txt (Modified)

  - **Purpose**: To ensure the project links against all necessary
    libraries for screenshot functionality.

  - **Contents**: Updates find_package and target_link_libraries to
    include Qt6::Gui and Gdi32.lib.

  - **Relationships**: Governs the build process for the entire app
    executable.

## 6. Acceptance Criteria

To consider User Story US-001 complete, the following criteria must be
met:

1.  When the application starts, a directory named screenshots must be
    automatically created in the application\'s local data folder (e.g.,
    %LOCALAPPDATA%/\<AppName\>/screenshots).

2.  The application must capture a screenshot of the entire primary
    display at a configurable, periodic interval.

3.  Each captured screenshot must be saved as a JPEG file in the
    screenshots directory.

4.  Each screenshot filename must be unique and contain a timestamp in
    the format screenshot_yyyyMMdd_hhmmss.jpg.

5.  The screenshot capture process must run in the background and not
    cause the main application UI to freeze or become unresponsive.

6.  The functionality must work correctly when the main window is hidden
    and the application is running in the system tray.

7.  The application must gracefully stop the screenshot timer when it
    exits.

## 7. Testing Plan

The following testing plan will be used to validate the acceptance
criteria.

### 7.1. Unit & Integration Testing

- **Test Case 1**: Verify Screenshot Directory Creation

  - **Description**: Check if the screenshots directory is created upon
    application startup.

  - **Test Steps**: 1. Delete the existing screenshots directory. 2.
    Launch the application. 3. Verify the screenshots directory now
    exists in the expected location.

  - **Expected Result**: The directory is created successfully.

  - **Testing Tool**: Manual verification / Filesystem check.

- **Test Case 2**: Verify Screenshot Capture Interval

  - **Description**: Ensure screenshots are captured at the specified
    interval.

  - **Test Data**: Set timer interval to 10 seconds.

  - **Test Steps**: 1. Run the application for 1 minute. 2. Check the
    screenshots directory.

  - **Expected Result**: Approximately 6 screenshot files should be
    present, created about 10 seconds apart.

  - **Testing Tool**: Manual verification / File timestamp analysis.

- **Test Case 3**: Verify Screenshot Content

  - **Description**: Confirm the captured image accurately reflects the
    screen content.

  - **Test Steps**: 1. Display a known image or application on the
    screen. 2. Let the application capture a screenshot. 3. Open the
    saved JPEG file.

  - **Expected Result**: The saved image is a clear, full-screen capture
    of the primary monitor at the time of capture.

  - **Testing Tool**: Manual verification / Image viewer.

### 7.2. User Acceptance Testing (UAT)

- **Test Case 4**: Non-blocking Background Operation

  - **Description**: Test that the application UI remains responsive
    during screenshot capture.

  - **Test Steps**: 1. Run the application. 2. Interact with the main
    window (if visible) while screenshots are being captured in the
    background. 3. Hide the window to the tray and ensure the tray icon
    remains responsive.

  - **Expected Result**: The UI does not lag or freeze. The tray icon\'s
    context menu opens without delay.

  - **Testing Tool**: Manual user interaction.

- **Test Case 5**: Graceful Shutdown

  - **Description**: Ensure the application exits cleanly without errors
    related to the timer.

  - **Test Steps**: 1. Run the application. 2. Exit the application
    using the tray icon\'s \"Exit\" action. 3. Check application logs
    for errors.

  - **Expected Result**: The application process terminates cleanly. No
    errors related to the QTimer are logged.

  - **Testing Tool**: Manual exit / Log file review.

## 8. Assumptions and Dependencies

- **Assumption 1**: The application is running on a Windows environment,
  as the implementation relies on Windows-specific APIs (Gdi32.lib) for
  screen capture.

- **Assumption 2**: The user has write permissions to the application\'s
  local data directory.

- **Dependency 1**: The project requires the Qt6 framework (Core and Gui
  modules).

- **Dependency 2**: The build environment is configured with CMake and a
  compatible C++ compiler (MSVC).

## 9. Non-Functional Requirements

- **Performance**: The screenshot capture process should have a minimal
  impact on system resources (CPU and memory). The file-saving operation
  should be efficient to avoid blocking the main thread.

- **Security**: Screenshots are stored locally and contain potentially
  sensitive information. While encryption is out of scope for Sprint 5,
  the storage location should be within the user\'s sandboxed
  application data folder.

- **Usability**: The feature should be completely automatic and require
  no user interaction after initial setup.

- **Reliability**: The screenshot mechanism must be robust and continue
  to function correctly over long periods of application runtime.

## 10. Conclusion

This backlog provides a clear and actionable plan for implementing the
screenshot capture functionality in Sprint 5. By following the user
stories, actions, and testing plan outlined here, the development team
can deliver a high-quality feature that meets the project\'s
requirements.
