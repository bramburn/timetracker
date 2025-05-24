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

    private ActivityDataModel? _currentActivity;
    private bool _disposed = false;

    public ActivityLogger(
        IDataAccess dataAccess,
        IPipedreamClient pipedreamClient,
        IWindowMonitor windowMonitor,
        IInputMonitor inputMonitor,
        ILogger<ActivityLogger> logger)
    {
        _logger = logger;
        _dataAccess = dataAccess;
        _pipedreamClient = pipedreamClient;
        _windowMonitor = windowMonitor;
        _inputMonitor = inputMonitor;

        // Subscribe to events from monitors
        _windowMonitor.WindowChanged += OnWindowChanged;
        _inputMonitor.ActivityStatusChanged += OnActivityStatusChanged;

        _logger.LogInformation("ActivityLogger initialized and event subscriptions established");
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
    private async void OnWindowChanged(ActivityDataModel activityData)
    {
        try
        {
            // Update activity status from InputMonitor
            activityData.ActivityStatus = _inputMonitor.GetCurrentActivityStatus();

            // Log the activity
            await LogActivityAsync(activityData);
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
    /// Logs activity data to both local storage and Pipedream
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

            // Store locally (this should always succeed or we have bigger problems)
            var localStorageSuccess = await _dataAccess.InsertActivityAsync(activityData);
            if (!localStorageSuccess)
            {
                _logger.LogError("Failed to store activity data locally: {Activity}", activityData.ToString());
            }

            // Submit to Pipedream (this can fail without affecting local storage)
            _ = Task.Run(async () =>
            {
                try
                {
                    var submissionSuccess = await _pipedreamClient.SubmitActivityDataAsync(activityData);
                    if (!submissionSuccess)
                    {
                        _logger.LogWarning("Failed to submit activity data to Pipedream: {Activity}", activityData.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting activity data to Pipedream");
                }
            });
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

        return $"Current Status: {currentStatus}, " +
               $"Time since last input: {(timeSinceLastInput == TimeSpan.MaxValue ? "Never" : timeSinceLastInput.ToString(@"hh\:mm\:ss"))}, " +
               $"Pipedream: {pipedreamStatus}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();

            // Unsubscribe from events
            _windowMonitor.WindowChanged -= OnWindowChanged;
            _inputMonitor.ActivityStatusChanged -= OnActivityStatusChanged;

            _disposed = true;
            _logger.LogInformation("ActivityLogger disposed");
        }
    }
}
