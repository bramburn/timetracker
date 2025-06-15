#pragma once

#include <QObject>
#include <QTimer>
#include <QDateTime>
#include <QMutex>

/**
 * @brief The IdleDetector class provides idle time detection functionality
 * 
 * This class monitors user activity and detects when the user has been idle
 * for a configurable amount of time. It emits signals when idle state changes.
 */
class IdleDetector : public QObject
{
    Q_OBJECT

public:
    /**
     * @brief Construct a new IdleDetector object
     * @param parent The parent QObject
     */
    explicit IdleDetector(QObject *parent = nullptr);

    /**
     * @brief Destroy the IdleDetector object
     */
    ~IdleDetector();

    /**
     * @brief Start the idle detection
     */
    void start();

    /**
     * @brief Stop the idle detection
     */
    void stop();

    /**
     * @brief Check if the detector is currently running
     * @return true if running, false otherwise
     */
    bool isRunning() const;

    /**
     * @brief Check if the user is currently idle
     * @return true if idle, false otherwise
     */
    bool isIdle() const;

    /**
     * @brief Get the idle threshold in seconds
     * @return The idle threshold in seconds
     */
    int getIdleThresholdSeconds() const;

    /**
     * @brief Set the idle threshold in seconds
     * @param seconds The idle threshold in seconds
     */
    void setIdleThresholdSeconds(int seconds);

    /**
     * @brief Get the last activity time
     * @return The last activity time
     */
    QDateTime getLastActivityTime() const;

    /**
     * @brief Get the current idle duration in seconds
     * @return The idle duration in seconds, or 0 if not idle
     */
    int getIdleDurationSeconds() const;

public slots:
    /**
     * @brief Update the last activity time to the current time
     * This should be called whenever user activity is detected
     */
    void updateLastActivityTime();

    /**
     * @brief Manually trigger idle state check (for testing purposes)
     */
    void triggerIdleCheck();

signals:
    /**
     * @brief Emitted when the user becomes idle
     * @param durationSeconds The duration of inactivity that triggered the idle state
     */
    void idleStarted(int durationSeconds);

    /**
     * @brief Emitted when the user is no longer idle
     * @param totalIdleDurationSeconds The total duration the user was idle
     */
    void idleEnded(int totalIdleDurationSeconds);

private slots:
    /**
     * @brief Check the current idle state
     * This is called periodically by the internal timer
     */
    void checkIdleState();

private:
    QTimer *m_checkTimer;           ///< Timer for periodic idle state checks
    QDateTime m_lastActivityTime;   ///< Timestamp of last user activity
    QDateTime m_idleStartTime;      ///< Timestamp when idle state started
    bool m_isCurrentlyIdle;         ///< Current idle state
    int m_idleThresholdSeconds;     ///< Idle threshold in seconds
    mutable QMutex m_mutex;         ///< Mutex for thread safety

    static const int CHECK_INTERVAL_MS = 1000; ///< Check interval in milliseconds (1 second)
};
