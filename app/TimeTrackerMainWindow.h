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
#include <windows.h>

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

private:
    void setupSystemTray();
    void setupScreenshotDirectory();

    QSystemTrayIcon *m_trayIcon = nullptr;

    // Screenshot functionality
    QTimer *m_screenshotTimer = nullptr;
    QString m_screenshotDirectory;

    // Windows hook handles for activity tracking
    HHOOK m_keyboardHook = nullptr;
    HHOOK m_mouseHook = nullptr;
};

#endif // TIMETRACKERMAINWINDOW_H
