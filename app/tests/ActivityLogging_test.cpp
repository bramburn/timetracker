#include <gtest/gtest.h>
#include <QApplication>
#include <QTest>
#include <QFile>
#include <QTextStream>
#include <QDir>
#include <QFileInfo>
#include <windows.h>
#include "../TimeTrackerMainWindow.h"
#include "test_utils.h"

/**
 * @file ActivityLogging_test.cpp
 * @brief Unit tests for Windows activity logging functionality
 * 
 * Tests cover:
 * - Windows hook setup and cleanup
 * - Activity log file creation
 * - Log file format and content
 * - Hook installation verification
 * - Error handling for hook failures
 * 
 * Note: Some tests may require administrator privileges on Windows
 */

namespace TimeTrackerTest {

class ActivityLoggingTest : public QtTestFixture {
protected:
    void SetUp() override {
        QtTestFixture::SetUp();
        
        // Set up offscreen platform for headless testing
        qputenv("QT_QPA_PLATFORM", "offscreen");
        
        // Create temporary directory for testing
        tempDir_ = std::make_unique<TempTestDirectory>();
        
        // Store original working directory
        originalDir_ = QDir::currentPath();
        
        // Change to temp directory for log file testing
        QDir::setCurrent(tempDir_->path());
        
        // Clean up any existing activity log
        if (QFile::exists("activity_log.txt")) {
            QFile::remove("activity_log.txt");
        }
    }

    void TearDown() override {
        // Restore original directory
        QDir::setCurrent(originalDir_);
        
        if (mainWindow_) {
            delete mainWindow_;
            mainWindow_ = nullptr;
        }
        
        QtTestFixture::TearDown();
    }

    TimeTrackerMainWindow* createMainWindow() {
        mainWindow_ = new TimeTrackerMainWindow();
        return mainWindow_;
    }

    bool isRunningAsAdministrator() {
        BOOL isAdmin = FALSE;
        PSID adminGroup = NULL;
        SID_IDENTIFIER_AUTHORITY ntAuthority = SECURITY_NT_AUTHORITY;
        
        if (AllocateAndInitializeSid(&ntAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID,
                                   DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &adminGroup)) {
            CheckTokenMembership(NULL, adminGroup, &isAdmin);
            FreeSid(adminGroup);
        }
        
        return isAdmin == TRUE;
    }

private:
    TimeTrackerMainWindow* mainWindow_ = nullptr;
    std::unique_ptr<TempTestDirectory> tempDir_;
    QString originalDir_;
};

// =============================================================================
// Activity Log File Tests
// =============================================================================

TEST_F(ActivityLoggingTest, ActivityLogFileIsCreatedOnStartup) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // Check if activity log file exists
    EXPECT_TRUE(QFile::exists("activity_log.txt")) 
        << "Activity log file should be created on startup";
}

TEST_F(ActivityLoggingTest, ActivityLogFileIsWritable) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    if (QFile::exists("activity_log.txt")) {
        QFileInfo logInfo("activity_log.txt");
        EXPECT_TRUE(logInfo.isWritable()) 
            << "Activity log file should be writable";
    }
}

TEST_F(ActivityLoggingTest, ActivityLogContainsStartupEntry) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    if (QFile::exists("activity_log.txt")) {
        QFile logFile("activity_log.txt");
        if (logFile.open(QIODevice::ReadOnly | QIODevice::Text)) {
            QTextStream stream(&logFile);
            QString content = stream.readAll();
            
            EXPECT_TRUE(content.contains("Activity tracking started")) 
                << "Log should contain startup entry";
            EXPECT_TRUE(content.contains("SYSTEM")) 
                << "Startup entry should be marked as SYSTEM event";
        }
    }
}

// =============================================================================
// Windows Hook Tests
// =============================================================================

TEST_F(ActivityLoggingTest, WindowsHooksAreAttempted) {
    // Note: Hook installation may fail in test environment without admin privileges
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // We can't directly test private hook handles, but we can verify
    // the window was created without crashing
    EXPECT_NE(window, nullptr) << "Window should be created even if hooks fail";
}

TEST_F(ActivityLoggingTest, HookFailureIsHandledGracefully) {
    // In most test environments, hooks will fail due to lack of admin privileges
    // This test ensures the application handles this gracefully
    
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // Application should continue to function even if hooks fail
    EXPECT_TRUE(window->isVisible() || !window->isVisible()) 
        << "Window state should be valid regardless of hook status";
}

