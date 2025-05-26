using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Monitors Windows user session changes (lock/unlock, logon/logoff) and notifies
/// the ActivityLogger to pause/resume tracking accordingly.
/// </summary>
public class SessionMonitor : IDisposable
{
    private readonly ILogger<SessionMonitor> _logger;
    private readonly ActivityLogger _activityLogger;
    private readonly SessionNotificationWindow _notificationWindow;
    private bool _disposed = false;
    private bool _isSessionLocked = false;

    // Events for session state changes
    public event Action<SessionState>? SessionStateChanged;

    public SessionMonitor(ActivityLogger activityLogger, ILogger<SessionMonitor> logger)
    {
        _activityLogger = activityLogger ?? throw new ArgumentNullException(nameof(activityLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create hidden window to receive session notifications
        _notificationWindow = new SessionNotificationWindow(logger);
        _notificationWindow.SessionStateChanged += OnSessionStateChanged;

        _logger.LogInformation("SessionMonitor initialized successfully");
    }

    /// <summary>
    /// Starts monitoring session changes
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed SessionMonitor");
            return;
        }

        try
        {
            _notificationWindow.RegisterForSessionNotifications();
            _logger.LogInformation("Session monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring session changes
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            _notificationWindow.UnregisterFromSessionNotifications();
            _logger.LogInformation("Session monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session monitoring");
        }
    }

    /// <summary>
    /// Gets the current session state
    /// </summary>
    public bool IsSessionLocked => _isSessionLocked;

    /// <summary>
    /// Event handler for session state changes from the notification window
    /// </summary>
    private void OnSessionStateChanged(SessionState sessionState)
    {
        try
        {
            _logger.LogInformation("Session state changed to: {SessionState}", sessionState);

            var wasLocked = _isSessionLocked;

            switch (sessionState)
            {
                case SessionState.Locked:
                    _isSessionLocked = true;
                    if (!wasLocked && _activityLogger.IsTrackingActive())
                    {
                        _logger.LogInformation("Session locked - pausing activity tracking");
                        _activityLogger.PauseTracking();
                    }
                    break;

                case SessionState.Unlocked:
                    _isSessionLocked = false;
                    if (wasLocked && _activityLogger.IsTrackingPaused())
                    {
                        _logger.LogInformation("Session unlocked - resuming activity tracking");
                        _activityLogger.ResumeTracking();
                    }
                    break;

                case SessionState.Logoff:
                    _isSessionLocked = false;
                    if (_activityLogger.IsTrackingActive() || _activityLogger.IsTrackingPaused())
                    {
                        _logger.LogInformation("User logging off - stopping activity tracking");
                        _activityLogger.Stop();
                    }
                    break;

                case SessionState.Logon:
                    _isSessionLocked = false;
                    // Don't automatically start tracking on logon - let user control this
                    _logger.LogInformation("User logged on - tracking remains in current state");
                    break;

                case SessionState.RemoteConnect:
                    _isSessionLocked = false;
                    _logger.LogInformation("Remote session connected");
                    break;

                case SessionState.RemoteDisconnect:
                    _isSessionLocked = true; // Treat remote disconnect as locked
                    if (_activityLogger.IsTrackingActive())
                    {
                        _logger.LogInformation("Remote session disconnected - pausing activity tracking");
                        _activityLogger.PauseTracking();
                    }
                    break;
            }

            // Notify subscribers
            SessionStateChanged?.Invoke(sessionState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling session state change: {SessionState}", sessionState);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                Stop();
                _notificationWindow?.Dispose();
                _logger.LogInformation("SessionMonitor disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing SessionMonitor");
            }
        }
    }
}

/// <summary>
/// Enumeration of possible session states
/// </summary>
public enum SessionState
{
    Locked,
    Unlocked,
    Logon,
    Logoff,
    RemoteConnect,
    RemoteDisconnect
}

/// <summary>
/// Hidden window class to receive Windows session notifications
/// </summary>
internal class SessionNotificationWindow : Form
{
    private readonly ILogger _logger;
    private bool _isRegistered = false;

    public event Action<SessionState>? SessionStateChanged;

    public SessionNotificationWindow(ILogger logger)
    {
        _logger = logger;

        // Make this a hidden window
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
        this.CreateControl(); // Ensure handle is created

        _logger.LogDebug("Session notification window created with handle: {Handle}", this.Handle);
    }

    /// <summary>
    /// Registers for session change notifications
    /// </summary>
    public void RegisterForSessionNotifications()
    {
        if (_isRegistered) return;

        try
        {
            var result = NativeMethods.WTSRegisterSessionNotification(
                this.Handle,
                NativeMethods.NOTIFY_FOR_THIS_SESSION);

            if (!result)
            {
                var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "Failed to register for session notifications");
            }

            _isRegistered = true;
            _logger.LogDebug("Successfully registered for session notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register for session notifications");
            throw;
        }
    }

    /// <summary>
    /// Unregisters from session change notifications
    /// </summary>
    public void UnregisterFromSessionNotifications()
    {
        if (!_isRegistered) return;

        try
        {
            var result = NativeMethods.WTSUnRegisterSessionNotification(this.Handle);
            if (!result)
            {
                var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to unregister from session notifications. Error: {Error}", error);
            }
            else
            {
                _logger.LogDebug("Successfully unregistered from session notifications");
            }

            _isRegistered = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering from session notifications");
        }
    }

    /// <summary>
    /// Processes Windows messages, specifically WM_WTSSESSION_CHANGE
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_WTSSESSION_CHANGE)
        {
            try
            {
                var sessionChangeReason = (int)m.WParam;
                var sessionId = (int)m.LParam;

                _logger.LogDebug("Session change notification: Reason={Reason}, SessionId={SessionId}", 
                    sessionChangeReason, sessionId);

                var sessionState = MapSessionChangeReason(sessionChangeReason);
                if (sessionState.HasValue)
                {
                    SessionStateChanged?.Invoke(sessionState.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing session change message");
            }
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Maps Windows session change reasons to our SessionState enum
    /// </summary>
    private SessionState? MapSessionChangeReason(int reason)
    {
        return reason switch
        {
            NativeMethods.WTS_SESSION_LOCK => SessionState.Locked,
            NativeMethods.WTS_SESSION_UNLOCK => SessionState.Unlocked,
            NativeMethods.WTS_SESSION_LOGON => SessionState.Logon,
            NativeMethods.WTS_SESSION_LOGOFF => SessionState.Logoff,
            NativeMethods.WTS_REMOTE_CONNECT => SessionState.RemoteConnect,
            NativeMethods.WTS_REMOTE_DISCONNECT => SessionState.RemoteDisconnect,
            _ => null // Ignore other session change reasons
        };
    }

    /// <summary>
    /// Ensures the window is never visible
    /// </summary>
    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterFromSessionNotifications();
        }
        base.Dispose(disposing);
    }
}
