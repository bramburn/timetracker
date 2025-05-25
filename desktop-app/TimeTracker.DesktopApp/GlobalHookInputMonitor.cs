using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Input monitor that uses low-level global hooks to detect keyboard and mouse activity.
/// This approach works across Windows session boundaries, making it suitable for Windows Services.
/// Uses SetWindowsHookEx with WH_KEYBOARD_LL and WH_MOUSE_LL to capture system-wide input events.
/// </summary>
public class GlobalHookInputMonitor : IInputMonitor
{
    private readonly ILogger<GlobalHookInputMonitor> _logger;
    private readonly int _activityTimeoutMs;
    private readonly System.Threading.Timer _activityTimeoutTimer;

    // Global hook handles
    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;

    // Hook procedure delegates (must be kept alive to prevent GC)
    private readonly NativeMethods.LowLevelProc _keyboardProc;
    private readonly NativeMethods.LowLevelProc _mouseProc;

    // Activity tracking
    private DateTime _lastInputTime = DateTime.MinValue;
    private ActivityStatus _currentActivityStatus = ActivityStatus.Inactive;
    private bool _disposed = false;

    // Debouncing configuration
    private const int DebounceThresholdMs = 50; // Ignore events less than 50ms apart
    private DateTime _lastProcessedInputTime = DateTime.MinValue;

    // Events
    public event Action<ActivityStatus>? ActivityStatusChanged;

    public GlobalHookInputMonitor(IConfiguration configuration, ILogger<GlobalHookInputMonitor> logger)
    {
        _logger = logger;
        _activityTimeoutMs = configuration.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000);

        // Initialize hook procedures (must keep references to prevent GC)
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        // Initialize activity timeout timer
        _activityTimeoutTimer = new System.Threading.Timer(CheckActivityTimeout, null,
            TimeSpan.FromMilliseconds(_activityTimeoutMs),
            TimeSpan.FromMilliseconds(_activityTimeoutMs));

        _logger.LogInformation("GlobalHookInputMonitor initialized with activity timeout: {Timeout}ms", _activityTimeoutMs);
    }

    /// <summary>
    /// Starts global input monitoring using low-level hooks
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed GlobalHookInputMonitor");
            return;
        }

        try
        {
            // Install keyboard hook
            _keyboardHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                NativeMethods.GetModuleHandle(null!),
                0);

            if (_keyboardHook == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to install keyboard hook. Error: {error}");
            }

            // Install mouse hook
            _mouseHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseProc,
                NativeMethods.GetModuleHandle(null!),
                0);

            if (_mouseHook == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                // Clean up keyboard hook if mouse hook fails
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
                throw new InvalidOperationException($"Failed to install mouse hook. Error: {error}");
            }

            _logger.LogInformation("Global hooks installed successfully (Keyboard: {KeyboardHook}, Mouse: {MouseHook})",
                _keyboardHook, _mouseHook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start global hook input monitoring");
            throw;
        }
    }

    /// <summary>
    /// Stops global input monitoring and removes hooks
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            // Remove keyboard hook
            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            // Remove mouse hook
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            _activityTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Global hooks removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping global hook input monitoring");
        }
    }

    /// <summary>
    /// Low-level keyboard hook callback
    /// </summary>
    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                // Process keyboard input
                HandleInputEvent();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in keyboard hook callback");
        }

        // Always call next hook
        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Low-level mouse hook callback
    /// </summary>
    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                // Process mouse input
                HandleInputEvent();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mouse hook callback");
        }

        // Always call next hook
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Handles detected input events with debouncing
    /// </summary>
    private void HandleInputEvent()
    {
        var currentTime = DateTime.UtcNow;

        // Debouncing: Ignore events too close to the last processed one
        if ((currentTime - _lastProcessedInputTime).TotalMilliseconds < DebounceThresholdMs)
        {
            return;
        }

        _lastInputTime = currentTime;
        _lastProcessedInputTime = currentTime;

        if (_currentActivityStatus == ActivityStatus.Inactive)
        {
            _currentActivityStatus = ActivityStatus.Active;
            _logger.LogDebug("Activity status changed to Active due to global hook input");
            ActivityStatusChanged?.Invoke(_currentActivityStatus);
        }
    }

    /// <summary>
    /// Timer callback to check for activity timeout
    /// </summary>
    private void CheckActivityTimeout(object? state)
    {
        if (_disposed) return;

        try
        {
            var timeSinceLastInput = DateTime.UtcNow - _lastInputTime;

            if (_currentActivityStatus == ActivityStatus.Active &&
                timeSinceLastInput.TotalMilliseconds > _activityTimeoutMs)
            {
                _currentActivityStatus = ActivityStatus.Inactive;
                _logger.LogDebug("Activity status changed to Inactive due to timeout ({Timeout}ms)", _activityTimeoutMs);
                ActivityStatusChanged?.Invoke(_currentActivityStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking activity timeout");
        }
    }

    /// <summary>
    /// Gets the current activity status
    /// </summary>
    public ActivityStatus GetCurrentActivityStatus()
    {
        return _currentActivityStatus;
    }

    /// <summary>
    /// Gets the time since last input was detected
    /// </summary>
    public TimeSpan GetTimeSinceLastInput()
    {
        if (_lastInputTime == DateTime.MinValue)
            return TimeSpan.MaxValue;

        return DateTime.UtcNow - _lastInputTime;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _activityTimeoutTimer?.Dispose();
            _disposed = true;
            _logger.LogInformation("GlobalHookInputMonitor disposed");
        }
    }
}
