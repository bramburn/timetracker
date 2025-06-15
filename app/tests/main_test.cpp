#include <gtest/gtest.h>
#include <QString>
#include <QApplication>
#include <QTest>
#include <QDir>
#include <QStandardPaths>
#include "../TimeTrackerMainWindow.h"
#include "test_utils.h"

/**
 * @file main_test.cpp
 * @brief Main test file with framework sanity tests and basic application tests
 *
 * This file contains:
 * - Framework sanity tests to ensure GTest is working
 * - Basic application tests to verify core functionality
 * - Integration tests for overall application behavior
 */

// =============================================================================
// Framework Sanity Tests
// =============================================================================

TEST(FrameworkSanityTest, CanRun) {
    EXPECT_TRUE(true);
}

TEST(FrameworkSanityTest, QtIntegrationWorks) {
    // Test that Qt integration works properly
    QString testString = "Time Tracker Application";
    EXPECT_EQ(testString.toStdString(), "Time Tracker Application");

    // Test QString operations
    EXPECT_EQ(testString.length(), 24);
    EXPECT_TRUE(testString.contains("Tracker"));
    EXPECT_TRUE(testString.startsWith("Time"));
    EXPECT_TRUE(testString.endsWith("Application"));
}

TEST(FrameworkSanityTest, TestUtilitiesWork) {
    // Test that our test utilities are functional
    using namespace TimeTrackerTest;

    // Test data generator
    QString screenshotPath = TestDataGenerator::generateTestScreenshotPath();
    EXPECT_TRUE(screenshotPath.contains("test_screenshots"));
    EXPECT_TRUE(screenshotPath.endsWith(".png"));

    QString activityLog = TestDataGenerator::generateTestActivityLog();
    EXPECT_TRUE(activityLog.contains("Test activity"));
}

// =============================================================================
// Application Sanity Tests
// =============================================================================

TEST(ApplicationSanityTest, ClassIsAccessible) {
    // Test that we can access the class and its methods without creating instances
    // This verifies that the refactoring was successful and the class is properly linked

    // Test that the header is included properly by checking if we can reference the class
    // We use sizeof to verify the class exists without instantiating it
    EXPECT_GT(sizeof(TimeTrackerMainWindow), 0);
}

TEST(ApplicationSanityTest, ClassHasExpectedMethods) {
    // Verify that the TimeTrackerMainWindow class has expected public methods
    const QMetaObject* metaObject = &TimeTrackerMainWindow::staticMetaObject;

    bool hasShowWindow = false;
    bool hasExitApplication = false;
    bool hasCaptureScreenshot = false;

    for (int i = 0; i < metaObject->methodCount(); ++i) {
        QMetaMethod method = metaObject->method(i);
        QString methodName = method.name();

        if (methodName == "showWindow") hasShowWindow = true;
        if (methodName == "exitApplication") hasExitApplication = true;
        if (methodName == "captureScreenshot") hasCaptureScreenshot = true;
    }

    EXPECT_TRUE(hasShowWindow) << "TimeTrackerMainWindow should have showWindow method";
    EXPECT_TRUE(hasExitApplication) << "TimeTrackerMainWindow should have exitApplication method";
    EXPECT_TRUE(hasCaptureScreenshot) << "TimeTrackerMainWindow should have captureScreenshot method";
}

// =============================================================================
// Basic Integration Tests
// =============================================================================

class ApplicationIntegrationTest : public TimeTrackerTest::QtTestFixture {
protected:
    void SetUp() override {
        QtTestFixture::SetUp();
        qputenv("QT_QPA_PLATFORM", "offscreen");
    }
};

TEST_F(ApplicationIntegrationTest, ApplicationCanBeCreatedAndDestroyed) {
    TimeTrackerMainWindow* window = new TimeTrackerMainWindow();
    EXPECT_NE(window, nullptr);

    // Process events to ensure initialization
    TimeTrackerTest::WidgetTestHelper::processEvents(100);

    delete window;
    // If we get here without crashing, creation and destruction work
    SUCCEED();
}

TEST_F(ApplicationIntegrationTest, ApplicationHasCorrectInitialState) {
    TimeTrackerMainWindow window;

    EXPECT_EQ(window.windowTitle(), "Time Tracker Application");
    EXPECT_EQ(window.size(), QSize(400, 300));
    EXPECT_NE(window.centralWidget(), nullptr);
}

// =============================================================================
// Environment and Configuration Tests
// =============================================================================

TEST(EnvironmentTest, StandardPathsAreAccessible) {
    // Test that Qt standard paths work in test environment
    QString appDataPath = QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation);
    EXPECT_FALSE(appDataPath.isEmpty()) << "AppLocalDataLocation should be available";

    QString tempPath = QStandardPaths::writableLocation(QStandardPaths::TempLocation);
    EXPECT_FALSE(tempPath.isEmpty()) << "TempLocation should be available";
}

TEST(EnvironmentTest, DirectoryOperationsWork) {
    // Test basic directory operations needed by the application
    QString tempDir = QDir::tempPath() + "/timetracker_test_" +
                     QString::number(QDateTime::currentMSecsSinceEpoch());

    // Create directory
    EXPECT_TRUE(QDir().mkpath(tempDir)) << "Should be able to create test directory";
    EXPECT_TRUE(QDir(tempDir).exists()) << "Created directory should exist";

    // Clean up
    EXPECT_TRUE(QDir(tempDir).removeRecursively()) << "Should be able to remove test directory";
}

// =============================================================================
// Build Configuration Tests
// =============================================================================

TEST(BuildConfigurationTest, DebugModeDetection) {
    #ifdef QT_DEBUG
    SUCCEED() << "Running in DEBUG mode";
    #else
    SUCCEED() << "Running in RELEASE mode";
    #endif
}

TEST(BuildConfigurationTest, WindowsPlatformDetection) {
    #ifdef WIN32
    SUCCEED() << "Running on Windows platform";
    #else
    FAIL() << "This application is designed for Windows platform";
    #endif
}

TEST(BuildConfigurationTest, QtVersionCompatibility) {
    // Verify we're using a compatible Qt version
    QString qtVersion = qVersion();
    EXPECT_FALSE(qtVersion.isEmpty()) << "Qt version should be available";

    // We expect Qt 6.x
    EXPECT_TRUE(qtVersion.startsWith("6.")) << "Should be using Qt 6.x, got: " << qtVersion.toStdString();
}
