using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Service responsible for batch processing and uploading activity data to Pipedream.
/// Collects unsynced records at regular intervals, submits them as batches, and manages local cleanup.
/// </summary>
public class BatchProcessor : IDisposable
{
    private readonly ILogger<BatchProcessor> _logger;
    private readonly IDataAccess _dataAccess;
    private readonly IPipedreamClient _pipedreamClient;
    private readonly int _batchIntervalMinutes;
    private readonly int _maxBatchSize;
    private readonly System.Threading.Timer _batchTimer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed = false;

    public BatchProcessor(
        ILogger<BatchProcessor> logger,
        IDataAccess dataAccess,
        IPipedreamClient pipedreamClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _dataAccess = dataAccess;
        _pipedreamClient = pipedreamClient;

        // Load configuration
        _batchIntervalMinutes = configuration.GetValue<int>("TimeTracker:PipedreamBatchIntervalMinutes", 1);
        _maxBatchSize = configuration.GetValue<int>("TimeTracker:MaxBatchSize", 50);

        // Initialize timer (but don't start it yet)
        var intervalMs = TimeSpan.FromMinutes(_batchIntervalMinutes).TotalMilliseconds;
        _batchTimer = new System.Threading.Timer(OnBatchTimerElapsed, null, Timeout.Infinite, (int)intervalMs);

        _logger.LogInformation("BatchProcessor initialized with interval: {IntervalMinutes} minutes, max batch size: {MaxBatchSize}",
            _batchIntervalMinutes, _maxBatchSize);
    }

    /// <summary>
    /// Starts the batch processor
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed BatchProcessor");
            return;
        }

        _logger.LogInformation("BatchProcessor starting...");

        // Wait a short time before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

        // Start the timer
        var intervalMs = TimeSpan.FromMinutes(_batchIntervalMinutes).TotalMilliseconds;
        _batchTimer.Change((int)intervalMs, (int)intervalMs);

        _logger.LogInformation("BatchProcessor started");
    }

    /// <summary>
    /// Timer callback for batch processing
    /// </summary>
    private async void OnBatchTimerElapsed(object? state)
    {
        if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
            return;

        try
        {
            await ProcessBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch processing");
        }
    }

    /// <summary>
    /// Processes a batch of unsynced records
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessBatchAsync()
    {
        try
        {
            // Get unsynced records from the database
            var unsyncedRecords = await _dataAccess.GetUnsyncedActivitiesAsync(_maxBatchSize);

            if (unsyncedRecords.Count == 0)
            {
                _logger.LogDebug("No unsynced records found for batch processing");
                return;
            }

            _logger.LogInformation("Processing batch of {Count} unsynced records", unsyncedRecords.Count);

            // Generate a unique batch ID
            var batchId = Guid.NewGuid();

            // Mark records as part of this batch
            var updateSuccess = await _dataAccess.MarkActivitiesAsSyncedAsync(unsyncedRecords, batchId);
            if (!updateSuccess)
            {
                _logger.LogError("Failed to update records for batch {BatchId}", batchId);
                return;
            }

            // Submit batch to Pipedream
            var submissionSuccess = await _pipedreamClient.SubmitBatchDataAsync(unsyncedRecords);

            if (submissionSuccess)
            {
                // Delete successfully synced records
                var deletionSuccess = await _dataAccess.DeleteSyncedRecordsByBatchIdAsync(batchId);

                if (deletionSuccess)
                {
                    _logger.LogInformation("Successfully processed and cleaned up batch {BatchId} with {Count} records",
                        batchId, unsyncedRecords.Count);
                }
                else
                {
                    _logger.LogWarning("Batch {BatchId} was submitted successfully but local cleanup failed. Records remain marked as synced.",
                        batchId);
                }
            }
            else
            {
                _logger.LogWarning("Failed to submit batch {BatchId} to Pipedream. Records remain in local database for retry.",
                    batchId);

                // Optionally, we could reset the IsSynced flag here to allow retry
                // For now, we'll leave them marked as synced with the BatchId for manual intervention if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch processing");
        }
    }

    /// <summary>
    /// Stops the batch processor
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        _logger.LogInformation("BatchProcessor stopping...");

        // Stop the timer
        _batchTimer.Change(Timeout.Infinite, Timeout.Infinite);

        // Signal cancellation
        _cancellationTokenSource.Cancel();

        // Process any remaining batch before stopping
        try
        {
            await ProcessBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final batch processing on shutdown");
        }

        _logger.LogInformation("BatchProcessor stopped gracefully");
    }

    /// <summary>
    /// Disposes of the batch processor and its resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                _batchTimer?.Dispose();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                _logger.LogInformation("BatchProcessor disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing BatchProcessor");
            }
        }
    }
}
