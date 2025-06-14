#ifndef TIMETRACKERMAINWINDOW_H
#define TIMETRACKERMAINWINDOW_H

#include <QMainWindow>
#include <QSystemTrayIcon>
#include <QCloseEvent>
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

private:
    void setupSystemTray();

    QSystemTrayIcon *m_trayIcon = nullptr;

    // Windows hook handles for activity tracking
    HHOOK m_keyboardHook = nullptr;
    HHOOK m_mouseHook = nullptr;
};

#endif // TIMETRACKERMAINWINDOW_H
