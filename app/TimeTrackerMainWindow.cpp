#include "TimeTrackerMainWindow.h"
#include "ApiService.h"
#include "IdleDetector.h"
#include "IdleAnnotationDialog.h"
#include <QApplication>
#include <QLabel>
#include <QVBoxLayout>
#include <QWidget>
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QMessageBox>
#include <QStyle>
#include <QDateTime>
#include <QDebug>
#include <QFileInfo>
#include <windows.h>
#include <fstream>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>
#include <Psapi.h>

// Static instance pointer for Windows hooks
TimeTrackerMainWindow* TimeTrackerMainWindow::s_instance = nullptr;

// Static callback functions for Windows hooks
LRESULT CALLBACK TimeTrackerMainWindow::LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode == HC_ACTION) {
        // Update idle detector if instance exists
        if (s_instance && s_instance->m_idleDetector) {
            s_instance->m_idleDetector->updateLastActivityTime();
        }
        std::ofstream log("activity_log.txt", std::ios::app);
        if (log.is_open()) {
            // Get current timestamp
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);

            // Format timestamp
            std::stringstream ss;
            ss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");

            // Log keyboard event
            std::string eventType;
            switch (wParam) {
                case WM_KEYDOWN:
                    eventType = "KEY_DOWN";
                    break;
                case WM_KEYUP:
                    eventType = "KEY_UP";
                    break;
                case WM_SYSKEYDOWN:
                    eventType = "SYSKEY_DOWN";
                    break;
                case WM_SYSKEYUP:
                    eventType = "SYSKEY_UP";
                    break;
                default:
                    eventType = "KEY_OTHER";
                    break;
            }

            KBDLLHOOKSTRUCT* p = reinterpret_cast<KBDLLHOOKSTRUCT*>(lParam);
            log << ss.str() << " - " << eventType << " - VK Code: " << p->vkCode << std::endl;
            log.close();
        }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

LRESULT CALLBACK TimeTrackerMainWindow::LowLevelMouseProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode == HC_ACTION) {
        // Update idle detector if instance exists
        if (s_instance && s_instance->m_idleDetector) {
            s_instance->m_idleDetector->updateLastActivityTime();
        }
        std::ofstream log("activity_log.txt", std::ios::app);
        if (log.is_open()) {
            // Get current timestamp
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);

            // Format timestamp
            std::stringstream ss;
            ss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");

            // Log mouse event
            std::string eventType;
            switch (wParam) {
                case WM_LBUTTONDOWN:
                    eventType = "MOUSE_LEFT_DOWN";
                    break;
                case WM_LBUTTONUP:
                    eventType = "MOUSE_LEFT_UP";
                    break;
                case WM_RBUTTONDOWN:
                    eventType = "MOUSE_RIGHT_DOWN";
                    break;
                case WM_RBUTTONUP:
                    eventType = "MOUSE_RIGHT_UP";
                    break;
                case WM_MOUSEMOVE:
                    eventType = "MOUSE_MOVE";
                    break;
                case WM_MOUSEWHEEL:
                    eventType = "MOUSE_WHEEL";
                    break;
                default:
                    eventType = "MOUSE_OTHER";
                    break;
            }

            MSLLHOOKSTRUCT* p = reinterpret_cast<MSLLHOOKSTRUCT*>(lParam);
            log << ss.str() << " - " << eventType << " - X: " << p->pt.x << ", Y: " << p->pt.y << std::endl;
            log.close();
        }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

