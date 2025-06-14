#include "TimeTrackerMainWindow.h"
#include <QApplication>
#include <QLabel>
#include <QVBoxLayout>
#include <QWidget>
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QMessageBox>
#include <QStyle>
#include <windows.h>
#include <fstream>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>

// Static callback functions for Windows hooks
static LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode == HC_ACTION) {
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

static LRESULT CALLBACK LowLevelMouseProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode == HC_ACTION) {
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
    // Clean up Windows hooks
    if (m_keyboardHook != nullptr) {
        UnhookWindowsHookEx(m_keyboardHook);
        m_keyboardHook = nullptr;
    }

    if (m_mouseHook != nullptr) {
        UnhookWindowsHookEx(m_mouseHook);
        m_mouseHook = nullptr;
    }
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
    
    m_trayIcon->showMessage("Time Tracker", "Application started and running in system tray",
                           QSystemTrayIcon::Information, 3000);
}

void TimeTrackerMainWindow::closeEvent(QCloseEvent *event)
{
    // Hide the window instead of closing
    hide();

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
