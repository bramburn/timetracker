#pragma once

#include <gtest/gtest.h>
#include <QApplication>
#include <QWidget>
#include <QTimer>
#include <QEventLoop>
#include <QTest>
#include <memory>

/**
 * @file test_utils.h
 * @brief Shared testing utilities and mock objects for TimeTracker tests
 * 
 * This file provides common testing infrastructure including:
 * - Qt application setup for headless testing
 * - Mock objects for Qt dependencies
 * - Helper functions for test data management
 * - Common test fixtures
 */

namespace TimeTrackerTest {

/**
 * @brief Test fixture for Qt-based tests that require QApplication
 * 
 * This fixture ensures that QApplication is properly initialized
 * for tests that need Qt's event system or widgets.
 */
class QtTestFixture : public ::testing::Test {
protected:
    void SetUp() override {
        // Ensure QApplication exists for Qt-based tests
        if (!QApplication::instance()) {
            static int argc = 1;
            static char* argv[] = {const_cast<char*>("TimeTrackerTests")};
            app_ = std::make_unique<QApplication>(argc, argv);
            
            // Set platform to offscreen for headless testing
            app_->setAttribute(Qt::AA_Use96Dpi, true);
        }
    }

    void TearDown() override {
        // Clean up any remaining widgets
        if (app_) {
            app_->processEvents();
        }
    }

private:
    std::unique_ptr<QApplication> app_;
};

/**
 * @brief Helper class for testing Qt widgets without showing them
 */
class WidgetTestHelper {
public:
    /**
     * @brief Process Qt events for a specified duration
     * @param timeoutMs Maximum time to process events (default: 100ms)
     */
    static void processEvents(int timeoutMs = 100) {
        QEventLoop loop;
        QTimer::singleShot(timeoutMs, &loop, &QEventLoop::quit);
        loop.exec();
    }

    /**
     * @brief Wait for a condition to become true
     * @param condition Function that returns true when condition is met
     * @param timeoutMs Maximum time to wait (default: 5000ms)
     * @return true if condition was met, false if timeout occurred
     */
    template<typename Condition>
    static bool waitFor(Condition condition, int timeoutMs = 5000) {
        QElapsedTimer timer;
        timer.start();
        
        while (!condition() && timer.elapsed() < timeoutMs) {
            QApplication::processEvents();
            QTest::qWait(10);
        }
        
        return condition();
    }
};

/**
 * @brief Mock object for testing file operations
 */
class MockFileSystem {
public:
    MockFileSystem() = default;
    virtual ~MockFileSystem() = default;

    // Mock methods for file operations
    virtual bool fileExists(const QString& path) const { return mock_file_exists_; }
    virtual bool createDirectory(const QString& path) const { return mock_create_dir_success_; }
    virtual bool writeFile(const QString& path, const QByteArray& data) const { return mock_write_success_; }

    // Setters for controlling mock behavior
    void setFileExists(bool exists) { mock_file_exists_ = exists; }
    void setCreateDirectorySuccess(bool success) { mock_create_dir_success_ = success; }
    void setWriteFileSuccess(bool success) { mock_write_success_ = success; }

private:
    bool mock_file_exists_ = true;
    bool mock_create_dir_success_ = true;
    bool mock_write_success_ = true;
};

/**
 * @brief Test data generator for common test scenarios
 */
class TestDataGenerator {
public:
    /**
     * @brief Generate a test screenshot file path
     */
    static QString generateTestScreenshotPath() {
        return QString("test_screenshots/test_%1.png").arg(QDateTime::currentMSecsSinceEpoch());
    }

    /**
     * @brief Generate test activity log data
     */
    static QString generateTestActivityLog() {
        return QString("Test activity at %1").arg(QDateTime::currentDateTime().toString());
    }

    /**
     * @brief Create a temporary directory for testing
     */
    static QString createTempTestDirectory() {
        QString tempDir = QDir::tempPath() + "/timetracker_tests";
        QDir().mkpath(tempDir);
        return tempDir;
    }
};

/**
 * @brief RAII helper for temporary test directories
 */
class TempTestDirectory {
public:
    TempTestDirectory() {
        path_ = TestDataGenerator::createTempTestDirectory() + "/" + 
                QString::number(QDateTime::currentMSecsSinceEpoch());
        QDir().mkpath(path_);
    }

    ~TempTestDirectory() {
        // Clean up test directory
        QDir dir(path_);
        if (dir.exists()) {
            dir.removeRecursively();
        }
    }

    QString path() const { return path_; }

private:
    QString path_;
};

} // namespace TimeTrackerTest

/**
 * @brief Macro for creating Qt-based test cases
 */
#define QT_TEST_F(test_fixture, test_name) \
    TEST_F(test_fixture, test_name)

/**
 * @brief Macro for testing Qt signals and slots
 */
#define EXPECT_SIGNAL_EMITTED(object, signal) \
    do { \
        QSignalSpy spy(object, signal); \
        /* Test code here */ \
        EXPECT_GT(spy.count(), 0) << "Signal " #signal " was not emitted"; \
    } while(0)
