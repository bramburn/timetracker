#include "IdleAnnotationDialog.h"
#include <QComboBox>
#include <QTextEdit>
#include <QPushButton>
#include <QLabel>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QFormLayout>
#include <QMessageBox>
#include <QDebug>

IdleAnnotationDialog::IdleAnnotationDialog(const QDateTime& startTime, const QDateTime& endTime, QWidget *parent)
    : QDialog(parent)
    , m_durationLabel(nullptr)
    , m_reasonComboBox(nullptr)
    , m_noteTextEdit(nullptr)
    , m_okButton(nullptr)
    , m_cancelButton(nullptr)
    , m_mainLayout(nullptr)
    , m_formLayout(nullptr)
    , m_buttonLayout(nullptr)
    , m_startTime(startTime)
    , m_endTime(endTime)
    , m_durationSeconds(static_cast<int>(startTime.secsTo(endTime)))
{
    setWindowTitle("Idle Time Annotation");
    setModal(true);
    setFixedSize(400, 300);
    
    setupUI();
    
    qDebug() << "IdleAnnotationDialog created for duration:" << m_durationSeconds << "seconds";
}

IdleAnnotationDialog::~IdleAnnotationDialog()
{
    qDebug() << "IdleAnnotationDialog destroyed";
}

void IdleAnnotationDialog::setupUI()
{
    // Create main layout
    m_mainLayout = new QVBoxLayout(this);
    
    // Create duration label
    m_durationLabel = new QLabel(this);
    m_durationLabel->setText(QString("You were idle for: %1").arg(getDurationText()));
    m_durationLabel->setStyleSheet("font-weight: bold; color: #2c3e50; margin-bottom: 10px;");
    m_mainLayout->addWidget(m_durationLabel);
    
    // Create form layout
    m_formLayout = new QFormLayout();
    
    // Create reason combo box
    m_reasonComboBox = new QComboBox(this);
    populateReasonComboBox();
    connect(m_reasonComboBox, QOverload<int>::of(&QComboBox::currentIndexChanged),
            this, &IdleAnnotationDialog::onReasonChanged);
    m_formLayout->addRow("Reason:", m_reasonComboBox);
    
    // Create note text edit
    m_noteTextEdit = new QTextEdit(this);
    m_noteTextEdit->setPlaceholderText("Optional: Add additional details about this idle period...");
    m_noteTextEdit->setMaximumHeight(80);
    m_formLayout->addRow("Note:", m_noteTextEdit);
    
    m_mainLayout->addLayout(m_formLayout);
    
    // Add spacer
    m_mainLayout->addStretch();
    
    // Create button layout
    m_buttonLayout = new QHBoxLayout();
    
    // Create buttons
    m_cancelButton = new QPushButton("Cancel", this);
    m_okButton = new QPushButton("OK", this);
    m_okButton->setDefault(true);
    
    // Connect button signals
    connect(m_okButton, &QPushButton::clicked, this, &IdleAnnotationDialog::onOkClicked);
    connect(m_cancelButton, &QPushButton::clicked, this, &IdleAnnotationDialog::onCancelClicked);
    
    // Add buttons to layout
    m_buttonLayout->addStretch();
    m_buttonLayout->addWidget(m_cancelButton);
    m_buttonLayout->addWidget(m_okButton);
    
    m_mainLayout->addLayout(m_buttonLayout);
    
    // Set initial focus
    m_reasonComboBox->setFocus();
}

void IdleAnnotationDialog::populateReasonComboBox()
{
    QStringList reasons = {
        "",  // Empty option for validation
        "Meeting",
        "Break",
        "Lunch",
        "Phone Call",
        "Away from Desk",
        "Other"
    };
    
    m_reasonComboBox->addItems(reasons);
}

QString IdleAnnotationDialog::getDurationText() const
{
    return formatDuration(m_durationSeconds);
}

QString IdleAnnotationDialog::formatDuration(int seconds) const
{
    if (seconds < 60) {
        return QString("%1 second%2").arg(seconds).arg(seconds == 1 ? "" : "s");
    } else if (seconds < 3600) {
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        if (remainingSeconds == 0) {
            return QString("%1 minute%2").arg(minutes).arg(minutes == 1 ? "" : "s");
        } else {
            return QString("%1 minute%2 %3 second%4")
                .arg(minutes).arg(minutes == 1 ? "" : "s")
                .arg(remainingSeconds).arg(remainingSeconds == 1 ? "" : "s");
        }
    } else {
        int hours = seconds / 3600;
        int remainingMinutes = (seconds % 3600) / 60;
        if (remainingMinutes == 0) {
            return QString("%1 hour%2").arg(hours).arg(hours == 1 ? "" : "s");
        } else {
            return QString("%1 hour%2 %3 minute%4")
                .arg(hours).arg(hours == 1 ? "" : "s")
                .arg(remainingMinutes).arg(remainingMinutes == 1 ? "" : "s");
        }
    }
}

QComboBox* IdleAnnotationDialog::getReasonComboBox() const
{
    return m_reasonComboBox;
}

QTextEdit* IdleAnnotationDialog::getNoteTextEdit() const
{
    return m_noteTextEdit;
}

QPushButton* IdleAnnotationDialog::getOkButton() const
{
    return m_okButton;
}

QPushButton* IdleAnnotationDialog::getCancelButton() const
{
    return m_cancelButton;
}

bool IdleAnnotationDialog::isValid() const
{
    return !m_reasonComboBox->currentText().isEmpty();
}

IdleAnnotationData IdleAnnotationDialog::getAnnotationData() const
{
    IdleAnnotationData data;
    data.reason = m_reasonComboBox->currentText();
    data.note = m_noteTextEdit->toPlainText();
    data.startTime = m_startTime;
    data.endTime = m_endTime;
    data.durationSeconds = m_durationSeconds;
    return data;
}

void IdleAnnotationDialog::submitAnnotation()
{
    if (isValid()) {
        QString reason = m_reasonComboBox->currentText();
        QString note = m_noteTextEdit->toPlainText();
        
        qDebug() << "Submitting annotation - Reason:" << reason << "Note:" << note;
        
        emit annotationSubmitted(reason, note);
        accept();
    } else {
        QMessageBox::warning(this, "Invalid Input", "Please select a reason for the idle time.");
    }
}

void IdleAnnotationDialog::onOkClicked()
{
    submitAnnotation();
}

void IdleAnnotationDialog::onCancelClicked()
{
    qDebug() << "Annotation cancelled";
    reject();
}

void IdleAnnotationDialog::onReasonChanged()
{
    // Enable/disable OK button based on validation
    m_okButton->setEnabled(isValid());
}
