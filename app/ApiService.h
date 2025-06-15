#pragma once
#include <QObject>
#include <QNetworkAccessManager>
#include <QNetworkRequest>
#include <QNetworkReply>
#include <QHttpMultiPart>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QTimer>
#include <QMutex>

// Forward declaration
struct IdleAnnotationData;

class ApiService : public QObject {
    Q_OBJECT

public:
    explicit ApiService(QObject *parent = nullptr);
    ~ApiService();

public slots:
    void uploadActivityLogs();
    void uploadScreenshot(const QString& filePath, const QString& userId, const QString& sessionId);
    void uploadIdleTime(const IdleAnnotationData& data);

signals:
    void activityLogsUploaded(bool success);
    void screenshotUploaded(bool success, const QString& filePath);
    void idleTimeUploaded(bool success);

private slots:
    void handleActivityResponse();
    void handleScreenshotResponse();
    void handleIdleTimeResponse();

private:
    void setupNetworkManager();
    QJsonArray readActivityLogs();
    void clearUploadedLogs(const QJsonArray& uploadedLogs);
    
    QNetworkAccessManager *m_networkManager;
    QTimer *m_uploadTimer;
    QMutex m_uploadMutex;
    
    QString m_baseUrl;
    QStringList m_pendingUploads;
};
