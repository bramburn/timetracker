using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Monitors global keyboard and mouse input to determine user activity status.
/// Uses low-level Windows hooks to detect input without capturing specific content.
/// Provides binary active/inactive status based on input presence within a timeout period.
/// </summary>
public class InputMonitor : IInputMonitor
{
    private readonly ILogger<InputMonitor> _logger;
    private readonly int _activityTimeoutMs;
    private readonly Timer _activityTimer;

    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private DateTime _lastInputTime = DateTime.MinValue;
    private ActivityStatus _currentActivityStatus = ActivityStatus.Inactive;
    private bool _disposed = false;

    // Hook procedure delegates (must be kept alive to prevent GC)
    private readonly NativeMethods.LowLevelProc _keyboardProc;
    private readonly NativeMethods.LowLevelProc _mouseProc;

    // Event to notify when activity status changes
    public event Action<ActivityStatus>? ActivityStatusChanged;

    public InputMonitor(IConfiguration configuration, ILogger<InputMonitor> logger)
    {
        _logger = logger;
        _activityTimeoutMs = configuration.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000);

        // Initialize hook procedures
        _keyboardProc = KeyboardHookProc;
        _mouseProc = MouseHookProc;

        // Initialize activity timeout timer
        _activityTimer = new Timer(CheckActivityTimeout, null,
            TimeSpan.FromMilliseconds(_activityTimeoutMs),
            TimeSpan.FromMilliseconds(_activityTimeoutMs));

        _logger.LogInformation("InputMonitor initialized with activity timeout: {Timeout}ms", _activityTimeoutMs);
    }

    /// <summary>
    /// Starts monitoring global input events
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed InputMonitor");
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
                _logger.LogError("Failed to install keyboard hook. Error code: {ErrorCode}", error);
            }
            else
            {
                _logger.LogDebug("Keyboard hook installed successfully");
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
                _logger.LogError("Failed to install mouse hook. Error code: {ErrorCode}", error);
            }
            else
            {
                _logger.LogDebug("Mouse hook installed successfully");
            }

            if (_keyboardHook != IntPtr.Zero || _mouseHook != IntPtr.Zero)
            {
                _logger.LogInformation("Input monitoring started");
            }
            else
            {
                _logger.LogError("Failed to start input monitoring - no hooks installed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start input monitoring");
        }
    }

    /// <summary>
    /// Stops monitoring global input events
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
                _logger.LogDebug("Keyboard hook removed");
            }

            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
                _logger.LogDebug("Mouse hook removed");
            }

            _logger.LogInformation("Input monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping input monitoring");
        }
    }

    /// <summary>
    /// Low-level keyboard hook procedure
    /// </summary>
    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= NativeMethods.HC_ACTION)
            {
                // Check for key down events
                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN ||
                    wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                {
                    OnInputDetected("Keyboard");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in keyboard hook procedure");
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Low-level mouse hook procedure
    /// </summary>
    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= NativeMethods.HC_ACTION)
            {
                // Check for mouse events (movement, clicks, wheel)
                if (wParam == (IntPtr)NativeMethods.WM_MOUSEMOVE ||
                    wParam == (IntPtr)NativeMethods.WM_LBUTTONDOWN ||
                    wParam == (IntPtr)NativeMethods.WM_RBUTTONDOWN ||
                    wParam == (IntPtr)NativeMethods.WM_MOUSEWHEEL)
                {
                    OnInputDetected("Mouse");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mouse hook procedure");
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Called when input is detected from keyboard or mouse
    /// </summary>
    /// <param name="inputType">Type of input detected (for logging)</param>
    private void OnInputDetected(string inputType)
    {
        _lastInputTime = DateTime.UtcNow;

        if (_currentActivityStatus == ActivityStatus.Inactive)
        {
            _currentActivityStatus = ActivityStatus.Active;
            _logger.LogDebug("Activity status changed to Active due to {InputType} input", inputType);
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
    /// <returns>Current activity status (Active or Inactive)</returns>
    public ActivityStatus GetCurrentActivityStatus()
    {
        return _currentActivityStatus;
    }

    /// <summary>
    /// Gets the time since last input was detected
    /// </summary>
    /// <returns>TimeSpan since last input, or TimeSpan.MaxValue if no input detected</returns>
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
            _activityTimer?.Dispose();
            _disposed = true;
            _logger.LogInformation("InputMonitor disposed");
        }
    }
}
