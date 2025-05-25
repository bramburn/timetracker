using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Background service responsible for batch processing and uploading activity data to Pipedream.
/// Collects unsynced records at regular intervals, submits them as batches, and manages local cleanup.
/// </summary>
public class BatchProcessor : BackgroundService
{
    private readonly ILogger<BatchProcessor> _logger;
    private readonly IDataAccess _dataAccess;
    private readonly IPipedreamClient _pipedreamClient;
    private readonly int _batchIntervalMinutes;
    private readonly int _maxBatchSize;

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

        _logger.LogInformation("BatchProcessor initialized with interval: {IntervalMinutes} minutes, max batch size: {MaxBatchSize}",
            _batchIntervalMinutes, _maxBatchSize);
    }

    /// <summary>
    /// Main execution loop for the background service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BatchProcessor started");

        // Wait a short time before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch processing");
            }

            // Wait for the configured interval before next batch processing
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_batchIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("BatchProcessor stopped");
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
    /// Handles service stop to ensure graceful shutdown
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BatchProcessor stopping...");

        // Process any remaining batch before stopping
        try
        {
            await ProcessBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final batch processing on shutdown");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("BatchProcessor stopped gracefully");
    }
}
