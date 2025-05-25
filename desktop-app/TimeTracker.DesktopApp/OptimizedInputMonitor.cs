using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Optimized input monitor that uses Raw Input API for low-latency input detection.
/// Falls back to GetLastInputInfo polling when Raw Input API is not available (e.g., in Windows Services).
/// Replaces low-level hooks with a more efficient, asynchronous approach using
/// a buffered queue and dedicated processing thread with debouncing.
/// </summary>
public class OptimizedInputMonitor : IInputMonitor
{
    private readonly ILogger<OptimizedInputMonitor> _logger;
    private readonly int _activityTimeoutMs;
    private readonly System.Threading.Timer _activityTimeoutTimer;

    // Raw Input related (primary method)
    private readonly HiddenInputWindow? _hiddenInputWindow;
    private bool _rawInputAvailable = false;

    // GetLastInputInfo fallback
    private readonly System.Threading.Timer? _fallbackInputTimer;
    private bool _usingFallbackMethod = false;

    // Buffered Queue related
    private readonly BlockingCollection<InputEvent> _inputQueue = new(200); // MaxQueueSize = 200
    private readonly CancellationTokenSource _cts = new();
    private Thread? _processingThread;

    // Activity tracking
    private DateTime _lastInputTime = DateTime.MinValue;
    private DateTime _lastProcessedInputTime = DateTime.MinValue;
    private ActivityStatus _currentActivityStatus = ActivityStatus.Inactive;
    private bool _disposed = false;

    // Debouncing configuration (configurable)
    private readonly int _debounceThresholdMs;
    private readonly int _fallbackCheckIntervalMs;

    // Event to notify when activity status changes
    public event Action<ActivityStatus>? ActivityStatusChanged;

    public OptimizedInputMonitor(IConfiguration configuration, ILogger<OptimizedInputMonitor> logger)
    {
        _logger = logger;
        _activityTimeoutMs = configuration.GetValue<int>("TimeTracker:ActivityTimeoutMs", 60000);
        _debounceThresholdMs = configuration.GetValue<int>("TimeTracker:DebounceThresholdMs", 100);
        _fallbackCheckIntervalMs = configuration.GetValue<int>("TimeTracker:FallbackCheckIntervalMs", 10000);

        // Try to initialize Raw Input first
        try
        {
            _hiddenInputWindow = new HiddenInputWindow(_logger);
            _hiddenInputWindow.RawInputDetected += OnRawInputDetected;
            _logger.LogDebug("Raw Input window initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Raw Input window, will use GetLastInputInfo fallback");
            _hiddenInputWindow = null;
        }

        // Initialize GetLastInputInfo fallback timer
        _fallbackInputTimer = new System.Threading.Timer(CheckLastInputInfo, null,
            Timeout.Infinite, Timeout.Infinite); // Start disabled

        // Initialize activity timeout timer
        _activityTimeoutTimer = new System.Threading.Timer(CheckActivityTimeout, null,
            TimeSpan.FromMilliseconds(_activityTimeoutMs),
            TimeSpan.FromMilliseconds(_activityTimeoutMs));

        _logger.LogInformation("OptimizedInputMonitor initialized with activity timeout: {Timeout}ms", _activityTimeoutMs);
    }

