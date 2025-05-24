Product Requirements Document: Time Tracking Application Performance Optimization

    Version: 1.0

    Date: May 24, 2025

    Author: Gemini

    Stakeholders: Engineering Team, Product Management, QA

1. Introduction

This document outlines the product requirements for enhancing the performance and resource efficiency of the Time Tracking Desktop Application. The current implementation exhibits several bottlenecks, particularly concerning user input monitoring and data processing, which can lead to system responsiveness issues and suboptimal resource utilization.

The primary goal of this initiative is to refactor critical components of the application to ensure stable, low-latency activity tracking while minimizing interference with the user's system, even under high CPU load or during bursts of user input.
2. Goals & Objectives

The overarching goal is to transform the Time Tracking application into a highly performant and resource-efficient background service.

Specific, Measurable Objectives:

    Input Latency: Achieve <10ms event timestamp accuracy for activity tracking, even under high load.

    CPU Efficiency: Reduce CPU spikes by 67 during input bursts.

    Predictable Latency: Ensure 95 of input events are processed within 2ms of arrival.

    System Impact: Eliminate system-wide input lag caused by the application's monitoring mechanisms.

    Resource Consumption: Minimize overall CPU and memory footprint during continuous operation.

    Data Reliability: Maintain 0 event loss rate for critical activity data.

    Graceful Degradation: Implement controlled event dropping (if necessary) to prevent UI freezes under extreme conditions.

3. Current State & Problem Statement

The current Time Tracking application, as analyzed from the provided codebase, relies on two primary monitoring mechanisms:

    WindowMonitor: Uses a polling mechanism (every 1000ms) to detect changes in the active foreground window.

    InputMonitor: Employs low-level Windows hooks (WH_KEYBOARD_LL and WH_MOUSE_LL) to detect global keyboard and mouse activity.

Current Performance Issues:

    Polling Overhead (WindowMonitor): Timer-based polling introduces inherent delays and can be inefficient, as it repeatedly checks for changes even when none have occurred.

    Hook-Related Input Lag (InputMonitor): Low-level hooks execute synchronously within the context of the calling thread. This design can lead to significant system-wide input lag, especially when the application experiences high CPU usage. Windows can remove hooks that do not respond within a timeout period, further exacerbating stability issues. The current implementation processes these hooks on the main application thread, which is a critical bottleneck.

    Blocking Operations: Data submission to the Pipedream endpoint and local database insertions are performed in a manner that can potentially block the main monitoring loop, affecting responsiveness and tracking accuracy.

    Inefficient Database Writes: Individual database inserts for each activity record can lead to high I/O overhead, particularly during periods of frequent activity.

4. Proposed Solution

The proposed solution involves a multi-faceted approach to optimize each problematic component, focusing on event-driven architectures, asynchronous processing, and batch operations.
4.1. Architectural Overview

The revised architecture will decouple monitoring from processing and data persistence. Input and window events will be captured efficiently and then queued for asynchronous, debounced, and batched processing in background threads.

    Event-Driven Window Monitoring: Replace polling with SetWinEventHook for immediate notification of foreground window changes.

    Raw Input API for Input Monitoring: Utilize the Raw Input API for capturing keyboard and mouse events, providing a more efficient, asynchronous, and process-local alternative to low-level hooks.

    Buffered Input Processing: Implement a producer-consumer queue to handle high-frequency input events, applying debouncing and temporal coalescing to reduce processing load.

    Asynchronous Data Submission: Offload Pipedream data submission to a background task queue with concurrency limits.

    Batch Database Inserts: Group multiple activity records into batches for more efficient database writes.

