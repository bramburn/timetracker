using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Central orchestrator for activity logging that coordinates between window monitoring,
/// input monitoring, local storage, and data submission components.
/// Receives activity data from monitors and ensures it's stored locally and submitted to Pipedream.
/// </summary>
public class ActivityLogger : IDisposable
{
    private readonly ILogger<ActivityLogger> _logger;
    private readonly IDataAccess _dataAccess;
    private readonly IPipedreamClient _pipedreamClient;
    private readonly IWindowMonitor _windowMonitor;
    private readonly IInputMonitor _inputMonitor;
    private readonly BackgroundTaskQueue _backgroundTaskQueue;
    private readonly SemaphoreSlim _submissionSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _submissionProcessorTask;

    private ActivityDataModel? _currentActivity;
    private bool _disposed = false;

    public ActivityLogger(
        IDataAccess dataAccess,
        IPipedreamClient pipedreamClient,
        IWindowMonitor windowMonitor,
        IInputMonitor inputMonitor,
        BackgroundTaskQueue backgroundTaskQueue,
        IConfiguration configuration,
        ILogger<ActivityLogger> logger)
    {
        _logger = logger;
        _dataAccess = dataAccess;
        _pipedreamClient = pipedreamClient;
        _windowMonitor = windowMonitor;
        _inputMonitor = inputMonitor;
        _backgroundTaskQueue = backgroundTaskQueue;

        // Initialize semaphore for concurrent submissions
        var maxConcurrentSubmissions = configuration.GetValue<int>("TimeTracker:MaxConcurrentSubmissions", 3);
        _submissionSemaphore = new SemaphoreSlim(maxConcurrentSubmissions, maxConcurrentSubmissions);

        // Subscribe to events from monitors
        _windowMonitor.WindowChanged += OnWindowChanged;
        _inputMonitor.ActivityStatusChanged += OnActivityStatusChanged;

        // Start background submission processor
        _submissionProcessorTask = Task.Run(ProcessSubmissionQueue);

        _logger.LogInformation("ActivityLogger initialized with {MaxConcurrentSubmissions} max concurrent submissions", maxConcurrentSubmissions);
    }

