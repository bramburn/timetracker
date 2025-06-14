#include <QApplication>
#include <QMessageBox>
#include <QDir>
#include <QStandardPaths>
#include <QDebug>
#include <QLibraryInfo>
#include "TimeTrackerMainWindow.h"

int main(int argc, char *argv[])
{
    // Enable Qt debug output for platform plugins if needed
    if (qgetenv("QT_DEBUG_PLUGINS").isEmpty()) {
        qputenv("QT_DEBUG_PLUGINS", "0");
    }

    QApplication app(argc, argv);

    // Set application properties
    app.setApplicationName("TimeTrackerApp");
    app.setApplicationVersion("1.0.0");
    app.setOrganizationName("TimeTracker");

    // Debug information about Qt installation
    qDebug() << "Qt version:" << QT_VERSION_STR;
    qDebug() << "Qt library paths:" << QCoreApplication::libraryPaths();
    qDebug() << "Application directory:" << QCoreApplication::applicationDirPath();

    // Check if platforms directory exists
    QString platformsPath = QCoreApplication::applicationDirPath() + "/platforms";
    if (!QDir(platformsPath).exists()) {
        qWarning() << "Platforms directory not found at:" << platformsPath;
        QMessageBox::critical(nullptr, "Platform Plugin Error",
            QString("Platforms directory not found at:\n%1\n\n"
                   "Please run fix-qt-plugins.bat or fix-qt-platform-plugins.ps1 to fix this issue.")
                   .arg(platformsPath));
        return 1;
    }

    // Check if qwindows.dll exists (debug or release version)
    QString qwindowsPath;
#ifdef _DEBUG
    qwindowsPath = platformsPath + "/qwindowsd.dll";  // Debug version
#else
    qwindowsPath = platformsPath + "/qwindows.dll";   // Release version
#endif
    
    if (!QFile::exists(qwindowsPath)) {
        qWarning() << "Qt platform plugin not found at:" << qwindowsPath;
        
        // Try the other version as fallback
        QString fallbackPath;
#ifdef _DEBUG
        fallbackPath = platformsPath + "/qwindows.dll";
#else
        fallbackPath = platformsPath + "/qwindowsd.dll";
#endif
        
        if (QFile::exists(fallbackPath)) {
            qDebug() << "Found fallback platform plugin at:" << fallbackPath;
        } else {
            QMessageBox::critical(nullptr, "Platform Plugin Error",
                QString("Qt platform plugin not found at:\n%1\n\n"
                       "Please run fix-qt-plugins.bat or fix-qt-platform-plugins.ps1 to fix this issue.")
                       .arg(qwindowsPath));
            return 1;
        }
    }

    try {
        // Create and show main window
        TimeTrackerMainWindow window;
        window.show();

        return app.exec();
    } catch (const std::exception& e) {
        qCritical() << "Exception in main:" << e.what();
        QMessageBox::critical(nullptr, "Application Error",
            QString("An error occurred: %1").arg(e.what()));
        return 1;
    } catch (...) {
        qCritical() << "Unknown exception in main";
        QMessageBox::critical(nullptr, "Application Error",
            "An unknown error occurred while starting the application.");
        return 1;
    }
}