TimeTrackerMainWindow::TimeTrackerMainWindow(QWidget *parent)
    : QMainWindow(parent)
{
    // Set static instance for hook callbacks
    s_instance = this;

    setWindowTitle("Time Tracker Application");
    setFixedSize(400, 300);

    // Create central widget and layout
    QWidget *centralWidget = new QWidget(this);
    setCentralWidget(centralWidget);

    QVBoxLayout *layout = new QVBoxLayout(centralWidget);

    // Add welcome label
    QLabel *welcomeLabel = new QLabel("Welcome to Time Tracker Application", this);
    welcomeLabel->setAlignment(Qt::AlignCenter);
    welcomeLabel->setStyleSheet("font-size: 16px; font-weight: bold; margin: 20px;");

    QLabel *statusLabel = new QLabel("Application initialized successfully!", this);
    statusLabel->setAlignment(Qt::AlignCenter);
    statusLabel->setStyleSheet("color: green; margin: 10px;");

    QLabel *versionLabel = new QLabel("Version 1.0.0 - Sprint 1 Foundation", this);
    versionLabel->setAlignment(Qt::AlignCenter);
    versionLabel->setStyleSheet("color: gray; font-size: 12px; margin: 10px;");

    layout->addWidget(welcomeLabel);
    layout->addWidget(statusLabel);
    layout->addWidget(versionLabel);
    layout->addStretch();

    // Setup system tray icon
    setupSystemTray();

    // Setup screenshot directory and timer
    setupScreenshotDirectory();
    configureScreenshotTimer();

    // Setup application tracking timer
    configureAppTracker();

    // Setup idle detection
    configureIdleDetection();

    // Initialize API service for backend communication
    m_apiService = new ApiService(this);

    // Connect API service signals
    connect(m_apiService, &ApiService::screenshotUploaded,
            this, [this](bool success, const QString& filePath) {
                if (success) {
                    qDebug() << "Screenshot upload completed:" << filePath;
                } else {
                    qWarning() << "Screenshot upload failed:" << filePath;
                }
            });

    connect(m_apiService, &ApiService::activityLogsUploaded,
            this, [this](bool success) {
                if (success) {
                    qDebug() << "Activity logs upload completed successfully";
                } else {
                    qWarning() << "Activity logs upload failed";
                }
            });

    connect(m_apiService, &ApiService::idleTimeUploaded,
            this, [this](bool success) {
                if (success) {
                    qDebug() << "Idle time upload completed successfully";
                } else {
                    qWarning() << "Idle time upload failed";
                }
            });

    // Setup Windows hooks for activity tracking
    m_keyboardHook = SetWindowsHookExW(WH_KEYBOARD_LL, LowLevelKeyboardProc, GetModuleHandle(NULL), 0);
    m_mouseHook = SetWindowsHookExW(WH_MOUSE_LL, LowLevelMouseProc, GetModuleHandle(NULL), 0);

    if (m_keyboardHook == NULL || m_mouseHook == NULL) {
        QString errorMsg = QString("Failed to set up activity tracking hooks.\n");
        if (m_keyboardHook == NULL) {
            errorMsg += QString("Keyboard hook failed. Error: %1\n").arg(GetLastError());
        }
        if (m_mouseHook == NULL) {
            errorMsg += QString("Mouse hook failed. Error: %1\n").arg(GetLastError());
        }
        errorMsg += "This may require administrator privileges.";
        QMessageBox::warning(this, "Hook Setup", errorMsg);
    } else {
        // Create initial log entry to confirm hooks are working
        std::ofstream log("activity_log.txt", std::ios::app);
        if (log.is_open()) {
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);
            std::stringstream ss;
            ss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");
            log << ss.str() << " - SYSTEM - Activity tracking started" << std::endl;
            log.close();
        }
    }
}

TimeTrackerMainWindow::~TimeTrackerMainWindow()
{
    // Reset static instance
    s_instance = nullptr;

    // Stop screenshot timer
    if (m_screenshotTimer) {
        m_screenshotTimer->stop();
        qDebug() << "Screenshot timer stopped";
    }

    // Stop application tracking timer
    if (m_appTrackerTimer) {
        m_appTrackerTimer->stop();
        qDebug() << "Application tracking timer stopped";
    }

    // Stop idle detector
    if (m_idleDetector) {
        m_idleDetector->stop();
        qDebug() << "Idle detector stopped";
    }

    // Clean up Windows hooks
    if (m_keyboardHook != nullptr) {
        UnhookWindowsHookEx(m_keyboardHook);
        m_keyboardHook = nullptr;
    }

    if (m_mouseHook != nullptr) {
        UnhookWindowsHookEx(m_mouseHook);
        m_mouseHook = nullptr;
    }

    qDebug() << "TimeTrackerMainWindow destroyed and all resources cleaned up";
}

