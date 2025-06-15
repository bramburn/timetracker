#include "IdleDetector.h"
#include <QDebug>
#include <QMutexLocker>

IdleDetector::IdleDetector(QObject *parent)
    : QObject(parent)
    , m_checkTimer(new QTimer(this))
    , m_lastActivityTime(QDateTime::currentDateTime())
    , m_isCurrentlyIdle(false)
    , m_idleThresholdSeconds(300) // Default: 5 minutes
{
    // Configure the check timer
    m_checkTimer->setInterval(CHECK_INTERVAL_MS);
    connect(m_checkTimer, &QTimer::timeout, this, &IdleDetector::checkIdleState);
    
    qDebug() << "IdleDetector created with threshold:" << m_idleThresholdSeconds << "seconds";
}

IdleDetector::~IdleDetector()
{
    stop();
    qDebug() << "IdleDetector destroyed";
}

void IdleDetector::start()
{
    QMutexLocker locker(&m_mutex);
    
    if (!m_checkTimer->isActive()) {
        m_lastActivityTime = QDateTime::currentDateTime();
        m_isCurrentlyIdle = false;
        m_checkTimer->start();
        
        qDebug() << "IdleDetector started with" << CHECK_INTERVAL_MS << "ms check interval";
    }
}

void IdleDetector::stop()
{
    QMutexLocker locker(&m_mutex);
    
    if (m_checkTimer->isActive()) {
        m_checkTimer->stop();
        
        // If we were idle when stopping, emit idle ended signal
        if (m_isCurrentlyIdle) {
            int totalIdleDuration = m_idleStartTime.secsTo(QDateTime::currentDateTime());
            m_isCurrentlyIdle = false;
            
            // Emit signal outside of mutex lock
            locker.unlock();
            emit idleEnded(totalIdleDuration);
            locker.relock();
        }
        
        qDebug() << "IdleDetector stopped";
    }
}

bool IdleDetector::isRunning() const
{
    QMutexLocker locker(&m_mutex);
    return m_checkTimer->isActive();
}

bool IdleDetector::isIdle() const
{
    QMutexLocker locker(&m_mutex);
    return m_isCurrentlyIdle;
}

int IdleDetector::getIdleThresholdSeconds() const
{
    QMutexLocker locker(&m_mutex);
    return m_idleThresholdSeconds;
}

void IdleDetector::setIdleThresholdSeconds(int seconds)
{
    QMutexLocker locker(&m_mutex);
    
    if (seconds > 0) {
        m_idleThresholdSeconds = seconds;
        qDebug() << "IdleDetector threshold set to:" << seconds << "seconds";
    } else {
        qWarning() << "Invalid idle threshold:" << seconds << "- must be positive";
    }
}

QDateTime IdleDetector::getLastActivityTime() const
{
    QMutexLocker locker(&m_mutex);
    return m_lastActivityTime;
}

int IdleDetector::getIdleDurationSeconds() const
{
    QMutexLocker locker(&m_mutex);
    
    if (!m_isCurrentlyIdle) {
        return 0;
    }
    
    return m_idleStartTime.secsTo(QDateTime::currentDateTime());
}

void IdleDetector::updateLastActivityTime()
{
    QMutexLocker locker(&m_mutex);
    
    QDateTime now = QDateTime::currentDateTime();
    bool wasIdle = m_isCurrentlyIdle;
    int totalIdleDuration = 0;
    
    if (wasIdle) {
        totalIdleDuration = m_idleStartTime.secsTo(now);
        m_isCurrentlyIdle = false;
        
        qDebug() << "Activity detected - ending idle state after" << totalIdleDuration << "seconds";
    }
    
    m_lastActivityTime = now;
    
    // Emit signal outside of mutex lock
    if (wasIdle) {
        locker.unlock();
        emit idleEnded(totalIdleDuration);
    }
}

void IdleDetector::triggerIdleCheck()
{
    checkIdleState();
}

void IdleDetector::checkIdleState()
{
    QMutexLocker locker(&m_mutex);
    
    QDateTime now = QDateTime::currentDateTime();
    int secondsSinceLastActivity = m_lastActivityTime.secsTo(now);
    
    // Check if we should enter idle state
    if (!m_isCurrentlyIdle && secondsSinceLastActivity >= m_idleThresholdSeconds) {
        m_isCurrentlyIdle = true;
        m_idleStartTime = m_lastActivityTime.addSecs(m_idleThresholdSeconds);
        
        qDebug() << "User entered idle state after" << m_idleThresholdSeconds << "seconds of inactivity";
        
        // Emit signal outside of mutex lock
        locker.unlock();
        emit idleStarted(m_idleThresholdSeconds);
    }
}
