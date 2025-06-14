#include <QApplication>
#include "TimeTrackerMainWindow.h"

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