void TimeTrackerMainWindow::showWindow()
{
    show();
    raise();
    activateWindow();
}

void TimeTrackerMainWindow::exitApplication()
{
    QApplication::quit();
}

void TimeTrackerMainWindow::setupSystemTray()
{
    if (!QSystemTrayIcon::isSystemTrayAvailable()) {
        QMessageBox::critical(this, "System Tray",
                            "System tray is not available on this system.");
        return;
    }

    // Create system tray icon
    m_trayIcon = new QSystemTrayIcon(this);
    
    // Set icon (using default application icon for now)
    m_trayIcon->setIcon(style()->standardIcon(QStyle::SP_ComputerIcon));
    
    // Create context menu
    QMenu *trayMenu = new QMenu(this);
    
    QAction *showAction = new QAction("Show Window", this);
    connect(showAction, &QAction::triggered, this, &TimeTrackerMainWindow::showWindow);
    
    QAction *exitAction = new QAction("Exit", this);
    connect(exitAction, &QAction::triggered, this, &TimeTrackerMainWindow::exitApplication);
    
    trayMenu->addAction(showAction);
    trayMenu->addSeparator();
    trayMenu->addAction(exitAction);
    
    m_trayIcon->setContextMenu(trayMenu);
    m_trayIcon->show();
    
    // Connect double-click to show window
    connect(m_trayIcon, &QSystemTrayIcon::activated, this, [this](QSystemTrayIcon::ActivationReason reason) {
        if (reason == QSystemTrayIcon::DoubleClick) {
            showWindow();
        }
    });
    
    m_trayIcon->showMessage("Time Tracker",
                           QString("Application started - Screenshot capture every %1 seconds")
                           .arg(m_screenshotInterval / 1000),
                           QSystemTrayIcon::Information, 3000);
}

void TimeTrackerMainWindow::closeEvent(QCloseEvent *event)
{
    // Hide the window instead of closing
    hide();

    // Show enhanced tray notification with screenshot status
    QString message = QString("Screenshot capture and activity logging continue in background.\n"
                             "Capturing every %1 seconds at %2% quality.")
                     .arg(m_screenshotInterval / 1000)
                     .arg(m_jpegQuality);

    m_trayIcon->showMessage(
        "Time Tracker is Active",
        message,
        QSystemTrayIcon::Information,
        4000 // 4 seconds for longer message
    );

    // Prevent the app from quitting
    event->ignore();
}

void TimeTrackerMainWindow::setupScreenshotDirectory()
{
    // Get the standard AppData location for this application
    QString appDataPath = QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation);

    // Create the screenshots subdirectory path
    m_screenshotDirectory = QDir(appDataPath).filePath("screenshots");

    // Create the directory if it doesn't exist
    QDir dir;
    if (!dir.exists(m_screenshotDirectory)) {
        if (dir.mkpath(m_screenshotDirectory)) {
            qDebug() << "Created screenshots directory:" << m_screenshotDirectory;
        } else {
            qWarning() << "Failed to create screenshots directory:" << m_screenshotDirectory;
        }
    } else {
        qDebug() << "Screenshots directory already exists:" << m_screenshotDirectory;
    }
}

void TimeTrackerMainWindow::configureScreenshotTimer()
{
    // Initialize screenshot timer
    m_screenshotTimer = new QTimer(this);
    connect(m_screenshotTimer, &QTimer::timeout, this, &TimeTrackerMainWindow::captureScreenshot);

    // Configure interval based on build type
#ifdef QT_DEBUG
    m_screenshotInterval = 10 * 1000;      // 10 seconds for development/testing
#else
    m_screenshotInterval = 10 * 60 * 1000; // 10 minutes for production
#endif

    m_screenshotTimer->setInterval(m_screenshotInterval);
    m_screenshotTimer->start();

    qDebug() << "Screenshot timer configured and started:";
    qDebug() << "  Interval:" << m_screenshotInterval << "ms ("
             << (m_screenshotInterval / 1000) << "seconds)";
    qDebug() << "  Quality:" << m_jpegQuality << "%";
    qDebug() << "  Directory:" << m_screenshotDirectory;
}

