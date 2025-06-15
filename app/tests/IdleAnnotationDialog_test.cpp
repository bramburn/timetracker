#include <gtest/gtest.h>
#include <QTest>
#include <QSignalSpy>
#include <QComboBox>
#include <QTextEdit>
#include <QPushButton>
#include <QApplication>
#include <QDateTime>
#include "IdleAnnotationDialog.h"

class IdleAnnotationDialogTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Ensure Qt application is available for widget tests
        if (!QApplication::instance()) {
            int argc = 0;
            char* argv[] = {nullptr};
            app = new QApplication(argc, argv);
        }
    }

    void TearDown() override {
        // Clean up if we created the application
        if (app) {
            delete app;
            app = nullptr;
        }
    }

    QApplication* app = nullptr;
};

// Test Case 1: IdleAnnotationDialog should exist and be constructible
TEST_F(IdleAnnotationDialogTest, ShouldBeConstructible) {
    // This will fail until we create the IdleAnnotationDialog class
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300); // 5 minutes ago
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    EXPECT_TRUE(true); // Basic construction test
}

// Test Case 2: Dialog should display idle duration correctly
TEST_F(IdleAnnotationDialogTest, ShouldDisplayIdleDurationCorrectly) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300); // 5 minutes ago
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Should display duration in a readable format
    QString durationText = dialog.getDurationText();
    EXPECT_TRUE(durationText.contains("5"));
    EXPECT_TRUE(durationText.contains("minute") || durationText.contains("min"));
}

// Test Case 3: Dialog should have predefined reason options
TEST_F(IdleAnnotationDialogTest, ShouldHavePredefinedReasonOptions) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Should have a reason combo box with predefined options
    QComboBox* reasonCombo = dialog.getReasonComboBox();
    EXPECT_NE(reasonCombo, nullptr);
    
    // Should have common idle reasons
    QStringList expectedReasons = {"Meeting", "Break", "Lunch", "Phone Call", "Away from Desk", "Other"};
    
    for (const QString& reason : expectedReasons) {
        bool found = false;
        for (int i = 0; i < reasonCombo->count(); ++i) {
            if (reasonCombo->itemText(i) == reason) {
                found = true;
                break;
            }
        }
        EXPECT_TRUE(found) << "Reason '" << reason.toStdString() << "' not found in combo box";
    }
}

// Test Case 4: Dialog should have note text field
TEST_F(IdleAnnotationDialogTest, ShouldHaveNoteTextField) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Should have a note text edit field
    QTextEdit* noteEdit = dialog.getNoteTextEdit();
    EXPECT_NE(noteEdit, nullptr);
    
    // Should be able to set and get note text
    QString testNote = "This is a test note for the idle session.";
    noteEdit->setPlainText(testNote);
    EXPECT_EQ(noteEdit->toPlainText(), testNote);
}

// Test Case 5: Dialog should emit signal when submitted
TEST_F(IdleAnnotationDialogTest, ShouldEmitSignalWhenSubmitted) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Create signal spy
    QSignalSpy submittedSpy(&dialog, &IdleAnnotationDialog::annotationSubmitted);
    
    // Set some values
    dialog.getReasonComboBox()->setCurrentText("Meeting");
    dialog.getNoteTextEdit()->setPlainText("Team standup meeting");
    
    // Submit the dialog
    dialog.submitAnnotation();
    
    // Should have emitted the signal
    EXPECT_EQ(submittedSpy.count(), 1);
    
    // Check signal parameters
    QList<QVariant> arguments = submittedSpy.takeFirst();
    EXPECT_EQ(arguments.at(0).toString(), "Meeting");
    EXPECT_EQ(arguments.at(1).toString(), "Team standup meeting");
}

// Test Case 6: Dialog should have OK and Cancel buttons
TEST_F(IdleAnnotationDialogTest, ShouldHaveOkAndCancelButtons) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Should have OK and Cancel buttons
    QPushButton* okButton = dialog.getOkButton();
    QPushButton* cancelButton = dialog.getCancelButton();
    
    EXPECT_NE(okButton, nullptr);
    EXPECT_NE(cancelButton, nullptr);
    
    EXPECT_EQ(okButton->text(), "OK");
    EXPECT_EQ(cancelButton->text(), "Cancel");
}

// Test Case 7: Dialog should validate input
TEST_F(IdleAnnotationDialogTest, ShouldValidateInput) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Should be valid with reason selected
    dialog.getReasonComboBox()->setCurrentText("Meeting");
    EXPECT_TRUE(dialog.isValid());
    
    // Should be invalid with empty reason
    dialog.getReasonComboBox()->setCurrentText("");
    EXPECT_FALSE(dialog.isValid());
}

// Test Case 8: Dialog should handle different time formats
TEST_F(IdleAnnotationDialogTest, ShouldHandleDifferentTimeFormats) {
    // Test with seconds
    QDateTime startTime1 = QDateTime::currentDateTime().addSecs(-45);
    QDateTime endTime1 = QDateTime::currentDateTime();
    IdleAnnotationDialog dialog1(startTime1, endTime1);
    QString duration1 = dialog1.getDurationText();
    EXPECT_TRUE(duration1.contains("45") || duration1.contains("second"));
    
    // Test with hours
    QDateTime startTime2 = QDateTime::currentDateTime().addSecs(-3661); // 1 hour 1 minute 1 second
    QDateTime endTime2 = QDateTime::currentDateTime();
    IdleAnnotationDialog dialog2(startTime2, endTime2);
    QString duration2 = dialog2.getDurationText();
    EXPECT_TRUE(duration2.contains("1") && (duration2.contains("hour") || duration2.contains("hr")));
}

// Test Case 9: Dialog should return annotation data
TEST_F(IdleAnnotationDialogTest, ShouldReturnAnnotationData) {
    QDateTime startTime = QDateTime::currentDateTime().addSecs(-300);
    QDateTime endTime = QDateTime::currentDateTime();
    
    IdleAnnotationDialog dialog(startTime, endTime);
    
    // Set values
    dialog.getReasonComboBox()->setCurrentText("Break");
    dialog.getNoteTextEdit()->setPlainText("Coffee break");
    
    // Get annotation data
    auto annotationData = dialog.getAnnotationData();
    
    EXPECT_EQ(annotationData.reason, "Break");
    EXPECT_EQ(annotationData.note, "Coffee break");
    EXPECT_EQ(annotationData.startTime, startTime);
    EXPECT_EQ(annotationData.endTime, endTime);
    EXPECT_GT(annotationData.durationSeconds, 0);
}