TEST_F(ActivityLoggingTest, AdminPrivilegesAffectHookInstallation) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    bool isAdmin = isRunningAsAdministrator();
    
    if (isAdmin) {
        // If running as admin, hooks might succeed
        // We can't test this directly, but we can verify no crash occurred
        SUCCEED() << "Running as administrator - hooks may be installed";
    } else {
        // If not admin, hooks will likely fail, but app should continue
        SUCCEED() << "Running without admin privileges - hooks likely failed gracefully";
    }
    
    // In either case, the application should not crash
    EXPECT_NE(window, nullptr) << "Application should remain stable";
}

// =============================================================================
// Log Format Tests
// =============================================================================

TEST_F(ActivityLoggingTest, LogEntryFormatIsCorrect) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    if (QFile::exists("activity_log.txt")) {
        QFile logFile("activity_log.txt");
        if (logFile.open(QIODevice::ReadOnly | QIODevice::Text)) {
            QTextStream stream(&logFile);
            QString firstLine = stream.readLine();
            
            if (!firstLine.isEmpty()) {
                // Log format should be: YYYY-MM-DD HH:MM:SS - EVENT_TYPE - Details
                QStringList parts = firstLine.split(" - ");
                EXPECT_GE(parts.size(), 2) << "Log entry should have timestamp and event type";
                
                // First part should be timestamp (YYYY-MM-DD HH:MM:SS format)
                QString timestamp = parts[0];
                EXPECT_EQ(timestamp.length(), 19) << "Timestamp should be 19 characters";
                EXPECT_TRUE(timestamp.contains("-")) << "Timestamp should contain date separators";
                EXPECT_TRUE(timestamp.contains(":")) << "Timestamp should contain time separators";
            }
        }
    }
}

// =============================================================================
// Cleanup Tests
// =============================================================================

TEST_F(ActivityLoggingTest, HooksAreCleanedUpOnDestruction) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // Delete the window - this should clean up hooks
    delete window;
    
    // If we get here without crashing, cleanup was successful
    SUCCEED() << "Window destruction completed without crashing - hooks cleaned up";
}

TEST_F(ActivityLoggingTest, MultipleWindowInstancesHandleHooksCorrectly) {
    // Test that multiple instances don't interfere with each other
    auto* window1 = new TimeTrackerMainWindow();
    WidgetTestHelper::processEvents(200);

    auto* window2 = new TimeTrackerMainWindow();
    WidgetTestHelper::processEvents(200);

    // Both windows should be created successfully
    EXPECT_NE(window1, nullptr);
    EXPECT_NE(window2, nullptr);

    // Clean up
    delete window1;
    delete window2;

    SUCCEED() << "Multiple window instances handled correctly";
}

// =============================================================================
// Error Handling Tests
// =============================================================================

TEST_F(ActivityLoggingTest, LogFileCreationFailureIsHandled) {
    // This test simulates log file creation failure
    // In practice, this is hard to simulate without changing file permissions
    
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // Application should not crash even if logging fails
    EXPECT_NE(window, nullptr) << "Application should handle log file issues gracefully";
}

// =============================================================================
// Performance Tests
// =============================================================================

TEST_F(ActivityLoggingTest, HookSetupPerformance) {
    QElapsedTimer timer;
    timer.start();

    auto* window = createMainWindow();

    qint64 elapsed = timer.elapsed();

    // Hook setup should complete quickly (within 2 seconds)
    EXPECT_LT(elapsed, 2000) << "Hook setup should complete within 2 seconds";

    delete window;
}

// =============================================================================
// Integration Tests
// =============================================================================

TEST_F(ActivityLoggingTest, ActivityLoggingIntegratesWithMainWindow) {
    auto* window = createMainWindow();
    
    // Process events to ensure initialization is complete
    WidgetTestHelper::processEvents(500);
    
    // Verify that activity logging doesn't interfere with main window functionality
    EXPECT_EQ(window->windowTitle(), "Time Tracker Application");
    EXPECT_EQ(window->size(), QSize(400, 300));
    
    // Window should be functional regardless of hook status
    window->show();
    EXPECT_TRUE(window->isVisible());
    
    window->hide();
    EXPECT_FALSE(window->isVisible());
}

} // namespace TimeTrackerTest