void TimeTrackerMainWindow::configureAppTracker()
{
    // Initialize application tracking timer
    m_appTrackerTimer = new QTimer(this);
    connect(m_appTrackerTimer, &QTimer::timeout, this, &TimeTrackerMainWindow::trackActiveApplication);

    // Set 5-second interval as specified in Sprint 6 requirements
    m_appTrackerTimer->setInterval(5 * 1000);
    m_appTrackerTimer->start();

    qDebug() << "Application tracking timer configured and started:";
    qDebug() << "  Interval: 5 seconds";
    qDebug() << "  Tracking active window and process name changes";
}

void TimeTrackerMainWindow::trackActiveApplication()
{
    // Get the handle to the foreground window
    HWND foregroundWindow = GetForegroundWindow();
    if (foregroundWindow == NULL) {
        // No foreground window (e.g., desktop focus) - handle gracefully
        QString currentWindowTitle = "Desktop/No Active Window";
        QString currentProcessName = "Desktop";

        // Check if this is different from last known state
        if (currentWindowTitle != m_lastWindowTitle || currentProcessName != m_lastProcessName) {
            // Log the desktop focus state
            std::ofstream logFile("activity_log.txt", std::ios::app);
            if (logFile.is_open()) {
                // Get current timestamp with milliseconds
                auto now = std::chrono::system_clock::now();
                std::time_t now_c = std::chrono::system_clock::to_time_t(now);
                std::tm tm;
                localtime_s(&tm, &now_c);
                auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(
                    now.time_since_epoch()) % 1000;

                std::stringstream timestamp;
                timestamp << std::put_time(&tm, "%Y-%m-%d %H:%M:%S") << "."
                         << std::setfill('0') << std::setw(3) << milliseconds.count();

                logFile << timestamp.str() << " - ACTIVE_APP - PROCESS: " << currentProcessName.toStdString()
                       << " - TITLE: " << currentWindowTitle.toStdString() << std::endl;
                logFile.close();
            }

            // Update last known state
            m_lastWindowTitle = currentWindowTitle;
            m_lastProcessName = currentProcessName;

            qDebug() << "Active application changed to:" << currentProcessName << "-" << currentWindowTitle;
        }
        return;
    }

    // Get the window title
    wchar_t windowTitle[256];
    int titleLength = GetWindowTextW(foregroundWindow, windowTitle, sizeof(windowTitle) / sizeof(wchar_t));
    QString currentWindowTitle = QString::fromWCharArray(windowTitle, titleLength);

    // Get the process ID from the window handle
    DWORD processId;
    GetWindowThreadProcessId(foregroundWindow, &processId);

    // Open the process to get more information
    HANDLE processHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId);
    QString currentProcessName = "Unknown";

    if (processHandle != NULL) {
        // Get the full path of the executable
        wchar_t processPath[MAX_PATH];
        DWORD pathSize = MAX_PATH;

        if (QueryFullProcessImageNameW(processHandle, 0, processPath, &pathSize)) {
            QString fullPath = QString::fromWCharArray(processPath);
            // Extract just the executable name from the full path
            QFileInfo fileInfo(fullPath);
            currentProcessName = fileInfo.fileName();
        }

        // Close the process handle
        CloseHandle(processHandle);
    }

    // Check if the current window title or process name has changed
    if (currentWindowTitle != m_lastWindowTitle || currentProcessName != m_lastProcessName) {
        // Log the application change
        std::ofstream logFile("activity_log.txt", std::ios::app);
        if (logFile.is_open()) {
            // Get current timestamp with milliseconds
            auto now = std::chrono::system_clock::now();
            std::time_t now_c = std::chrono::system_clock::to_time_t(now);
            std::tm tm;
            localtime_s(&tm, &now_c);
            auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(
                now.time_since_epoch()) % 1000;

            std::stringstream timestamp;
            timestamp << std::put_time(&tm, "%Y-%m-%d %H:%M:%S") << "."
                     << std::setfill('0') << std::setw(3) << milliseconds.count();

            logFile << timestamp.str() << " - ACTIVE_APP - PROCESS: " << currentProcessName.toStdString()
                   << " - TITLE: " << currentWindowTitle.toStdString() << std::endl;
            logFile.close();
        }

        // Update last known state
        m_lastWindowTitle = currentWindowTitle;
        m_lastProcessName = currentProcessName;

        qDebug() << "Active application changed to:" << currentProcessName << "-" << currentWindowTitle;
    }
}

