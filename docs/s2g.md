To implement **Sprint 2**—"Minimize to Tray"—for your Qt6/C++ application, follow these steps, which are directly mapped to your backlog and acceptance criteria[1].

---

## **Sprint 2: Minimize to Tray Implementation**

### **Objective**
When the user closes the main window (clicks the 'X'), the window should hide to the system tray, the app process should remain running, and a notification should inform the user that the app is still active. The tray icon must remain functional, allowing the user to restore the window from the tray.

---

## **Step-by-Step Implementation**

### **1. Update the Main Window Header**

**File:** `app/TimeTrackerMainWindow.h`

- Add the `closeEvent` override.
- Include ``.

```cpp
// app/TimeTrackerMainWindow.h

#pragma once
#include 
#include 
#include 
#include  // Needed for closeEvent

class TimeTrackerMainWindow : public QMainWindow {
    Q_OBJECT
public:
    explicit TimeTrackerMainWindow(QWidget* parent = nullptr);
    ~TimeTrackerMainWindow();

protected:
    void closeEvent(QCloseEvent *event) override; // Add this declaration

private:
    QSystemTrayIcon* m_trayIcon;
    QMenu* m_trayMenu;
    // ... other members ...
};
```
**API:**  
- [QMainWindow::closeEvent(QCloseEvent*)](https://doc.qt.io/qt-6/qmainwindow.html#closeEvent)
- [QSystemTrayIcon](https://doc.qt.io/qt-6/qsystemtrayicon.html)

---

### **2. Implement the Minimize-to-Tray Logic**

**File:** `app/TimeTrackerMainWindow.cpp`

- Implement `closeEvent` to hide the window, ignore the event, and show a tray notification.

```cpp
// app/TimeTrackerMainWindow.cpp

#include "TimeTrackerMainWindow.h"
#include 
#include 

TimeTrackerMainWindow::TimeTrackerMainWindow(QWidget* parent)
    : QMainWindow(parent)
{
    // ... existing setup code ...
    m_trayIcon = new QSystemTrayIcon(QIcon(":/icon.png"), this);
    m_trayMenu = new QMenu(this);

    QAction* showAction = new QAction("Show Window", this);
    QAction* exitAction = new QAction("Exit", this);

    connect(showAction, &QAction::triggered, this, [this]() {
        this->show();
        this->raise();
        this->activateWindow();
    });
    connect(exitAction, &QAction::triggered, qApp, &QApplication::quit);

    m_trayMenu->addAction(showAction);
    m_trayMenu->addAction(exitAction);
    m_trayIcon->setContextMenu(m_trayMenu);

    connect(m_trayIcon, &QSystemTrayIcon::activated, this, [this](QSystemTrayIcon::ActivationReason reason) {
        if (reason == QSystemTrayIcon::DoubleClick) {
            this->show();
            this->raise();
            this->activateWindow();
        }
    });

    m_trayIcon->show();
}

void TimeTrackerMainWindow::closeEvent(QCloseEvent* event)
{
    // Hide the window instead of closing
    this->hide();

    // Show tray notification
    m_trayIcon->showMessage(
        "Time Tracker is Active",
        "The application continues to run in the background.",
        QSystemTrayIcon::Information,
        3000 // 3 seconds
    );

    // Prevent the app from quitting
    event->ignore();
}
```
**API:**  
- [QSystemTrayIcon::showMessage](https://doc.qt.io/qt-6/qsystemtrayicon.html#showMessage)
- [QCloseEvent::ignore](https://doc.qt.io/qt-6/qcloseevent.html#ignore)

---

### **3. Test and Validate**

#### **Acceptance Criteria Checklist**
- Clicking the window 'X' hides the window, does not quit the app, and shows a tray notification for ~3s.
- The tray icon remains visible and responsive.
- "Show Window" from the tray menu or double-clicking the tray icon restores the window.
- The process remains running (check in Task Manager).
- Manual testing as described in the backlog[1].

---

## **API/External Package Guidance**

- **Qt6**: All functionality uses Qt6 Widgets and Core modules. No additional packages are required for this sprint.
- **Platform**: This approach is cross-platform, but tray icon behavior and notifications are best on Windows (as per your backlog's assumptions).
- **vcpkg**: Ensure `qtbase` is installed via vcpkg.
- **CMake**: No changes needed if Qt6 is already integrated.

---

## **Further Guidance**

- **Tray icon resource**: Place your icon PNG in a Qt resource file or the executable directory.
- **Restore logic**: Always use `show()`, `raise()`, and `activateWindow()` for best user experience.
- **Notification**: The message is non-intrusive and lasts 3 seconds, as required.

---

## **Summary Table**

| Task                       | File                          | API/Class                | Key Method/Signal         |
|----------------------------|-------------------------------|--------------------------|---------------------------|
| Add closeEvent override    | app/TimeTrackerMainWindow.h   | QMainWindow, QCloseEvent | closeEvent(QCloseEvent*)  |
| Hide on close, notify user | app/TimeTrackerMainWindow.cpp | QSystemTrayIcon          | showMessage(), hide(), ignore() |
| Tray menu/restore          | app/TimeTrackerMainWindow.cpp | QSystemTrayIcon, QMenu   | activated, triggered      |

---

**You now have a complete, Qt6-native "minimize to tray" implementation for Sprint 2, ready for manual testing and further development.**[1]

[1] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/35208055/e19792e0-3642-47e4-a168-b98c97aadb40/Sprint2.md
[2] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/collection_59dddd7d-a43e-4981-8f37-8f1d5f4c9bdc/39178ad2-15a6-4478-bf2f-d68b85feb663/paste.txt
[3] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/collection_59dddd7d-a43e-4981-8f37-8f1d5f4c9bdc/4f544544-aaa8-49e7-aeb7-0a8f03e3efd0/how-does-3dsurvey-software-app-wUW6gHbzTGSnOP8HJzfduA.md

To implement **Sprint 3: Proof-of-Concept Activity Logging to File** for your Qt6/C++ desktop application, follow these steps, which directly address your backlog, acceptance criteria, and technical requirements[1].

---

## **Sprint 3 Objective**

**Goal:**  
Implement a system-wide Windows hook for keyboard and mouse events that logs activity to a local file (`activity_log.txt`). This validates your ability to capture user activity at the OS level, even while the app is minimized to the tray.

---

## **Step-by-Step Implementation**

### **1. Update Class Declaration**

**File:** `app/TimeTrackerMainWindow.h`

Add Windows hook handles and destructor:

```cpp
#pragma once
#include 
#include 
#include 
#include 
#include  // 
#include 
#include 
#include 
#include 
```

**Static callback functions for hooks:**

```cpp
static LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION) {
        KBDLLHOOKSTRUCT* p = reinterpret_cast(lParam);
        std::ofstream log("activity_log.txt", std::ios::app);
        if (log.is_open()) {
            // Timestamp
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);
            log vkCode (lParam);
        std::ofstream log("activity_log.txt", std::ios::app);
        if (log.is_open()) {
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);
            log pt.x pt.y `, ``, `` for timestamps.
- **No third-party packages** needed for this sprint.

---

## **Summary Table**

| Task                        | File                          | API/Class                  | Key Methods/Functions         |
|-----------------------------|-------------------------------|----------------------------|-------------------------------|
| Add hook handles/destructor | app/TimeTrackerMainWindow.h   | HHOOK, ~TimeTrackerMainWindow |                               |
| Implement hooks/logging     | app/TimeTrackerMainWindow.cpp | SetWindowsHookExW, CallNextHookEx, std::ofstream |                               |
| Cleanup on exit             | app/TimeTrackerMainWindow.cpp | UnhookWindowsHookEx        |                               |
| Log file output             | activity_log.txt (runtime)    | std::ofstream              |                               |

---

**You now have a working technical proof-of-concept for global activity logging in your Qt6 app, as required by Sprint 3.**  
This validates your ability to capture system-wide input and lays the groundwork for more advanced tracking features in future sprints[1].

[1] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/35208055/e19792e0-3642-47e4-a168-b98c97aadb40/Sprint2.md
[2] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/collection_59dddd7d-a43e-4981-8f37-8f1d5f4c9bdc/39178ad2-15a6-4478-bf2f-d68b85feb663/paste.txt
[3] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/collection_59dddd7d-a43e-4981-8f37-8f1d5f4c9bdc/4f544544-aaa8-49e7-aeb7-0a8f03e3efd0/how-does-3dsurvey-software-app-wUW6gHbzTGSnOP8HJzfduA.md

Based on the PRD documentation, here's the complete implementation for **Sprint 3: Proof-of-Concept Activity Logging to File**[1][2]:

## **Sprint 3 Implementation Overview**

This sprint validates the core Windows API hooking mechanism by implementing basic keyboard and mouse event logging to a local file[2].

**1. Updated Header File (TimeTrackerMainWindow.h)**
```cpp
#pragma once
#include 
#include 
#include 
#include 
#include 
#include 

class TimeTrackerMainWindow : public QMainWindow {
    Q_OBJECT

public:
    explicit TimeTrackerMainWindow(QWidget *parent = nullptr);
    ~TimeTrackerMainWindow();

protected:
    void closeEvent(QCloseEvent *event) override;

private slots:
    void toggleWindowVisibility();
    void onTrayIconActivated(QSystemTrayIcon::ActivationReason reason);

private:
    void createTrayIcon();
    void createActions();
    
    // UI Components
    QSystemTrayIcon *m_trayIcon;
    QMenu *m_trayMenu;
    QAction *m_showHideAction;
    QAction *m_quitAction;
    
    // Windows API Hook Handles
    HHOOK m_keyboardHook = nullptr;
    HHOOK m_mouseHook = nullptr;
    
    // Static callback functions for Windows hooks
    static LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam);
    static LRESULT CALLBACK LowLevelMouseProc(int nCode, WPARAM wParam, LPARAM lParam);
};
```

**2. Implementation File (TimeTrackerMainWindow.cpp)**
```cpp
#include "TimeTrackerMainWindow.h"
#include 
#include 
#include 
#include 
#include 
#include 
#include 

TimeTrackerMainWindow::TimeTrackerMainWindow(QWidget *parent)
    : QMainWindow(parent) {
    setWindowTitle("Time Tracker Application");
    setFixedSize(400, 300);
    
    createActions();
    createTrayIcon();
    
    // Install Windows API hooks for system-wide monitoring
    m_keyboardHook = SetWindowsHookExW(
        WH_KEYBOARD_LL,
        LowLevelKeyboardProc,
        GetModuleHandle(nullptr),
        0
    );
    
    m_mouseHook = SetWindowsHookExW(
        WH_MOUSE_LL,
        LowLevelMouseProc,
        GetModuleHandle(nullptr),
        0
    );
    
    if (!m_keyboardHook) {
        qCritical() isVisible()) {
        hide();
        event->ignore();
        m_trayIcon->showMessage(
            "Time Tracker is Active",
            "The application continues to run in the background.",
            QSystemTrayIcon::Information,
            3000
        );
    }
}

// Static callback for keyboard events
LRESULT CALLBACK TimeTrackerMainWindow::LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION) {
        KBDLLHOOKSTRUCT* keyboardStruct = reinterpret_cast(lParam);
        
        // Get current timestamp
        auto now = std::chrono::system_clock::now();
        auto time_t = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast(
            now.time_since_epoch()) % 1000;
        
        std::stringstream timestamp;
        timestamp vkCode (lParam);
        
        // Get current timestamp
        auto now = std::chrono::system_clock::now();
        auto time_t = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast(
            now.time_since_epoch()) % 1000;
        
        std::stringstream timestamp;
        timestamp pt.x pt.y addAction(m_showHideAction);
    m_trayMenu->addSeparator();
    m_trayMenu->addAction(m_quitAction);

    m_trayIcon = new QSystemTrayIcon(this);
    m_trayIcon->setContextMenu(m_trayMenu);
    m_trayIcon->setIcon(QIcon(":/icons/tray_icon.png"));
    m_trayIcon->show();

    connect(m_trayIcon, &QSystemTrayIcon::activated, 
            this, &TimeTrackerMainWindow::onTrayIconActivated);
}

void TimeTrackerMainWindow::onTrayIconActivated(QSystemTrayIcon::ActivationReason reason) {
    if (reason == QSystemTrayIcon::DoubleClick) {
        showNormal();
    }
}
```

**3. Updated CMakeLists.txt**
```cmake
cmake_minimum_required(VERSION 3.16)
project(TimeTrackerApp LANGUAGES CXX)

set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_CXX_STANDARD 17)

find_package(Qt6 REQUIRED COMPONENTS Widgets)

add_executable(TimeTrackerApp
    main.cpp
    TimeTrackerMainWindow.cpp
    TimeTrackerMainWindow.h
)

target_link_libraries(TimeTrackerApp PRIVATE
    Qt6::Widgets
    User32.lib  # Required for Windows API hooks
)

# Include resources for tray icon
qt_add_resources(TimeTrackerApp "resources"
    PREFIX "/"
    FILES
        icons/tray_icon.png
)
```

## **Key Implementation Details**

**Windows API Components Used:**
- `SetWindowsHookExW()`: Installs low-level keyboard/mouse hooks[2]
- `WH_KEYBOARD_LL`/`WH_MOUSE_LL`: Hook types for global input monitoring[2]
- `KBDLLHOOKSTRUCT`/`MSLLHOOKSTRUCT`: Structs containing event details[2]
- `CallNextHookEx()`: Passes events to next hook in chain[2]

**Testing Protocol:**
1. **Verify Log File Creation**: Run application and confirm `activity_log.txt` appears[2]
2. **Test Cross-Application Tracking**: Type in Notepad, browser, etc. - all should log[2]
3. **Mouse Event Validation**: Click, scroll, right-click across different windows[2]
4. **UI Responsiveness**: Ensure main window remains functional during logging[2]

**Expected Log Output:**
```
2025-06-14 21:14:23.456 - KEY_DOWN - VK_CODE: 72
2025-06-14 21:14:23.467 - KEY_UP - VK_CODE: 72
2025-06-14 21:14:24.123 - MOUSE_LEFT_DOWN - POS: (342,156)
2025-06-14 21:14:24.134 - MOUSE_LEFT_UP - POS: (342,156)
```

This implementation successfully validates the Windows API hooking mechanism required for the full time-tracking system while maintaining Qt6's cross-platform architecture[1][2].

[1] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/35208055/a25a2d4a-3383-4864-9be6-453155cda6fa/prd0.md
[2] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/35208055/6e8bf437-dc5d-4fd5-ae3a-50b45f2bb41d/Sprint2.md
[3] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/attachments/35208055/177dac62-5df9-48ec-806b-2d11bb1a28db/prd1.md
[4] https://ppl-ai-file-upload.s3.amazonaws.com/web/direct-files/collection_82368f44-3b85-4ad4-9901-c96324d445f3/b9226a62-9487-4ef0-abdb-55b9bfca8fb4/other-than-csharp-net-is-there-_oaIW2tbTxOPVA4jFigSXQ.md