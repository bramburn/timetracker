using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Optimized window monitor that uses SetWinEventHook for event-driven window change detection.
/// Replaces the polling mechanism with immediate notification of foreground window changes.
/// Provides significant performance improvements over timer-based polling.
/// </summary>
public class OptimizedWindowMonitor : IWindowMonitor
{
    private readonly ILogger<OptimizedWindowMonitor> _logger;
    private readonly string _currentUsername;
    private readonly NativeMethods.WinEventDelegate _winEventProc;

    private IntPtr _winEventHook = IntPtr.Zero;
    private ActivityDataModel? _lastActivity;
    private bool _disposed = false;

    // Event to notify when window changes are detected
    public event Action<ActivityDataModel?>? WindowChanged;

    public OptimizedWindowMonitor(IConfiguration configuration, ILogger<OptimizedWindowMonitor> logger)
    {
        _logger = logger;

        // Capture current Windows username
        _currentUsername = GetCurrentUsername();

        // Keep delegate alive to prevent garbage collection
        _winEventProc = WinEventCallback;

        _logger.LogInformation("OptimizedWindowMonitor initialized for user: {Username}", _currentUsername);
    }

    /// <summary>
    /// Gets the current Windows username
    /// </summary>
    /// <returns>The current Windows username or "UNKNOWN" if unable to determine</returns>
    private static string GetCurrentUsername()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.Name ?? "UNKNOWN";
        }
        catch (Exception)
        {
            return "UNKNOWN";
        }
    }

    /// <summary>
    /// Starts the optimized window monitoring using SetWinEventHook
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed OptimizedWindowMonitor");
            return;
        }

        try
        {
            _winEventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventProc,
                0,
                0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);

            if (_winEventHook == IntPtr.Zero)
            {
                _logger.LogError("Failed to install window event hook");
                throw new InvalidOperationException("Cannot install window monitoring hook");
            }

            _logger.LogInformation("Optimized window monitoring started with event hook");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start optimized window monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops the window monitoring and unhooks the event
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
                _logger.LogInformation("Window event hook removed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping window monitoring");
        }
    }

    /// <summary>
    /// WinEvent callback method that processes EVENT_SYSTEM_FOREGROUND events
    /// </summary>
    private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            if (eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
            {
                var activity = CaptureWindowActivity(hwnd);
                if (activity != null && activity.HasSignificantChanges(_lastActivity))
                {
                    _logger.LogDebug("Window change detected: {Activity}", activity.ToString());
                    _lastActivity = activity.Clone();
                    WindowChanged?.Invoke(activity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in window event callback");
        }
    }

    /// <summary>
    /// Captures window activity for the specified window handle
    /// </summary>
    /// <param name="hwnd">The window handle to capture activity for</param>
    /// <returns>ActivityDataModel with window information, or null if unable to capture</returns>
    private ActivityDataModel? CaptureWindowActivity(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero)
            {
                _logger.LogDebug("Invalid window handle provided");
                return null;
            }

            // Get window title
            string windowTitle = NativeMethods.GetWindowTitle(hwnd);
            if (string.IsNullOrEmpty(windowTitle))
            {
                _logger.LogDebug("Window has no title");
                return null;
            }

            // Get process information
            uint processId = NativeMethods.GetWindowProcessId(hwnd);
            string processName = NativeMethods.GetProcessName(processId);

            if (string.IsNullOrEmpty(processName))
            {
                _logger.LogDebug("Unable to determine process name for window: {WindowTitle}", windowTitle);
                return null;
            }

            // Create activity data model
            var activity = new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow,
                WindowsUsername = _currentUsername,
                ActiveWindowTitle = windowTitle,
                ApplicationProcessName = processName,
                ActiveWindowHandle = hwnd,
                ActivityStatus = ActivityStatus.Active // Will be updated by InputMonitor
            };

            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture window activity for handle: {Handle}", hwnd);
            return null;
        }
    }

    /// <summary>
    /// Gets the current active window information without triggering change detection
    /// </summary>
    /// <returns>Current window activity data or null if unable to capture</returns>
    public ActivityDataModel? GetCurrentActivity()
    {
        try
        {
            IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            return CaptureWindowActivity(foregroundWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current activity");
            return null;
        }
    }

    /// <summary>
    /// Gets the last captured activity data
    /// </summary>
    /// <returns>Last activity data or null if no activity has been captured</returns>
    public ActivityDataModel? GetLastActivity()
    {
        return _lastActivity?.Clone();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
            _logger.LogInformation("OptimizedWindowMonitor disposed");
        }
    }
}