    /// <summary>
    /// Starts the optimized input monitoring using Raw Input API or GetLastInputInfo fallback
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed OptimizedInputMonitor");
            return;
        }

        // Try Raw Input API first
        if (_hiddenInputWindow != null)
        {
            try
            {
                // Register Raw Input Devices
                var devices = new[]
                {
                    new NativeMethods.RAWINPUTDEVICE
                    {
                        UsagePage = 0x01, // Generic desktop
                        Usage = 0x06,     // Keyboard
                        Flags = NativeMethods.RIDEV_INPUTSINK,
                        Target = _hiddenInputWindow.Handle
                    },
                    new NativeMethods.RAWINPUTDEVICE
                    {
                        UsagePage = 0x01, // Generic desktop
                        Usage = 0x02,     // Mouse
                        Flags = NativeMethods.RIDEV_INPUTSINK,
                        Target = _hiddenInputWindow.Handle
                    }
                };

                if (NativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length,
                    (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTDEVICE))))
                {
                    _rawInputAvailable = true;
                    _logger.LogInformation("Raw Input monitoring started successfully");

                    // Start background processing thread for Raw Input
                    _processingThread = new Thread(ProcessInputQueue)
                    {
                        Priority = ThreadPriority.AboveNormal,
                        IsBackground = true,
                        Name = "InputProcessingThread"
                    };
                    _processingThread.Start();
                    _logger.LogInformation("Input processing thread started");
                    return; // Success with Raw Input
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogWarning("Failed to register raw input devices. Error code: {ErrorCode}. Falling back to GetLastInputInfo", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Raw Input API failed. Falling back to GetLastInputInfo");
            }
        }

        // Fallback to GetLastInputInfo
        StartFallbackMethod();
    }

    /// <summary>
    /// Starts the GetLastInputInfo fallback method
    /// </summary>
    private void StartFallbackMethod()
    {
        try
        {
            _usingFallbackMethod = true;

            // Start background processing thread for fallback events
            _processingThread = new Thread(ProcessInputQueue)
            {
                Priority = ThreadPriority.Normal,
                IsBackground = true,
                Name = "FallbackInputProcessingThread"
            };
            _processingThread.Start();

            _fallbackInputTimer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(_fallbackCheckIntervalMs));
            _logger.LogInformation("GetLastInputInfo fallback monitoring started (checking every {Interval}ms)", _fallbackCheckIntervalMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start GetLastInputInfo fallback method");
            throw;
        }
    }

    /// <summary>
    /// Timer callback for GetLastInputInfo fallback method
    /// </summary>
    private void CheckLastInputInfo(object? state)
    {
        if (_disposed || !_usingFallbackMethod) return;

        try
        {
            var inputInfo = new NativeMethods.LASTINPUTINFO
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
            };

            if (NativeMethods.GetLastInputInfo(ref inputInfo))
            {
                // Calculate the actual time of last input
                var ticksSinceLastInput = Environment.TickCount - inputInfo.dwTime;
                var lastInputTime = DateTime.UtcNow.AddMilliseconds(-ticksSinceLastInput);

                // Check if this is newer than our last recorded input
                if (lastInputTime > _lastInputTime)
                {
                    _logger.LogDebug("GetLastInputInfo detected new input at {LastInputTime}", lastInputTime);

                    // Simulate an input event
                    if (!_inputQueue.IsAddingCompleted)
                    {
                        _inputQueue.Add(new InputEvent(lastInputTime, InputType.Generic));
                    }
                }
            }
            else
            {
                _logger.LogDebug("GetLastInputInfo call failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLastInputInfo fallback check");
        }
    }

    /// <summary>
    /// Stops the input monitoring and processing
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            // Stop fallback timer
            _fallbackInputTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _usingFallbackMethod = false;

            // Stop processing thread
            _cts.Cancel(); // Signal processing thread to stop
            _processingThread?.Join(500); // Wait for thread to finish

            _activityTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            var method = _rawInputAvailable ? "Raw Input" : "GetLastInputInfo fallback";
            _logger.LogInformation("Optimized input monitoring stopped (was using {Method})", method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping optimized input monitoring");
        }
    }

    /// <summary>
    /// Called when raw input is detected by the hidden window
    /// </summary>
    /// <param name="hRawInput">Handle to raw input data</param>
    private void OnRawInputDetected(IntPtr hRawInput)
    {
        try
        {
            // Enqueue the event for asynchronous processing
            // For activity detection, we only need the timestamp
            if (!_inputQueue.IsAddingCompleted)
            {
                _inputQueue.Add(new InputEvent(DateTime.UtcNow, InputType.Generic));
            }
        }
        catch (InvalidOperationException)
        {
            // Queue is completed, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing raw input event");
        }
    }

    /// <summary>
    /// Background thread method that processes the input queue
    /// </summary>
    private void ProcessInputQueue()
    {
        _logger.LogInformation("Input processing thread started consuming events.");
        try
        {
            foreach (var inputEvent in _inputQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    HandleInputEvent(inputEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling input event from queue.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Input processing thread cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in input processing thread.");
        }
    }

    /// <summary>
    /// Handles an individual input event with debouncing logic
    /// </summary>
    /// <param name="input">The input event to handle</param>
    private void HandleInputEvent(InputEvent input)
    {
        // Debouncing: Ignore events too close to the last processed one
        if ((input.Timestamp - _lastProcessedInputTime).TotalMilliseconds < _debounceThresholdMs)
        {
            _logger.LogTrace("Debounced input event at {Timestamp}", input.Timestamp);
            return;
        }

        _lastInputTime = input.Timestamp; // Update last actual input time
        _lastProcessedInputTime = input.Timestamp; // Update last processed input time

        if (_currentActivityStatus == ActivityStatus.Inactive)
        {
            _currentActivityStatus = ActivityStatus.Active;
            _logger.LogDebug("Activity status changed to Active due to input");
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
            Stop(); // Stop monitoring and processing thread
            _activityTimeoutTimer?.Dispose();
            _fallbackInputTimer?.Dispose();
            _inputQueue.Dispose();
            _cts.Dispose();
            _hiddenInputWindow?.Dispose(); // Dispose the hidden window

            _disposed = true;
            _logger.LogInformation("OptimizedInputMonitor disposed");
        }
    }

    /// <summary>
    /// Hidden window class to handle Raw Input messages
    /// </summary>
    private class HiddenInputWindow : Form
    {
        public event Action<IntPtr>? RawInputDetected;
        private readonly ILogger _logger;

        public HiddenInputWindow(ILogger logger)
        {
            _logger = logger;
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.CreateControl(); // Ensure handle is created
            _logger.LogDebug("Hidden input window created with Handle: {Handle}", this.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_INPUT)
            {
                RawInputDetected?.Invoke(m.LParam);
                m.Result = IntPtr.Zero; // Mark message as handled
            }
            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            // Ensure the window is never visible
            base.SetVisibleCore(false);
        }
    }
}
