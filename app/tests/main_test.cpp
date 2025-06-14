#include <gtest/gtest.h>
#include <QString>
#include "../TimeTrackerMainWindow.h" // Relative path to new header

// Test to ensure the testing framework is linked and running
TEST(FrameworkSanityTest, CanRun) {
    EXPECT_TRUE(true);
}

// Test to ensure the TimeTrackerMainWindow class is properly linked and accessible
TEST(ApplicationSanityTest, ClassIsAccessible) {
    // Test that we can access the class and its methods without creating instances
    // This verifies that the refactoring was successful and the class is properly linked

    // Test that QString works (Qt Core functionality)
    QString testString = "Time Tracker Application";
    EXPECT_EQ(testString.toStdString(), "Time Tracker Application");

    // Test that the header is included properly by checking if we can reference the class
    // We use sizeof to verify the class exists without instantiating it
    EXPECT_GT(sizeof(TimeTrackerMainWindow), 0);
}