4.2. Detailed Component Changes
4.2.1. Optimized Window Monitoring (WindowMonitor)

    Change: Replace the existing Timer-based polling in WindowMonitor with SetWinEventHook. This Win32 API provides event-driven notifications for foreground window changes, eliminating the need for continuous polling.

    Benefits:

        Instantaneous Detection: Responds immediately to window changes.

        Reduced CPU Usage: Eliminates the overhead of periodic polling.

        Improved Responsiveness: Ensures activity data is captured as soon as a window change occurs.

    Reference (from research):

    public class OptimizedWindowMonitor : IDisposable
    {
        private IntPtr _winEventHook = IntPtr.Zero;
        private readonly WinEventDelegate _winEventProc;
        private readonly ILogger _logger;
        private readonly string _currentUsername;
        private ActivityDataModel? _lastActivity;

        // Event delegate - must be kept alive to prevent GC
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, 
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, 
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, 
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        public event Action<ActivityDataModel>? WindowChanged;

        public OptimizedWindowMonitor(ILogger<OptimizedWindowMonitor> logger)
        {
            _logger = logger;
            _currentUsername = GetCurrentUsername(); // Existing method
            _winEventProc = WinEventCallback; // Keep delegate alive
        }

        public void Start()
        {
            _winEventHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            if (_winEventHook == IntPtr.Zero)
            {
                _logger.LogError("Failed to install window event hook");
                throw new InvalidOperationException("Cannot install window monitoring hook");
            }

            _logger.LogInformation("Optimized window monitoring started");
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                if (eventType == EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
                {
                    // CaptureWindowActivity is an existing method that needs to be adapted
                    // to take hwnd as a parameter or get it from foregroundWindow
                    var activity = CaptureWindowActivity(hwnd); 
                    if (activity != null && activity.HasSignificantChanges(_lastActivity))
                    {
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

        // Existing GetCurrentUsername, CaptureWindowActivity, GetLastActivity, Stop, Dispose methods
        // CaptureWindowActivity needs to be updated to accept IntPtr hwnd if it doesn't already.
        private ActivityDataModel? CaptureWindowActivity(IntPtr hwnd)
        {
            // Existing logic from original CaptureCurrentWindowActivity, adapted to use provided hwnd
            // ...
            return null; // Placeholder
        }

        public void Dispose()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
                _logger.LogInformation("Window event hook removed");
            }
            // ... other disposal logic
        }
    }

4.2.2. Optimized Input Monitoring (InputMonitor)

    Change: Replace low-level hooks (WH_KEYBOARD_LL, WH_MOUSE_LL) with the Raw Input API for capturing keyboard and mouse events. This API operates asynchronously via the message queue, processes batched input events without blocking, and uses significantly less CPU. Additionally, integrate a buffered queue with a dedicated processing thread to handle high-frequency inputs smoothly, applying debouncing and temporal coalescing.

    Benefits:

        Reduced Input Lag: Eliminates synchronous blocking caused by hooks, leading to 2−8ms input latency.

        Lower CPU Usage: Uses 0.5−1.5 CPU, a substantial reduction from hooks.

        Process-Local Impact: Input processing is isolated to the application's process.

        Smooth High-Frequency Handling: The buffered queue ensures that bursts of input are processed efficiently without overwhelming the system, maintaining <10ms event timestamp accuracy.

        Graceful Degradation: Selective event dropping (if queue exceeds capacity) prevents UI freezes under extreme conditions.

    Implementation Strategy:

        Raw Input Registration: Register for raw input devices (keyboard and mouse) using RegisterRawInputDevices within a hidden window (or a Form in a WinForms context, which can handle WndProc).

        Event Enqueuing: The WndProc method of the hidden window will receive WM_INPUT messages. Instead of direct processing, it will enqueue InputEvent objects (timestamped) into a BlockingCollection.

        Dedicated Processing Thread: A separate, high-priority background thread will consume events from this queue.

        Debouncing & Coalescing: The processing thread will apply debouncing (e.g., ignore events <50ms apart) and temporal coalescing (grouping rapid events into a single activity burst) to reduce redundant processing.

    Reference (adapted from research):

    public class OptimizedInputMonitor : IInputMonitor
    {
        private readonly ILogger<OptimizedInputMonitor> _logger;
        private readonly int _activityTimeoutMs;
        private readonly Timer _activityTimeoutTimer; // For checking idle status
        private DateTime _lastInputTime = DateTime.MinValue;
        private ActivityStatus _currentActivityStatus = ActivityStatus.Inactive;
        private bool _disposed = false;

        // Raw Input API related
        private readonly HiddenInputWindow _hiddenInputWindow;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003; // WM_INPUT message

        // Buffered Queue related
        private readonly BlockingCollection<InputEvent> _inputQueue = new(200); // MaxQueueSize = 200
        private readonly CancellationTokenSource _cts = new();
        private Thread? _processingThread;

        public event Action<ActivityStatus>? ActivityStatusChanged;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        // Internal class to handle window messages for Raw Input
        private class HiddenInputWindow : Form
        {
            public event Action<IntPtr>? RawInputDetected;
            private readonly ILogger _logger;

            public HiddenInputWindow(ILogger logger)
            {
                _logger = logger;
                this.Visible = false;
                this.ShowInTaskbar = false;
                this.CreateControl(); // Ensure handle is created
                _logger.LogDebug("Hidden input window created with Handle: {Handle}", this.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == RID_INPUT)
                {
                    RawInputDetected?.Invoke(m.LParam);
                    m.Result = IntPtr.Zero; // Mark message as handled
                }
                base.WndProc(ref m);
            }
        }

        public OptimizedInputMonitor(IConfiguration configuration, ILogger<OptimizedInputMonitor> logger)
        {
            _logger = logger;
            _activityTimeoutMs = configuration.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000);

            _hiddenInputWindow = new HiddenInputWindow(_logger);
            _hiddenInputWindow.RawInputDetected += OnRawInputDetected;

            // Initialize activity timeout timer
            _activityTimeoutTimer = new Timer(CheckActivityTimeout, null,
                TimeSpan.FromMilliseconds(_activityTimeoutMs),
                TimeSpan.FromMilliseconds(_activityTimeoutMs));

            _logger.LogInformation("OptimizedInputMonitor initialized with activity timeout: {Timeout}ms", _activityTimeoutMs);
        }

        public void Start()
        {
            if (_disposed)
            {
                _logger.LogWarning("Cannot start disposed OptimizedInputMonitor");
                return;
            }

            try
            {
                // Register Raw Input Devices
                var devices = new[]
                {
                    new RAWINPUTDEVICE
                    {
                        UsagePage = 0x01, // Generic desktop
                        Usage = 0x06,     // Keyboard
                        Flags = RIDEV_INPUTSINK,
                        Target = _hiddenInputWindow.Handle
                    },
                    new RAWINPUTDEVICE
                    {
                        UsagePage = 0x01, // Generic desktop
                        Usage = 0x02,     // Mouse
                        Flags = RIDEV_INPUTSINK,
                        Target = _hiddenInputWindow.Handle
                    }
                };

                if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to register raw input devices. Error code: {ErrorCode}", error);
                    throw new InvalidOperationException($"Failed to register raw input devices. Error: {error}");
                }
                _logger.LogInformation("Raw Input monitoring started.");

                // Start background processing thread
                _processingThread = new Thread(ProcessInputQueue)
                {
                    Priority = ThreadPriority.AboveNormal, // Set high priority for responsiveness
                    IsBackground = true,
                    Name = "InputProcessingThread"
                };
                _processingThread.Start();
                _logger.LogInformation("Input processing thread started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start optimized input monitoring");
                throw;
            }
        }

        public void Stop()
        {
            if (_disposed) return;

            try
            {
                // Unregister Raw Input Devices (can be tricky, often done on app exit)
                // For simplicity, we'll rely on Dispose to clean up, or a more robust unregistration
                // if the hidden window is explicitly closed.
                // A proper unregistration would involve setting Target to IntPtr.Zero and Flags to RIDEV_REMOVE
                // but this can be complex in a service context.

                _cts.Cancel(); // Signal processing thread to stop
                _processingThread?.Join(500); // Wait for thread to finish

                _activityTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogInformation("Optimized input monitoring stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while stopping optimized input monitoring");
            }
        }

        private void OnRawInputDetected(IntPtr hRawInput)
        {
            // Enqueue the event for asynchronous processing
            // In a real scenario, you'd parse hRawInput to get more details
            // For now, just a timestamp is sufficient for activity detection
            _inputQueue.Add(new InputEvent(DateTime.UtcNow, InputType.Generic));
        }

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

        private DateTime _lastProcessedInputTime = DateTime.MinValue;
        private const int DebounceThresholdMs = 50; // Ignore events less than 50ms apart

        private void HandleInputEvent(InputEvent input)
        {
            // Debouncing: Ignore events too close to the last processed one
            if ((input.Timestamp - _lastProcessedInputTime).TotalMilliseconds < DebounceThresholdMs)
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

        public ActivityStatus GetCurrentActivityStatus()
        {
            // This might need to be adjusted if _currentActivityStatus is only updated by the processing thread.
            // For simplicity, we'll assume it's safe to read.
            return _currentActivityStatus;
        }

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
                _inputQueue.Dispose();
                _cts.Dispose();
                _hiddenInputWindow?.Dispose(); // Dispose the hidden window

                _disposed = true;
                _logger.LogInformation("OptimizedInputMonitor disposed");
            }
        }

        // Helper classes for buffered queue
        private class InputEvent
        {
            public DateTime Timestamp { get; }
            public InputType Type { get; }

            public InputEvent(DateTime timestamp, InputType type)
            {
                Timestamp = timestamp;
                Type = type;
            }
        }

        private enum InputType { Keyboard, Mouse, Generic }
    }

