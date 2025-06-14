#ifndef TIMETRACKERMAINWINDOW_H
#define TIMETRACKERMAINWINDOW_H

#include <QMainWindow>
#include <QSystemTrayIcon>

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

private slots:
    void showWindow();
    void exitApplication();

private:
    void setupSystemTray();

    QSystemTrayIcon *m_trayIcon = nullptr;
};

#endif // TIMETRACKERMAINWINDOW_H
