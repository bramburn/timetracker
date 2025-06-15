#pragma once

#include <QDialog>
#include <QDateTime>
#include <QString>

class QComboBox;
class QTextEdit;
class QPushButton;
class QLabel;
class QVBoxLayout;
class QHBoxLayout;
class QFormLayout;

/**
 * @brief Data structure for idle session annotation
 */
struct IdleAnnotationData {
    QString reason;
    QString note;
    QDateTime startTime;
    QDateTime endTime;
    int durationSeconds;
};

/**
 * @brief The IdleAnnotationDialog class provides a dialog for users to annotate idle time
 * 
 * This dialog allows users to specify the reason for their idle time and add optional notes.
 * It displays the idle duration and provides predefined reason options.
 */
class IdleAnnotationDialog : public QDialog
{
    Q_OBJECT

public:
    /**
     * @brief Construct a new IdleAnnotationDialog object
     * @param startTime The start time of the idle period
     * @param endTime The end time of the idle period
     * @param parent The parent widget
     */
    explicit IdleAnnotationDialog(const QDateTime& startTime, const QDateTime& endTime, QWidget *parent = nullptr);

    /**
     * @brief Destroy the IdleAnnotationDialog object
     */
    ~IdleAnnotationDialog();

    /**
     * @brief Get the formatted duration text
     * @return A human-readable duration string
     */
    QString getDurationText() const;

    /**
     * @brief Get the reason combo box widget
     * @return Pointer to the reason combo box
     */
    QComboBox* getReasonComboBox() const;

    /**
     * @brief Get the note text edit widget
     * @return Pointer to the note text edit
     */
    QTextEdit* getNoteTextEdit() const;

    /**
     * @brief Get the OK button widget
     * @return Pointer to the OK button
     */
    QPushButton* getOkButton() const;

    /**
     * @brief Get the Cancel button widget
     * @return Pointer to the Cancel button
     */
    QPushButton* getCancelButton() const;

    /**
     * @brief Check if the current input is valid
     * @return true if valid, false otherwise
     */
    bool isValid() const;

    /**
     * @brief Get the annotation data from the dialog
     * @return IdleAnnotationData structure with all the information
     */
    IdleAnnotationData getAnnotationData() const;

public slots:
    /**
     * @brief Submit the annotation (called when OK is clicked)
     */
    void submitAnnotation();

signals:
    /**
     * @brief Emitted when the annotation is submitted
     * @param reason The selected reason
     * @param note The entered note
     */
    void annotationSubmitted(const QString& reason, const QString& note);

private slots:
    /**
     * @brief Handle OK button click
     */
    void onOkClicked();

    /**
     * @brief Handle Cancel button click
     */
    void onCancelClicked();

    /**
     * @brief Handle reason selection change
     */
    void onReasonChanged();

private:
    /**
     * @brief Set up the user interface
     */
    void setupUI();

    /**
     * @brief Format duration into human-readable text
     * @param seconds Duration in seconds
     * @return Formatted duration string
     */
    QString formatDuration(int seconds) const;

    /**
     * @brief Populate the reason combo box with predefined options
     */
    void populateReasonComboBox();

    // UI Components
    QLabel* m_durationLabel;
    QComboBox* m_reasonComboBox;
    QTextEdit* m_noteTextEdit;
    QPushButton* m_okButton;
    QPushButton* m_cancelButton;

    // Layouts
    QVBoxLayout* m_mainLayout;
    QFormLayout* m_formLayout;
    QHBoxLayout* m_buttonLayout;

    // Data
    QDateTime m_startTime;
    QDateTime m_endTime;
    int m_durationSeconds;
};