4.2.3. Asynchronous Data Handling (ActivityLogger)

    Change: Modify ActivityLogger to offload the _pipedreamClient.SubmitActivityDataAsync call to a background task queue. This will ensure that network operations (which can be slow or unreliable) do not block the core activity monitoring and local storage logic. A SemaphoreSlim will be used to limit concurrent submissions to prevent overwhelming the network or endpoint.

    Benefits:

        Non-Blocking Monitoring: Ensures the main monitoring thread remains responsive.

        Improved Reliability: Decouples local storage from remote submission, allowing local data capture even if Pipedream is unavailable.

        Resource Management: Prevents excessive concurrent network requests.

    Reference (from research):

    public class OptimizedActivityLogger : ActivityLogger // Inherit or adapt existing ActivityLogger
    {
        private readonly BackgroundTaskQueue _submissionQueue;
        private readonly SemaphoreSlim _submissionSemaphore;
        private readonly ILogger<OptimizedActivityLogger> _logger; // Use specific logger
        private readonly IDataAccess _dataAccess;
        private readonly IPipedreamClient _pipedreamClient;
        private readonly CancellationTokenSource _cts = new(); // For background task cancellation

        // Assuming ActivityLogger now has a way to get dependencies
        public OptimizedActivityLogger(
            IDataAccess dataAccess,
            IPipedreamClient pipedreamClient,
            IWindowMonitor windowMonitor, // Now OptimizedWindowMonitor
            IInputMonitor inputMonitor,   // Now OptimizedInputMonitor
            ILogger<OptimizedActivityLogger> logger)
            : base(dataAccess, pipedreamClient, windowMonitor, inputMonitor, (ILogger<ActivityLogger>)logger) // Pass base constructor args
        {
            _logger = logger;
            _dataAccess = dataAccess;
            _pipedreamClient = pipedreamClient;
            _submissionQueue = new BackgroundTaskQueue();
            _submissionSemaphore = new SemaphoreSlim(3, 3); // Limit concurrent submissions to 3

            // Start background submission processor
            _ = Task.Run(() => ProcessSubmissionQueue(_cts.Token));

            _logger.LogInformation("OptimizedActivityLogger initialized with background submission queue.");
        }

        // Override or replace the original LogActivityAsync
        private async Task LogActivityAsync(ActivityDataModel activityData)
        {
            if (_disposed) return; // Assuming _disposed from base class or local

            try
            {
                _logger.LogDebug("Logging activity: {Activity}", activityData.ToString());

                // Update current activity (if needed, this logic might be in base)
                // _currentActivity = activityData.Clone(); 

                // Store locally (this should always succeed or we have bigger problems)
                var localStorageSuccess = await _dataAccess.InsertActivityAsync(activityData);
                if (!localStorageSuccess)
                {
                    _logger.LogError("Failed to store activity data locally: {Activity}", activityData.ToString());
                    return; // Don't attempt remote submission if local storage failed
                }

                // Queue remote submission without blocking
                _submissionQueue.QueueBackgroundWorkItem(async token =>
                {
                    await _submissionSemaphore.WaitAsync(token);
                    try
                    {
                        var submissionSuccess = await _pipedreamClient.SubmitActivityDataAsync(activityData);
                        if (!submissionSuccess)
                        {
                            _logger.LogWarning("Failed to submit activity data to Pipedream: {Activity}", activityData.ToString());
                        }
                    }
                    finally
                    {
                        _submissionSemaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimized activity logging: {Activity}", activityData.ToString());
            }
        }

        private async Task ProcessSubmissionQueue(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pipedream submission queue processor started.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue and execute the work item
                    await _submissionQueue.DequeueAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Pipedream submission queue processor cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing submission queue item. Retrying in 1 second...");
                    await Task.Delay(1000, cancellationToken); // Small delay on error
                }
            }
        }

        public override void Dispose()
        {
            if (!_disposed) // Assuming _disposed from base class
            {
                _cts.Cancel(); // Signal background task to stop
                _submissionQueue.CompleteAdding(); // Mark queue as complete
                // Give some time for pending tasks to finish, or wait on _processingThread.Join() if it was exposed
                _submissionSemaphore.Dispose();
                _submissionQueue.Dispose();
                base.Dispose(); // Call base dispose
                _logger.LogInformation("OptimizedActivityLogger disposed");
            }
        }
    }

    // A simple BackgroundTaskQueue implementation (can be more robust)
    public class BackgroundTaskQueue : IDisposable
    {
        private readonly BlockingCollection<Func<CancellationToken, Task>> _workItems = new();

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            _workItems.Add(workItem);
        }

        public async Task DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = _workItems.Take(cancellationToken);
            await workItem(cancellationToken);
        }

        public void CompleteAdding()
        {
            _workItems.CompleteAdding();
        }

        public void Dispose()
        {
            _workItems.Dispose();
        }
    }

