#include <gtest/gtest.h>
#include <QTest>
#include <QSignalSpy>
#include <QTimer>
#include <QDateTime>
#include <QCoreApplication>
#include "IdleDetector.h"

class IdleDetectorTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Ensure Qt event loop is available for timer tests
        if (!QCoreApplication::instance()) {
            int argc = 0;
            char* argv[] = {nullptr};
            app = new QCoreApplication(argc, argv);
        }
    }

    void TearDown() override {
        // Clean up if we created the application
        if (app) {
            delete app;
            app = nullptr;
        }
    }

    QCoreApplication* app = nullptr;
};

// Test Case 1: IdleDetector should exist and be constructible
TEST_F(IdleDetectorTest, ShouldBeConstructible) {
    // This will fail until we create the IdleDetector class
    IdleDetector detector;
    EXPECT_TRUE(true); // Basic construction test
}

// Test Case 2: IdleDetector should have configurable idle threshold
TEST_F(IdleDetectorTest, ShouldHaveConfigurableIdleThreshold) {
    IdleDetector detector;
    
    // Test default threshold (should be 5 minutes = 300 seconds)
    EXPECT_EQ(detector.getIdleThresholdSeconds(), 300);
    
    // Test setting custom threshold
    detector.setIdleThresholdSeconds(120); // 2 minutes
    EXPECT_EQ(detector.getIdleThresholdSeconds(), 120);
}

// Test Case 3: IdleDetector should detect idle state after threshold
TEST_F(IdleDetectorTest, ShouldDetectIdleStateAfterThreshold) {
    IdleDetector detector;

    // Set a very short threshold for testing (100ms)
    detector.setIdleThresholdSeconds(1); // 1 second for testing

    // Initially should not be idle
    EXPECT_FALSE(detector.isIdle());

    // Start the detector
    detector.start();

    // Wait for more than the threshold
    QTest::qWait(1500); // Wait 1.5 seconds

    // Manually trigger idle check for reliable testing
    detector.triggerIdleCheck();

    // Should now be idle
    EXPECT_TRUE(detector.isIdle());

    detector.stop();
}

// Test Case 4: IdleDetector should emit signals when idle state changes
TEST_F(IdleDetectorTest, ShouldEmitSignalsOnIdleStateChange) {
    IdleDetector detector;
    detector.setIdleThresholdSeconds(1); // 1 second for testing

    // Create signal spies
    QSignalSpy idleStartedSpy(&detector, &IdleDetector::idleStarted);
    QSignalSpy idleEndedSpy(&detector, &IdleDetector::idleEnded);

    // Start the detector
    detector.start();

    // Wait for idle state
    QTest::qWait(1500);

    // Manually trigger idle check for reliable testing
    detector.triggerIdleCheck();

    // Should have emitted idleStarted signal
    EXPECT_EQ(idleStartedSpy.count(), 1);
    EXPECT_EQ(idleEndedSpy.count(), 0);

    // Simulate activity (reset activity time)
    detector.updateLastActivityTime();

    // Process events to ensure signal is emitted
    if (QCoreApplication::instance()) {
        QCoreApplication::processEvents();
    }

    // Should have emitted idleEnded signal
    EXPECT_EQ(idleEndedSpy.count(), 1);

    detector.stop();
}

// Test Case 5: IdleDetector should track last activity time
TEST_F(IdleDetectorTest, ShouldTrackLastActivityTime) {
    IdleDetector detector;
    
    QDateTime beforeUpdate = QDateTime::currentDateTime();
    
    // Update activity time
    detector.updateLastActivityTime();
    
    QDateTime afterUpdate = QDateTime::currentDateTime();
    QDateTime lastActivity = detector.getLastActivityTime();
    
    // Last activity time should be between before and after
    EXPECT_TRUE(lastActivity >= beforeUpdate);
    EXPECT_TRUE(lastActivity <= afterUpdate);
}

// Test Case 6: IdleDetector should calculate idle duration correctly
TEST_F(IdleDetectorTest, ShouldCalculateIdleDurationCorrectly) {
    IdleDetector detector;
    detector.setIdleThresholdSeconds(1); // 1 second for testing
    
    detector.start();
    
    // Wait for idle state
    QTest::qWait(2000); // Wait 2 seconds
    
    // Should be idle and duration should be approximately 1 second (threshold)
    EXPECT_TRUE(detector.isIdle());
    int duration = detector.getIdleDurationSeconds();
    EXPECT_GE(duration, 1); // At least 1 second
    EXPECT_LE(duration, 3); // But not more than 3 seconds (allowing for timing variations)
    
    detector.stop();
}

// Test Case 7: IdleDetector should handle start/stop correctly
TEST_F(IdleDetectorTest, ShouldHandleStartStopCorrectly) {
    IdleDetector detector;
    
    // Initially should not be running
    EXPECT_FALSE(detector.isRunning());
    
    // Start the detector
    detector.start();
    EXPECT_TRUE(detector.isRunning());
    
    // Stop the detector
    detector.stop();
    EXPECT_FALSE(detector.isRunning());
}

// Test Case 8: IdleDetector should reset idle state when activity is detected
TEST_F(IdleDetectorTest, ShouldResetIdleStateOnActivity) {
    IdleDetector detector;
    detector.setIdleThresholdSeconds(1); // 1 second for testing
    
    detector.start();

    // Wait for idle state
    QTest::qWait(1500);

    // Manually trigger idle check for reliable testing
    detector.triggerIdleCheck();
    EXPECT_TRUE(detector.isIdle());
    
    // Simulate activity
    detector.updateLastActivityTime();
    
    // Should no longer be idle
    EXPECT_FALSE(detector.isIdle());
    
    detector.stop();
}
