#include <QApplication>
#include <QMainWindow>
#include <QLabel>
#include <QVBoxLayout>
#include <QWidget>
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QMessageBox>

class TimeTrackerMainWindow : public QMainWindow
{
    Q_OBJECT

public:
    TimeTrackerMainWindow(QWidget *parent = nullptr)
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

        QLabel *versionLabel = new QLabel("Version 1.0.0 - Sprint 0 MVP", this);
        versionLabel->setAlignment(Qt::AlignCenter);
        versionLabel->setStyleSheet("color: gray; font-size: 12px; margin: 10px;");

        layout->addWidget(welcomeLabel);
        layout->addWidget(statusLabel);
        layout->addWidget(versionLabel);
        layout->addStretch();

        // Setup system tray icon
        setupSystemTray();
    }

private slots:
    void showWindow()
    {
        show();
        raise();
        activateWindow();
    }

    void exitApplication()
    {
        QApplication::quit();
    }

private:
    void setupSystemTray()
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

    QSystemTrayIcon *m_trayIcon = nullptr;
};

int main(int argc, char *argv[])
{
    QApplication app(argc, argv);
    
    // Set application properties
    app.setApplicationName("Time Tracker");
    app.setApplicationVersion("1.0.0");
    app.setOrganizationName("Time Tracker Organization");
    
    // Create and show main window
    TimeTrackerMainWindow window;
    window.show();
    
    return app.exec();
}

#include "main.moc"
