#ifndef TIMETRACKERMAINWINDOW_H
#define TIMETRACKERMAINWINDOW_H

#include <QMainWindow>
#include <QSystemTrayIcon>
#include <QCloseEvent>
#include <QTimer>
#include <QStandardPaths>
#include <QDir>
#include <QScreen>
#include <QGuiApplication>
#include <QDateTime>
#include <QPixmap>
#include <QDebug>
#include <QMutex>
#include <windows.h>
#include <Psapi.h>
#include <string>
#include <vector>

// Forward declaration
class ApiService;

QT_BEGIN_NAMESPACE
class QLabel;
class QVBoxLayout;
class QWidget;
class QMenu;
class QAction;
QT_END_NAMESPACE

class TimeTrackerMainWindow : public QMainWindow
{
    Q_OBJECT

public:
    TimeTrackerMainWindow(QWidget *parent = nullptr);
    ~TimeTrackerMainWindow();

protected:
    void closeEvent(QCloseEvent *event) override;

private slots:
    void showWindow();
    void exitApplication();
    void captureScreenshot();
    void trackActiveApplication();

private:
    void setupSystemTray();
    void setupScreenshotDirectory();
    void configureScreenshotTimer();
    void configureAppTracker();
    QString getCurrentUserEmail();
    QString getCurrentSessionId();

    QSystemTrayIcon *m_trayIcon = nullptr;

    // Screenshot functionality
    QTimer *m_screenshotTimer = nullptr;
    QString m_screenshotDirectory;
    QMutex m_screenshotMutex;

    // Configuration settings
    int m_screenshotInterval = 10 * 1000;  // 10 seconds for testing
    int m_jpegQuality = 85;                // 85% quality for good compression

    // Application tracking components
    QTimer *m_appTrackerTimer = nullptr;
    QString m_lastWindowTitle;
    QString m_lastProcessName;

    // Windows hook handles for activity tracking
    HHOOK m_keyboardHook = nullptr;
    HHOOK m_mouseHook = nullptr;

    // API service for backend communication
    ApiService *m_apiService = nullptr;
};

#endif // TIMETRACKERMAINWINDOW_H
