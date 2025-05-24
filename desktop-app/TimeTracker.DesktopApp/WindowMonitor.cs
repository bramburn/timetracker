using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Monitors active window changes and captures window title and associated process information.
/// Uses Win32 APIs to poll for foreground window changes and detects when the user switches applications.
/// </summary>
public class WindowMonitor : IWindowMonitor
{
    private readonly ILogger<WindowMonitor> _logger;
    private readonly System.Threading.Timer _monitoringTimer;
    private readonly int _monitoringIntervalMs;
    private readonly string _currentUsername;

    private ActivityDataModel? _lastActivity;
    private bool _disposed = false;

    // Event to notify when window changes are detected
    public event Action<ActivityDataModel?>? WindowChanged;

    public WindowMonitor(IConfiguration configuration, ILogger<WindowMonitor> logger)
    {
        _logger = logger;
        _monitoringIntervalMs = configuration.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000);

        // Capture current Windows username
        _currentUsername = GetCurrentUsername();
        _logger.LogInformation("WindowMonitor initialized for user: {Username}, monitoring interval: {Interval}ms",
            _currentUsername, _monitoringIntervalMs);

        // Initialize monitoring timer
        _monitoringTimer = new System.Threading.Timer(MonitorActiveWindow, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_monitoringIntervalMs));
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
    /// Timer callback method that monitors the active window
    /// </summary>
    /// <param name="state">Timer state (unused)</param>
    private void MonitorActiveWindow(object? state)
    {
        if (_disposed) return;

        try
        {
            var currentActivity = CaptureCurrentWindowActivity();

            // Check if there's a significant change from the last activity
            if (currentActivity != null && currentActivity.HasSignificantChanges(_lastActivity))
            {
                _logger.LogDebug("Window change detected: {Activity}", currentActivity.ToString());

                // Update last activity and notify listeners
                _lastActivity = currentActivity.Clone();
                WindowChanged?.Invoke(currentActivity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while monitoring active window");
        }
    }

    /// <summary>
    /// Captures the current active window information
    /// </summary>
    /// <returns>ActivityDataModel with current window information, or null if unable to capture</returns>
    private ActivityDataModel? CaptureCurrentWindowActivity()
    {
        try
        {
            // Get the foreground window handle
            IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                _logger.LogDebug("No foreground window detected");
                return null;
            }

            // Get window title
            string windowTitle = NativeMethods.GetWindowTitle(foregroundWindow);
            if (string.IsNullOrEmpty(windowTitle))
            {
                _logger.LogDebug("Foreground window has no title");
                return null;
            }

            // Get process information
            uint processId = NativeMethods.GetWindowProcessId(foregroundWindow);
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
                ActiveWindowHandle = foregroundWindow,
                ActivityStatus = ActivityStatus.Active // Will be updated by InputMonitor
            };

            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture current window activity");
            return null;
        }
    }

    /// <summary>
    /// Gets the current active window information without triggering change detection
    /// </summary>
    /// <returns>Current window activity data or null if unable to capture</returns>
    public ActivityDataModel? GetCurrentActivity()
    {
        return CaptureCurrentWindowActivity();
    }

    /// <summary>
    /// Gets the last captured activity data
    /// </summary>
    /// <returns>Last activity data or null if no activity has been captured</returns>
    public ActivityDataModel? GetLastActivity()
    {
        return _lastActivity?.Clone();
    }

    /// <summary>
    /// Starts the window monitoring process
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed WindowMonitor");
            return;
        }

        _logger.LogInformation("Window monitoring started");
        // Timer is already started in constructor, this method is for explicit control if needed
    }

    /// <summary>
    /// Stops the window monitoring process
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        _monitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.LogInformation("Window monitoring stopped");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _monitoringTimer?.Dispose();
            _disposed = true;
            _logger.LogInformation("WindowMonitor disposed");
        }
    }
}
