#include "ApiService.h"
#include <QApplication>
#include <QDebug>
#include <QFile>
#include <QTextStream>
#include <QHttpPart>
#include <QFileInfo>
#include <QStandardPaths>
#include <QSslConfiguration>
#include <QSslSocket>

ApiService::ApiService(QObject *parent) : QObject(parent) {
    setupNetworkManager();
    
    // Configure base URL (use localhost for development)
    m_baseUrl = "https://localhost:7001/api/trackingdata";
    
    // Setup periodic activity log upload (every 5 minutes)
    m_uploadTimer = new QTimer(this);
    connect(m_uploadTimer, &QTimer::timeout, this, &ApiService::uploadActivityLogs);
    m_uploadTimer->start(5 * 60 * 1000); // 5 minutes
    
    qDebug() << "ApiService initialized with base URL:" << m_baseUrl;
}

ApiService::~ApiService() {
    if (m_uploadTimer) {
        m_uploadTimer->stop();
    }
}

void ApiService::setupNetworkManager() {
    m_networkManager = new QNetworkAccessManager(this);
    
    // Configure SSL for HTTPS
    QSslConfiguration sslConfig = QSslConfiguration::defaultConfiguration();
    sslConfig.setPeerVerifyMode(QSslSocket::VerifyNone); // For development only
}

void ApiService::uploadActivityLogs() {
    QMutexLocker locker(&m_uploadMutex);
    
    QJsonArray activityLogs = readActivityLogs();
    if (activityLogs.isEmpty()) {
        qDebug() << "No activity logs to upload";
        return;
    }
    
    QJsonDocument doc(activityLogs);
    QByteArray data = doc.toJson();
    
    QNetworkRequest request(QUrl(m_baseUrl + "/activity"));
    request.setHeader(QNetworkRequest::ContentTypeHeader, "application/json");
    request.setRawHeader("User-Agent", "TimeTracker-Client/1.0");
    
    QNetworkReply *reply = m_networkManager->post(request, data);
    reply->setProperty("activityLogs", QVariant::fromValue(activityLogs));
    
    connect(reply, &QNetworkReply::finished, this, &ApiService::handleActivityResponse);
    
    qDebug() << "Uploading" << activityLogs.size() << "activity log entries";
}

void ApiService::uploadScreenshot(const QString& filePath, const QString& userId, const QString& sessionId) {
    QFile file(filePath);
    if (!file.exists()) {
        qWarning() << "Screenshot file does not exist:" << filePath;
        emit screenshotUploaded(false, filePath);
        return;
    }
    
    if (!file.open(QIODevice::ReadOnly)) {
        qWarning() << "Failed to open screenshot file:" << filePath;
        emit screenshotUploaded(false, filePath);
        return;
    }
    
    // Create multipart form data
    QHttpMultiPart *multiPart = new QHttpMultiPart(QHttpMultiPart::FormDataType);
    
    // Read file data
    QByteArray fileData = file.readAll();
    file.close();

    // Add file part
    QHttpPart filePart;
    filePart.setHeader(QNetworkRequest::ContentTypeHeader, "image/jpeg");
    filePart.setHeader(QNetworkRequest::ContentDispositionHeader,
                      QVariant("form-data; name=\"file\"; filename=\"" + QFileInfo(filePath).fileName() + "\""));
    filePart.setBody(fileData);
    multiPart->append(filePart);
    
    // Add userId part
    QHttpPart userIdPart;
    userIdPart.setHeader(QNetworkRequest::ContentDispositionHeader, QVariant("form-data; name=\"userId\""));
    userIdPart.setBody(userId.toUtf8());
    multiPart->append(userIdPart);
    
    // Add sessionId part
    QHttpPart sessionIdPart;
    sessionIdPart.setHeader(QNetworkRequest::ContentDispositionHeader, QVariant("form-data; name=\"sessionId\""));
    sessionIdPart.setBody(sessionId.toUtf8());
    multiPart->append(sessionIdPart);
    
    QNetworkRequest request(QUrl(m_baseUrl + "/screenshots"));
    request.setRawHeader("User-Agent", "TimeTracker-Client/1.0");
    
    QNetworkReply *reply = m_networkManager->post(request, multiPart);
    reply->setProperty("filePath", filePath);
    multiPart->setParent(reply);
    
    connect(reply, &QNetworkReply::finished, this, &ApiService::handleScreenshotResponse);
    
    qDebug() << "Uploading screenshot:" << filePath << "for user:" << userId;
}

QJsonArray ApiService::readActivityLogs() {
    QJsonArray logs;
    QFile file("activity_log.txt");

    if (!file.open(QIODevice::ReadOnly | QIODevice::Text)) {
        return logs;
    }

    QTextStream in(&file);
    while (!in.atEnd()) {
        QString line = in.readLine().trimmed();
        if (line.isEmpty()) continue;

        // Parse log line format: "2025-06-14 23:18:45.123 - EVENT_TYPE - DETAILS"
        QStringList parts = line.split(" - ");
        if (parts.size() >= 3) {
            QJsonObject logEntry;
            logEntry["timestamp"] = parts[0];
            logEntry["eventType"] = parts[1];
            logEntry["details"] = parts.mid(2).join(" - ");
            logEntry["userId"] = "current_user@company.com"; // Replace with actual user
            logEntry["sessionId"] = "1"; // Replace with actual session ID

            logs.append(logEntry);
        }
    }

    return logs;
}

void ApiService::handleActivityResponse() {
    QNetworkReply *reply = qobject_cast<QNetworkReply*>(sender());
    if (!reply) return;

    QJsonArray uploadedLogs = reply->property("activityLogs").value<QJsonArray>();

    if (reply->error() == QNetworkReply::NoError) {
        qDebug() << "Activity logs uploaded successfully";
        clearUploadedLogs(uploadedLogs);
        emit activityLogsUploaded(true);
    } else {
        qWarning() << "Failed to upload activity logs:" << reply->errorString();
        emit activityLogsUploaded(false);
    }

    reply->deleteLater();
}

void ApiService::handleScreenshotResponse() {
    QNetworkReply *reply = qobject_cast<QNetworkReply*>(sender());
    if (!reply) return;

    QString filePath = reply->property("filePath").toString();

    if (reply->error() == QNetworkReply::NoError) {
        qDebug() << "Screenshot uploaded successfully:" << filePath;

        // Delete local file after successful upload
        QFile::remove(filePath);
        emit screenshotUploaded(true, filePath);
    } else {
        qWarning() << "Failed to upload screenshot:" << reply->errorString();
        emit screenshotUploaded(false, filePath);
    }

    reply->deleteLater();
}

void ApiService::clearUploadedLogs(const QJsonArray& uploadedLogs) {
    // Clear the activity log file after successful upload
    QFile file("activity_log.txt");
    if (file.open(QIODevice::WriteOnly | QIODevice::Truncate)) {
        file.close();
        qDebug() << "Activity log file cleared after successful upload";
    }
}
