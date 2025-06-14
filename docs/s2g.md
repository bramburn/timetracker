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