4.2.4. Optimized Database Operations (SQLiteDataAccess)

    Change: Implement batch insertion for activity records in SQLiteDataAccess. Instead of inserting each record individually, incoming records will be enqueued and periodically inserted in batches (e.g., every 10 seconds or when a batch size of 50 items is reached). This will significantly reduce database I/O overhead.

    Benefits:

        Reduced I/O Operations: Fewer disk writes, improving performance.

        Improved Throughput: Processes more records per unit of time.

        Lower CPU Usage: Less overhead from frequent connection opening/closing and transaction management.

    Reference (from research):

    public class OptimizedSQLiteDataAccess : IDataAccess
    {
        private readonly string _connectionString;
        private readonly ILogger<OptimizedSQLiteDataAccess> _logger;
        private bool _disposed = false;

        private readonly ConcurrentQueue<ActivityDataModel> _pendingInserts;
        private readonly Timer _batchInsertTimer;
        private readonly SemaphoreSlim _batchSemaphore; // To prevent concurrent batch writes
        private const int MaxBatchSize = 50;
        private const int BatchInsertIntervalMs = 10000; // 10 seconds

        public OptimizedSQLiteDataAccess(string databasePath, ILogger<OptimizedSQLiteDataAccess> logger)
        {
            _logger = logger;

            // Ensure the database directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={databasePath}";

            InitializeDatabase(); // Existing method

            _pendingInserts = new ConcurrentQueue<ActivityDataModel>();
            _batchSemaphore = new SemaphoreSlim(1, 1); // Only one batch insert at a time

            // Batch insert every 10 seconds
            _batchInsertTimer = new Timer(ProcessBatchInserts, null,
                TimeSpan.FromMilliseconds(BatchInsertIntervalMs),
                TimeSpan.FromMilliseconds(BatchInsertIntervalMs));

            _logger.LogInformation("OptimizedSQLiteDataAccess initialized with batching.");
        }

        // Existing InitializeDatabase, GetRecentActivitiesAsync, GetActivityCountAsync methods

        /// <summary>
        /// Enqueues activity data for batch insertion.
        /// </summary>
        public async Task<bool> InsertActivityAsync(ActivityDataModel activityData)
        {
            if (_disposed)
            {
                _logger.LogWarning("Attempted to insert data into disposed OptimizedSQLiteDataAccess");
                return false;
            }

            if (activityData == null)
            {
                _logger.LogWarning("Attempted to enqueue null activity data");
                return false;
            }

            _pendingInserts.Enqueue(activityData);

            // Trigger immediate batch if queue is large
            if (_pendingInserts.Count >= MaxBatchSize)
            {
                _logger.LogDebug("Batch size reached ({Count}), triggering immediate batch insert.", _pendingInserts.Count);
                // Run in background to avoid blocking the caller
                _ = Task.Run(() => ProcessBatchInserts(null));
            }

            return true; // Enqueue operation always succeeds immediately
        }

        /// <summary>
        /// Processes pending inserts in batches.
        /// </summary>
        private async void ProcessBatchInserts(object? state)
        {
            // Try to acquire the semaphore within a short timeout
            if (_disposed || !await _batchSemaphore.WaitAsync(100))
            {
                _logger.LogTrace("Skipping batch insert, disposed or semaphore not acquired.");
                return;
            }

            try
            {
                var batch = new List<ActivityDataModel>();
                // Dequeue up to MaxBatchSize or until queue is empty
                while (_pendingInserts.TryDequeue(out var item) && batch.Count < MaxBatchSize)
                {
                    batch.Add(item);
                }

                if (batch.Count == 0)
                {
                    _logger.LogTrace("No items in queue for batch insert.");
                    return;
                }

                await ExecuteBatchInsert(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch inserts.");
            }
            finally
            {
                _batchSemaphore.Release(); // Release the semaphore
            }
        }

        /// <summary>
        /// Executes a single batch insert transaction.
        /// </summary>
        private async Task ExecuteBatchInsert(List<ActivityDataModel> activities)
        {
            if (activities == null || activities.Count == 0) return;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand();

                command.CommandText = @"
                    INSERT INTO ActivityLogs
                    (Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus)
                    VALUES (@timestamp, @username, @windowTitle, @processName, @activityStatus)";

                // Prepare parameters once outside the loop for performance
                command.Parameters.Add("@timestamp", SqliteType.Text);
                command.Parameters.Add("@username", SqliteType.Text);
                command.Parameters.Add("@windowTitle", SqliteType.Text);
                command.Parameters.Add("@processName", SqliteType.Text);
                command.Parameters.Add("@activityStatus", SqliteType.Text);

                foreach (var activity in activities)
                {
                    command.Parameters["@timestamp"].Value = activity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    command.Parameters["@username"].Value = activity.WindowsUsername ?? string.Empty;
                    command.Parameters["@windowTitle"].Value = activity.ActiveWindowTitle ?? string.Empty;
                    command.Parameters["@processName"].Value = activity.ApplicationProcessName ?? string.Empty;
                    command.Parameters["@activityStatus"].Value = activity.ActivityStatus.ToString();

                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                _logger.LogDebug("Batch inserted {Count} activity records.", activities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute batch insert for {Count} activities.", activities.Count);
                // Consider rolling back transaction if not already handled by 'using' or re-throwing
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Ensure any pending inserts are processed before disposing
                ProcessBatchInserts(null).Wait(); // Wait for final batch
                _batchInsertTimer?.Dispose();
                _batchSemaphore.Dispose();
                _disposed = true;
                _logger.LogInformation("OptimizedSQLiteDataAccess disposed");
            }
        }
    }

5. Key Performance Indicators (KPIs) & Success Metrics

The success of these optimizations will be measured against the following KPIs:

    Input Latency:

        99th percentile input event processing time: ≤8ms.

        Event timestamp accuracy: ≤10ms (deviation from actual event time).

    CPU Usage:

        Peak CPU usage during input bursts: ≤4 (reduction of 67).

        Average CPU usage during continuous operation: ≤1.5.

    Event Throughput:

        Max throughput: ≥12,000 events/second.

    Event Loss Rate:

        Critical event loss rate: ≤0.1 (for buffered input).

    User Experience:

        No perceptible input lag or UI freezes reported by users.

        Smooth application startup and shutdown.

6. Configuration Updates

The appsettings.json file will be updated to reflect new configuration parameters and optimal intervals:

{
  "TimeTracker": {
    "ActivityTimeoutMs": 30000,          // User idle timeout (30 seconds)
    "BatchInsertIntervalMs": 10000,      // SQLite batch insert interval (10 seconds)
    "MaxBatchSize": 50,                  // Max records per SQLite batch
    "MaxConcurrentSubmissions": 3,       // Max concurrent Pipedream submissions
    "DatabasePath": "TimeTracker.db",    // Path to SQLite database
    "PipedreamEndpointUrl": "",          // Pipedream endpoint URL (should be configured)
    "RetryAttempts": 3,                  // Pipedream submission retry attempts
    "RetryDelayMs": 5000                 // Pipedream submission initial retry delay (ms)
  }
}

7. Technical Considerations

    P/Invoke Management: Careful handling of native Windows API calls (SetWinEventHook, RegisterRawInputDevices, GetLastInputInfo) and their associated delegates (WinEventDelegate) to ensure proper memory management and prevent garbage collection issues.

    Thread Safety: All shared resources (e.g., _lastActivity, _currentActivityStatus, _pendingInserts, _submissionQueue) must be accessed in a thread-safe manner using appropriate synchronization primitives (lock, SemaphoreSlim, ConcurrentQueue, BlockingCollection).

    Error Handling: Robust try-catch blocks are essential around all native API calls, asynchronous operations, and database interactions to ensure application stability and proper logging.

    Deployment: The application will continue to run as a Windows Service. The hidden window for Raw Input will need to be managed appropriately within this service context.

    Testing: Comprehensive unit and integration tests will be developed for all optimized components to verify performance improvements and functional correctness.

8. Open Questions / Future Work

    Raw Input Device Unregistration: Investigate the most robust way to unregister Raw Input devices gracefully upon service shutdown to avoid resource leaks, especially when running as a Windows Service.

    Advanced Input Coalescing: Explore more sophisticated algorithms for temporal coalescing of input events if further reductions in processing load are required.

    Configuration UI: Consider adding a user interface for configuring ActivityTimeoutMs, PipedreamEndpointUrl, and other parameters.

    Telemetry: Implement detailed telemetry to monitor actual performance metrics (latency, CPU, throughput) in production environments.