using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Manages periodic heartbeat transmissions to Pipedream for Phase 1 MVP validation.
/// Sends simple connectivity test messages at regular intervals.
/// </summary>
public class HeartbeatService : IDisposable
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly IPipedreamClient _pipedreamClient;
    private readonly IConfiguration _configuration;
    private readonly System.Threading.Timer _heartbeatTimer;
    private readonly int _intervalMinutes;
    private bool _disposed = false;
    private MainForm? _mainForm;

    public HeartbeatService(
        ILogger<HeartbeatService> logger,
        IPipedreamClient pipedreamClient,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipedreamClient = pipedreamClient ?? throw new ArgumentNullException(nameof(pipedreamClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Get heartbeat interval from configuration (default to 5 minutes)
        _intervalMinutes = _configuration.GetValue<int>("TimeTracker:HeartbeatIntervalMinutes", 5);

        // Initialize timer (but don't start it yet)
        _heartbeatTimer = new System.Threading.Timer(OnHeartbeatTimer, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("HeartbeatService initialized with {Interval} minute interval", _intervalMinutes);
    }

    /// <summary>
    /// Starts the heartbeat service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed HeartbeatService");
            return;
        }

        _logger.LogInformation("Starting HeartbeatService...");

        try
        {
            // Send initial heartbeat immediately
            await SendHeartbeatAsync();

            // Start the timer for periodic heartbeats
            var intervalMs = TimeSpan.FromMinutes(_intervalMinutes).TotalMilliseconds;
            _heartbeatTimer.Change((int)intervalMs, (int)intervalMs);

            _logger.LogInformation("HeartbeatService started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start HeartbeatService");
            throw;
        }
    }

    /// <summary>
    /// Stops the heartbeat service
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return Task.CompletedTask;

        try
        {
            _logger.LogInformation("Stopping HeartbeatService...");

            // Stop the timer
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("HeartbeatService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping HeartbeatService");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the main form reference for UI updates
    /// </summary>
    public void SetMainForm(MainForm mainForm)
    {
        _mainForm = mainForm;
        _logger.LogInformation("Main form reference set in HeartbeatService");
    }

    /// <summary>
    /// Timer callback for periodic heartbeat transmission
    /// </summary>
    private async void OnHeartbeatTimer(object? state)
    {
        if (_disposed) return;

        try
        {
            await SendHeartbeatAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic heartbeat transmission");

            // Update UI if available
            _mainForm?.UpdateHeartbeatStatus($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a heartbeat message to Pipedream
    /// </summary>
    private async Task SendHeartbeatAsync()
    {
        try
        {
            _logger.LogInformation("Sending heartbeat to Pipedream...");

            // Create heartbeat data model
            var heartbeatData = new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow,
                WindowsUsername = Environment.UserName,
                ActiveWindowTitle = "TimeTracker MVP Heartbeat - Desktop App MVP Connectivity Test",
                ApplicationProcessName = "TimeTracker.DesktopApp.MVP",
                ActivityStatus = ActivityStatus.Active
            };

            // Submit to Pipedream
            var success = await _pipedreamClient.SubmitActivityDataAsync(heartbeatData);

            if (success)
            {
                var statusMessage = $"Last sent: {DateTime.Now:HH:mm:ss} - Success";
                _logger.LogInformation("Heartbeat sent successfully at {Time}", DateTime.Now);

                // Update UI if available
                _mainForm?.UpdateHeartbeatStatus(statusMessage);
            }
            else
            {
                var statusMessage = $"Last attempt: {DateTime.Now:HH:mm:ss} - Failed";
                _logger.LogWarning("Heartbeat transmission failed");

                // Update UI if available
                _mainForm?.UpdateHeartbeatStatus(statusMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");

            // Update UI if available
            var statusMessage = $"Last attempt: {DateTime.Now:HH:mm:ss} - Error: {ex.Message}";
            _mainForm?.UpdateHeartbeatStatus(statusMessage);

            throw;
        }
    }

    /// <summary>
    /// Sends a test heartbeat immediately (for testing purposes)
    /// </summary>
    public async Task<bool> SendTestHeartbeatAsync()
    {
        try
        {
            _logger.LogInformation("Sending test heartbeat...");
            await SendHeartbeatAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test heartbeat failed");
            return false;
        }
    }

    /// <summary>
    /// Disposes of the heartbeat service
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                _heartbeatTimer?.Dispose();
                _logger.LogInformation("HeartbeatService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing HeartbeatService");
            }
        }
    }
}
