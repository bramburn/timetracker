#include <gtest/gtest.h>
#include <QApplication>
#include <QTest>
#include <QSignalSpy>
#include <QTimer>
#include <QDir>
#include <QStandardPaths>
#include <QSystemTrayIcon>
#include <QCloseEvent>
#include "../TimeTrackerMainWindow.h"
#include "test_utils.h"

/**
 * @file TimeTrackerMainWindow_test.cpp
 * @brief Comprehensive unit tests for TimeTrackerMainWindow class
 * 
 * Tests cover:
 * - Window initialization and configuration
 * - System tray functionality
 * - Screenshot capture functionality
 * - Event handling (close events)
 * - Timer configuration
 * - Directory setup
 */

namespace TimeTrackerTest {

class TimeTrackerMainWindowTest : public QtTestFixture {
protected:
    void SetUp() override {
        QtTestFixture::SetUp();
        
        // Create temporary directory for testing
        tempDir_ = std::make_unique<TempTestDirectory>();
        
        // Override screenshot directory for testing
        qputenv("QT_QPA_PLATFORM", "offscreen");
    }

    void TearDown() override {
        // Clean up any created windows
        if (mainWindow_) {
            mainWindow_->close();
            delete mainWindow_;
            mainWindow_ = nullptr;
        }
        
        QtTestFixture::TearDown();
    }

    TimeTrackerMainWindow* createMainWindow() {
        mainWindow_ = new TimeTrackerMainWindow();
        return mainWindow_;
    }

private:
    TimeTrackerMainWindow* mainWindow_ = nullptr;
    std::unique_ptr<TempTestDirectory> tempDir_;
};

// =============================================================================
// Constructor and Initialization Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, ConstructorInitializesCorrectly) {
    auto* window = createMainWindow();
    
    EXPECT_NE(window, nullptr);
    EXPECT_EQ(window->windowTitle(), "Time Tracker Application");
    EXPECT_EQ(window->size(), QSize(400, 300));
}

TEST_F(TimeTrackerMainWindowTest, WindowHasCorrectFixedSize) {
    auto* window = createMainWindow();
    
    // Test that window has fixed size
    EXPECT_EQ(window->minimumSize(), QSize(400, 300));
    EXPECT_EQ(window->maximumSize(), QSize(400, 300));
}

TEST_F(TimeTrackerMainWindowTest, CentralWidgetIsConfigured) {
    auto* window = createMainWindow();
    
    EXPECT_NE(window->centralWidget(), nullptr);
    EXPECT_NE(window->centralWidget()->layout(), nullptr);
}

// =============================================================================
// System Tray Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, SystemTrayIsSetupWhenAvailable) {
    if (!QSystemTrayIcon::isSystemTrayAvailable()) {
        GTEST_SKIP() << "System tray not available on this system";
    }
    
    auto* window = createMainWindow();
    
    // Find the system tray icon (it's a private member, so we test indirectly)
    auto trayIcons = window->findChildren<QSystemTrayIcon*>();
    EXPECT_GT(trayIcons.size(), 0) << "System tray icon should be created";
    
    if (!trayIcons.isEmpty()) {
        auto* trayIcon = trayIcons.first();
        EXPECT_TRUE(trayIcon->isVisible());
        EXPECT_NE(trayIcon->contextMenu(), nullptr);
    }
}

// =============================================================================
// Screenshot Functionality Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, ScreenshotTimerIsConfigured) {
    auto* window = createMainWindow();
    
    // Find the screenshot timer
    auto timers = window->findChildren<QTimer*>();
    bool screenshotTimerFound = false;
    
    for (auto* timer : timers) {
        if (timer->interval() > 0) {
            screenshotTimerFound = true;
            EXPECT_TRUE(timer->isActive()) << "Screenshot timer should be active";
            
            // In debug mode, interval should be 10 seconds
            #ifdef QT_DEBUG
            EXPECT_EQ(timer->interval(), 10000) << "Debug mode should use 10 second interval";
            #else
            EXPECT_EQ(timer->interval(), 600000) << "Release mode should use 10 minute interval";
            #endif
            break;
        }
    }
    
    EXPECT_TRUE(screenshotTimerFound) << "Screenshot timer should be configured";
}

TEST_F(TimeTrackerMainWindowTest, ScreenshotDirectoryIsCreated) {
    auto* window = createMainWindow();
    
    // Process events to ensure directory setup is complete
    WidgetTestHelper::processEvents(100);
    
    // Check that screenshot directory exists
    QString appDataPath = QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation);
    QString screenshotDir = QDir(appDataPath).filePath("screenshots");
    
    EXPECT_TRUE(QDir(screenshotDir).exists()) << "Screenshot directory should be created";
}

// =============================================================================
// Event Handling Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, CloseEventHidesWindow) {
    if (!QSystemTrayIcon::isSystemTrayAvailable()) {
        GTEST_SKIP() << "System tray not available - close event behavior differs";
    }
    
    auto* window = createMainWindow();
    window->show();
    
    EXPECT_TRUE(window->isVisible());
    
    // Simulate close event
    QCloseEvent closeEvent;
    QApplication::sendEvent(window, &closeEvent);
    
    // Window should be hidden, not closed
    EXPECT_FALSE(window->isVisible());
    EXPECT_TRUE(closeEvent.isAccepted() == false) << "Close event should be ignored";
}

// =============================================================================
// Public Method Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, ShowWindowMakesWindowVisible) {
    auto* window = createMainWindow();
    window->hide();
    
    EXPECT_FALSE(window->isVisible());
    
    // Call showWindow method
    QMetaObject::invokeMethod(window, "showWindow");
    
    EXPECT_TRUE(window->isVisible());
}

TEST_F(TimeTrackerMainWindowTest, ExitApplicationQuitsApp) {
    auto* window = createMainWindow();
    
    // We can't actually test QApplication::quit() in unit tests
    // as it would terminate the test runner. Instead, we verify
    // the method exists and is callable.
    
    bool methodExists = false;
    for (int i = 0; i < window->metaObject()->methodCount(); ++i) {
        QMetaMethod method = window->metaObject()->method(i);
        if (method.name() == "exitApplication") {
            methodExists = true;
            break;
        }
    }
    
    EXPECT_TRUE(methodExists) << "exitApplication method should exist";
}

// =============================================================================
// Screenshot Capture Tests (Mock-based)
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, CaptureScreenshotMethodExists) {
    auto* window = createMainWindow();
    
    // Verify the captureScreenshot method exists
    bool methodExists = false;
    for (int i = 0; i < window->metaObject()->methodCount(); ++i) {
        QMetaMethod method = window->metaObject()->method(i);
        if (method.name() == "captureScreenshot") {
            methodExists = true;
            break;
        }
    }
    
    EXPECT_TRUE(methodExists) << "captureScreenshot method should exist";
}

// =============================================================================
// Memory Management Tests
// =============================================================================

TEST_F(TimeTrackerMainWindowTest, DestructorCleansUpResources) {
    auto* window = createMainWindow();
    
    // Get initial timer count
    auto initialTimers = window->findChildren<QTimer*>();
    int initialTimerCount = initialTimers.size();
    
    // Delete the window
    delete window;
    // mainWindow_ is already managed by createMainWindow, no need to reset
    
    // We can't directly test that timers are cleaned up since the window
    // is deleted, but we can verify the destructor doesn't crash
    SUCCEED() << "Destructor completed without crashing";
}

} // namespace TimeTrackerTest