    /// <summary>
    /// Starts the activity logging process
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed ActivityLogger");
            return;
        }

        try
        {
            _logger.LogInformation("Starting activity logging...");

            // Start monitoring components
            _windowMonitor.Start();
            _inputMonitor.Start();

            // Test Pipedream connection
            var connectionTest = await _pipedreamClient.TestConnectionAsync();
            _logger.LogInformation("Pipedream connection test: {Result}", connectionTest ? "Success" : "Failed");

            // Log initial activity if available
            var initialActivity = _windowMonitor.GetCurrentActivity();
            if (initialActivity != null)
            {
                initialActivity.ActivityStatus = _inputMonitor.GetCurrentActivityStatus();
                await LogActivityAsync(initialActivity);
            }

            _logger.LogInformation("Activity logging started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start activity logging");
            throw;
        }
    }

    /// <summary>
    /// Stops the activity logging process
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            _logger.LogInformation("Stopping activity logging...");

            _windowMonitor.Stop();
            _inputMonitor.Stop();

            _logger.LogInformation("Activity logging stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping activity logging");
        }
    }

    /// <summary>
    /// Event handler for window changes detected by WindowMonitor
    /// </summary>
    /// <param name="activityData">The new window activity data</param>
    private async void OnWindowChanged(ActivityDataModel? activityData)
    {
        try
        {
            if (activityData != null)
            {
                // Update activity status from InputMonitor
                activityData.ActivityStatus = _inputMonitor.GetCurrentActivityStatus();

                // Log the activity
                await LogActivityAsync(activityData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling window change event");
        }
    }

    /// <summary>
    /// Event handler for activity status changes detected by InputMonitor
    /// </summary>
    /// <param name="newStatus">The new activity status</param>
    private async void OnActivityStatusChanged(ActivityStatus newStatus)
    {
        try
        {
            // If we have current activity data, update its status and log
            if (_currentActivity != null)
            {
                var updatedActivity = _currentActivity.Clone();
                updatedActivity.ActivityStatus = newStatus;
                updatedActivity.Timestamp = DateTime.UtcNow;

                await LogActivityAsync(updatedActivity);
            }
            else
            {
                // No current window activity, try to get it
                var windowActivity = _windowMonitor.GetCurrentActivity();
                if (windowActivity != null)
                {
                    windowActivity.ActivityStatus = newStatus;
                    await LogActivityAsync(windowActivity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling activity status change event");
        }
    }

    /// <summary>
    /// Logs activity data to local storage for batch processing
    /// </summary>
    /// <param name="activityData">The activity data to log</param>
    private async Task LogActivityAsync(ActivityDataModel activityData)
    {
        if (_disposed) return;

        try
        {
            _logger.LogDebug("Logging activity: {Activity}", activityData.ToString());

            // Update current activity
            _currentActivity = activityData.Clone();

            // Store locally for batch processing (BatchProcessor will handle Pipedream submission)
            var localStorageSuccess = await _dataAccess.InsertActivityAsync(activityData);
            if (!localStorageSuccess)
            {
                _logger.LogError("Failed to store activity data locally: {Activity}", activityData.ToString());
            }
            else
            {
                _logger.LogDebug("Successfully stored activity data locally: {Activity}", activityData.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging activity data: {Activity}", activityData.ToString());
        }
    }

    /// <summary>
    /// Gets the current activity data
    /// </summary>
    /// <returns>Current activity data or null if no activity has been logged</returns>
    public ActivityDataModel? GetCurrentActivity()
    {
        return _currentActivity?.Clone();
    }

    /// <summary>
    /// Gets recent activity records from the database
    /// </summary>
    /// <param name="count">Number of recent records to retrieve</param>
    /// <returns>List of recent activity records</returns>
    public async Task<List<ActivityDataModel>> GetRecentActivitiesAsync(int count = 10)
    {
        try
        {
            return await _dataAccess.GetRecentActivitiesAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activities");
            return new List<ActivityDataModel>();
        }
    }

    /// <summary>
    /// Gets the total count of logged activities
    /// </summary>
    /// <returns>Total number of activity records</returns>
    public async Task<long> GetActivityCountAsync()
    {
        try
        {
            return await _dataAccess.GetActivityCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity count");
            return 0;
        }
    }

    /// <summary>
    /// Gets status information about the logging components
    /// </summary>
    /// <returns>Status information string</returns>
    public string GetStatusInfo()
    {
        var timeSinceLastInput = _inputMonitor.GetTimeSinceLastInput();
        var currentStatus = _inputMonitor.GetCurrentActivityStatus();
        var pipedreamStatus = _pipedreamClient.GetConfigurationStatus();
        var queueCount = _backgroundTaskQueue.Count;

        return $"Current Status: {currentStatus}, " +
               $"Time since last input: {(timeSinceLastInput == TimeSpan.MaxValue ? "Never" : timeSinceLastInput.ToString(@"hh\:mm\:ss"))}, " +
               $"Pipedream: {pipedreamStatus}, " +
               $"Pending submissions: {queueCount}";
    }

    /// <summary>
    /// Background task that processes the submission queue
    /// </summary>
    private async Task ProcessSubmissionQueue()
    {
        _logger.LogInformation("Background submission processor started");

        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var workItem = await _backgroundTaskQueue.DequeueAsync(_cancellationTokenSource.Token);
                if (workItem != null)
                {
                    try
                    {
                        await workItem(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing background work item");
                    }
                }
                else
                {
                    // Queue is completed
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background submission processor");
        }

        _logger.LogInformation("Background submission processor stopped");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                Stop();

                // Signal cancellation and complete the background queue
                _cancellationTokenSource.Cancel();
                _backgroundTaskQueue.CompleteAdding();

                // Wait for the submission processor to complete (with timeout)
                if (!_submissionProcessorTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    _logger.LogWarning("Background submission processor did not complete within timeout");
                }

                // Unsubscribe from events
                _windowMonitor.WindowChanged -= OnWindowChanged;
                _inputMonitor.ActivityStatusChanged -= OnActivityStatusChanged;

                // Dispose resources
                _submissionSemaphore?.Dispose();
                _backgroundTaskQueue?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInformation("ActivityLogger disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ActivityLogger disposal");
            }
        }
    }
}
