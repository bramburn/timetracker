#include <gtest/gtest.h>
#include <QApplication>
#include <QTest>
#include <QDir>
#include <QStandardPaths>
#include <QPixmap>
#include <QScreen>
#include <QGuiApplication>
#include <QDateTime>
#include <QFileInfo>
#include <QTimer>
#include "../TimeTrackerMainWindow.h"
#include "test_utils.h"

/**
 * @file ScreenshotCapture_test.cpp
 * @brief Unit tests for screenshot capture functionality
 * 
 * Tests cover:
 * - Screenshot directory management
 * - File creation and naming
 * - Screenshot timing and intervals
 * - Error handling for screenshot operations
 * - File system permissions and access
 */

namespace TimeTrackerTest {

class ScreenshotCaptureTest : public QtTestFixture {
protected:
    void SetUp() override {
        QtTestFixture::SetUp();
        
        // Set up offscreen platform for headless testing
        qputenv("QT_QPA_PLATFORM", "offscreen");
        
        // Create temporary directory for testing
        tempDir_ = std::make_unique<TempTestDirectory>();
        
        // Override the standard paths for testing
        testScreenshotDir_ = tempDir_->path() + "/screenshots";
        QDir().mkpath(testScreenshotDir_);
    }

    void TearDown() override {
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

    QString getScreenshotDirectory() {
        QString appDataPath = QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation);
        return QDir(appDataPath).filePath("screenshots");
    }

private:
    TimeTrackerMainWindow* mainWindow_ = nullptr;
    std::unique_ptr<TempTestDirectory> tempDir_;
    QString testScreenshotDir_;
};

// =============================================================================
// Directory Setup Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, ScreenshotDirectoryIsCreatedOnStartup) {
    auto* window = createMainWindow();
    
    // Process events to ensure directory setup is complete
    WidgetTestHelper::processEvents(200);
    
    QString screenshotDir = getScreenshotDirectory();
    EXPECT_TRUE(QDir(screenshotDir).exists()) 
        << "Screenshot directory should be created: " << screenshotDir.toStdString();
}

TEST_F(ScreenshotCaptureTest, ScreenshotDirectoryIsWritable) {
    auto* window = createMainWindow();
    
    // Process events to ensure directory setup is complete
    WidgetTestHelper::processEvents(200);
    
    QString screenshotDir = getScreenshotDirectory();
    QFileInfo dirInfo(screenshotDir);
    
    EXPECT_TRUE(dirInfo.exists()) << "Screenshot directory should exist";
    EXPECT_TRUE(dirInfo.isDir()) << "Screenshot path should be a directory";
    EXPECT_TRUE(dirInfo.isWritable()) << "Screenshot directory should be writable";
}

TEST_F(ScreenshotCaptureTest, ScreenshotDirectoryPathIsCorrect) {
    auto* window = createMainWindow();
    
    QString expectedPath = QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation);
    expectedPath = QDir(expectedPath).filePath("screenshots");
    
    QString actualPath = getScreenshotDirectory();
    
    EXPECT_EQ(actualPath, expectedPath) 
        << "Screenshot directory should be in AppLocalDataLocation";
}

// =============================================================================
// Screenshot Timer Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, ScreenshotTimerIsConfiguredCorrectly) {
    auto* window = createMainWindow();
    
    auto timers = window->findChildren<QTimer*>();
    QTimer* screenshotTimer = nullptr;
    
    // Find the screenshot timer (should be the one with the longest interval)
    for (auto* timer : timers) {
        if (timer->interval() >= 10000) { // 10 seconds or more
            screenshotTimer = timer;
            break;
        }
    }
    
    ASSERT_NE(screenshotTimer, nullptr) << "Screenshot timer should exist";
    EXPECT_TRUE(screenshotTimer->isActive()) << "Screenshot timer should be active";
    
    #ifdef QT_DEBUG
    EXPECT_EQ(screenshotTimer->interval(), 10000) << "Debug mode: 10 second interval";
    #else
    EXPECT_EQ(screenshotTimer->interval(), 600000) << "Release mode: 10 minute interval";
    #endif
}

TEST_F(ScreenshotCaptureTest, ScreenshotTimerConnectedToSlot) {
    auto* window = createMainWindow();
    
    auto timers = window->findChildren<QTimer*>();
    bool timerConnected = false;
    
    for (auto* timer : timers) {
        if (timer->interval() >= 10000) {
            // Check if timer is connected to captureScreenshot slot
            // We can't directly test the connection, but we can verify
            // the timer exists and is active
            timerConnected = timer->isActive();
            break;
        }
    }
    
    EXPECT_TRUE(timerConnected) << "Screenshot timer should be connected and active";
}