void TimeTrackerMainWindow::captureScreenshot()
{
    QMutexLocker locker(&m_screenshotMutex);
    qDebug() << "Capturing screenshot...";

    // Get the primary screen
    QScreen *primaryScreen = QGuiApplication::primaryScreen();
    if (!primaryScreen) {
        qWarning() << "Failed to get primary screen";
        return;
    }

    // Capture the entire screen
    QPixmap screenshot = primaryScreen->grabWindow(0);
    if (screenshot.isNull()) {
        qWarning() << "Failed to capture screenshot - grabWindow returned null";
        return;
    }

    // Generate enhanced timestamp-based filename with milliseconds
    QString timestamp = QDateTime::currentDateTime().toString("yyyyMMdd_hhmmss_zzz");
    QString filename = QString("screenshot_%1.jpg").arg(timestamp);
    QString fullPath = QDir(m_screenshotDirectory).filePath(filename);

    // Save the screenshot as JPEG with configurable quality
    if (screenshot.save(fullPath, "JPEG", m_jpegQuality)) {
        qDebug() << "Screenshot saved successfully:" << fullPath
                 << "Size:" << screenshot.size()
                 << "Quality:" << m_jpegQuality << "%";

        // Upload screenshot to server
        if (m_apiService) {
            QString userId = getCurrentUserEmail();
            QString sessionId = getCurrentSessionId();
            m_apiService->uploadScreenshot(fullPath, userId, sessionId);
        }
    } else {
        qWarning() << "Failed to save screenshot:" << fullPath;
        qWarning() << "Directory exists:" << QDir(m_screenshotDirectory).exists();
        qWarning() << "Directory writable:" << QFileInfo(m_screenshotDirectory).isWritable();
    }
}

QString TimeTrackerMainWindow::getCurrentUserEmail()
{
    // For now, return a placeholder email
    // In a real implementation, this would get the actual user email from system or configuration
    return "current_user@company.com";
}

QString TimeTrackerMainWindow::getCurrentSessionId()
{
    // For now, return a simple session ID
    // In a real implementation, this would be a unique session identifier
    static QString sessionId = QString::number(QDateTime::currentSecsSinceEpoch());
    return sessionId;
}

void TimeTrackerMainWindow::configureIdleDetection()
{
    // Initialize idle detector
    m_idleDetector = new IdleDetector(this);

    // Set 5-minute threshold (industry standard)
    m_idleDetector->setIdleThresholdSeconds(5 * 60); // 5 minutes = 300 seconds

    // Connect idle detector signals
    connect(m_idleDetector, &IdleDetector::idleStarted,
            this, &TimeTrackerMainWindow::onIdleStarted);
    connect(m_idleDetector, &IdleDetector::idleEnded,
            this, &TimeTrackerMainWindow::onIdleEnded);

    // Start the idle detector
    m_idleDetector->start();

    qDebug() << "Idle detection configured and started:";
    qDebug() << "  Threshold: 5 minutes (300 seconds)";
    qDebug() << "  Check interval: 30 seconds";
}