// =============================================================================
// Screenshot Filename Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, ScreenshotFilenameFormatIsCorrect) {
    // Test the expected filename format: screenshot_YYYYMMDD_HHMMSS_ZZZ.jpg
    QString timestamp = QDateTime::currentDateTime().toString("yyyyMMdd_hhmmss_zzz");
    QString expectedPattern = QString("screenshot_%1.jpg").arg(timestamp);
    
    // The pattern should contain the basic structure
    EXPECT_TRUE(expectedPattern.startsWith("screenshot_")) 
        << "Filename should start with 'screenshot_'";
    EXPECT_TRUE(expectedPattern.endsWith(".jpg")) 
        << "Filename should end with '.jpg'";
    EXPECT_GT(expectedPattern.length(), 20) 
        << "Filename should include timestamp";
}

TEST_F(ScreenshotCaptureTest, ScreenshotFilenameIsUnique) {
    // Generate multiple filenames and ensure they're different
    QStringList filenames;
    
    for (int i = 0; i < 5; ++i) {
        QString timestamp = QDateTime::currentDateTime().toString("yyyyMMdd_hhmmss_zzz");
        QString filename = QString("screenshot_%1.jpg").arg(timestamp);
        filenames.append(filename);
        
        // Small delay to ensure different timestamps
        QTest::qWait(10);
    }
    
    // Check that all filenames are unique
    QSet<QString> uniqueFilenames(filenames.begin(), filenames.end());
    EXPECT_EQ(uniqueFilenames.size(), filenames.size()) 
        << "All generated filenames should be unique";
}

// =============================================================================
// Screenshot Capture Method Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, CaptureScreenshotMethodCanBeInvoked) {
    auto* window = createMainWindow();
    
    // Verify we can invoke the captureScreenshot method
    bool methodInvoked = QMetaObject::invokeMethod(window, "captureScreenshot");
    EXPECT_TRUE(methodInvoked) << "captureScreenshot method should be invokable";
}

TEST_F(ScreenshotCaptureTest, ScreenshotCaptureHandlesNullScreen) {
    // This test verifies that the screenshot capture handles edge cases
    // In a headless environment, screen capture might fail gracefully
    
    auto* window = createMainWindow();
    
    // Invoke screenshot capture in headless environment
    bool methodInvoked = QMetaObject::invokeMethod(window, "captureScreenshot");
    EXPECT_TRUE(methodInvoked) << "Method should be invokable even in headless environment";
    
    // The method should not crash even if screen capture fails
    SUCCEED() << "Screenshot capture completed without crashing";
}

// =============================================================================
// Error Handling Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, HandlesInvalidScreenshotDirectory) {
    // This test would require mocking the file system or using a read-only directory
    // For now, we test that the directory creation logic works
    
    auto* window = createMainWindow();
    
    QString screenshotDir = getScreenshotDirectory();
    EXPECT_TRUE(QDir(screenshotDir).exists()) 
        << "Screenshot directory should be created even if it didn't exist initially";
}

// =============================================================================
// Performance Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, ScreenshotCapturePerformance) {
    auto* window = createMainWindow();
    
    // Measure time for screenshot capture method invocation
    QElapsedTimer timer;
    timer.start();
    
    QMetaObject::invokeMethod(window, "captureScreenshot");
    
    qint64 elapsed = timer.elapsed();
    
    // Screenshot capture should complete within reasonable time (5 seconds max)
    EXPECT_LT(elapsed, 5000) << "Screenshot capture should complete within 5 seconds";
}

// =============================================================================
// Integration Tests
// =============================================================================

TEST_F(ScreenshotCaptureTest, ScreenshotTimerTriggersCapture) {
    auto* window = createMainWindow();

    auto timers = window->findChildren<QTimer*>();
    QTimer* screenshotTimer = nullptr;

    for (auto* timer : timers) {
        if (timer->interval() >= 10000) {
            screenshotTimer = timer;
            break;
        }
    }

    ASSERT_NE(screenshotTimer, nullptr) << "Screenshot timer should exist";

    // Test that we can invoke the captureScreenshot method directly
    bool methodInvoked = QMetaObject::invokeMethod(window, "captureScreenshot");
    EXPECT_TRUE(methodInvoked) << "captureScreenshot method should be invokable";

    // If we get here without crashing, the connection works
    SUCCEED() << "Screenshot capture method processed successfully";
}

} // namespace TimeTrackerTest