void TimeTrackerMainWindow::onIdleStarted(int idleThresholdSeconds)
{
    // Record when idle state started
    m_idleStartTime = QDateTime::currentDateTime().addSecs(-idleThresholdSeconds);

    qDebug() << "User entered idle state after" << idleThresholdSeconds << "seconds of inactivity";
    qDebug() << "Idle start time:" << m_idleStartTime.toString(Qt::ISODate);

    // Update system tray to show idle status
    if (m_trayIcon) {
        m_trayIcon->setToolTip("Time Tracker - User Idle");
    }
}

void TimeTrackerMainWindow::onIdleEnded(int idleDurationSeconds)
{
    qDebug() << "User activity resumed after" << idleDurationSeconds << "seconds of idle time";

    // Update system tray to show active status
    if (m_trayIcon) {
        m_trayIcon->setToolTip("Time Tracker - Active");
        m_trayIcon->showMessage("Activity Resumed",
            QString("Idle period of %1 detected").arg(formatDuration(idleDurationSeconds)),
            QSystemTrayIcon::Information, 3000);
    }

    // Show idle annotation dialog for periods longer than 1 minute
    if (idleDurationSeconds >= 60) {
        showIdleAnnotationDialog(idleDurationSeconds);
    }
}

void TimeTrackerMainWindow::showIdleAnnotationDialog(int idleDurationSeconds)
{
    QDateTime endTime = QDateTime::currentDateTime();
    QDateTime startTime = m_idleStartTime;

    // Create and show the idle annotation dialog
    IdleAnnotationDialog *dialog = new IdleAnnotationDialog(startTime, endTime, this);

    // Connect the dialog's submission signal
    connect(dialog, &IdleAnnotationDialog::annotationSubmitted,
            this, &TimeTrackerMainWindow::onIdleAnnotationSubmitted);

    // Show dialog and bring to front
    dialog->show();
    dialog->raise();
    dialog->activateWindow();

    // Ensure dialog appears on top
    dialog->setWindowFlags(Qt::Dialog | Qt::WindowStaysOnTopHint);

    qDebug() << "Showing idle annotation dialog for" << idleDurationSeconds << "seconds";
}

void TimeTrackerMainWindow::onIdleAnnotationSubmitted(const QString& reason, const QString& note)
{
    QDateTime endTime = QDateTime::currentDateTime();
    QDateTime startTime = m_idleStartTime;
    int durationSeconds = startTime.secsTo(endTime);

    // Create idle annotation data
    IdleAnnotationData data;
    data.reason = reason;
    data.note = note;
    data.startTime = startTime;
    data.endTime = endTime;
    data.durationSeconds = durationSeconds;

    // Send to server via API service
    if (m_apiService) {
        m_apiService->uploadIdleTime(data);
    }

    // Log locally for backup
    std::ofstream logFile("activity_log.txt", std::ios::app);
    if (logFile.is_open()) {
        auto now = std::chrono::system_clock::now();
        std::time_t now_c = std::chrono::system_clock::to_time_t(now);
        std::tm tm;
        localtime_s(&tm, &now_c);

        std::stringstream timestamp;
        timestamp << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");

        logFile << timestamp.str() << " - IDLE_ANNOTATED"
               << " - DURATION: " << durationSeconds << "s"
               << " - REASON: " << reason.toStdString()
               << " - NOTE: " << note.toStdString() << std::endl;
        logFile.close();
    }

    qDebug() << "Idle time annotated:" << reason << "Note:" << note << "Duration:" << durationSeconds << "seconds";
}

QString TimeTrackerMainWindow::formatDuration(int seconds)
{
    int hours = seconds / 3600;
    int minutes = (seconds % 3600) / 60;
    int secs = seconds % 60;

    if (hours > 0) {
        return QString("%1h %2m %3s").arg(hours).arg(minutes).arg(secs);
    } else if (minutes > 0) {
        return QString("%1m %2s").arg(minutes).arg(secs);
    } else {
        return QString("%1s").arg(secs);
    }
}
