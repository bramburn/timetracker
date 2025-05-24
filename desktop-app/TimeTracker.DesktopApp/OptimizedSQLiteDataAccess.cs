using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Optimized SQLite data access implementation with batch processing capabilities.
/// Implements efficient batch insertion to reduce database I/O operations and improve performance.
/// </summary>
public class OptimizedSQLiteDataAccess : IDataAccess
{
    private readonly ILogger<OptimizedSQLiteDataAccess> _logger;
    private readonly string _connectionString;
    private readonly ConcurrentQueue<ActivityDataModel> _pendingInserts = new();
    private readonly System.Threading.Timer _batchInsertTimer;
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    private readonly int _maxBatchSize;
    private readonly int _batchInsertIntervalMs;
    private bool _disposed = false;

    public OptimizedSQLiteDataAccess(string databasePath, IConfiguration configuration, ILogger<OptimizedSQLiteDataAccess> logger)
    {
        _logger = logger;
        _connectionString = $"Data Source={databasePath}";

        // Get configuration values
        _maxBatchSize = configuration.GetValue<int>("TimeTracker:MaxBatchSize", 50);
        _batchInsertIntervalMs = configuration.GetValue<int>("TimeTracker:BatchInsertIntervalMs", 10000);

        _logger.LogInformation("OptimizedSQLiteDataAccess initialized with MaxBatchSize: {MaxBatchSize}, BatchInterval: {BatchInterval}ms",
            _maxBatchSize, _batchInsertIntervalMs);

        // Initialize database
        InitializeDatabase();

        // Start batch processing timer
        _batchInsertTimer = new System.Threading.Timer(async _ => await ProcessBatchInserts(), null,
            TimeSpan.FromMilliseconds(_batchInsertIntervalMs),
            TimeSpan.FromMilliseconds(_batchInsertIntervalMs));
    }

    /// <summary>
    /// Initializes the database and creates necessary tables
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ActivityLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    WindowsUsername TEXT NOT NULL,
                    ActiveWindowTitle TEXT NOT NULL,
                    ApplicationProcessName TEXT NOT NULL,
                    ActivityStatus TEXT NOT NULL,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_timestamp ON ActivityLogs(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_username ON ActivityLogs(WindowsUsername);
                CREATE INDEX IF NOT EXISTS idx_process ON ActivityLogs(ApplicationProcessName);
            ";

            createTableCommand.ExecuteNonQuery();
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Inserts activity data by enqueuing it for batch processing
    /// </summary>
    /// <param name="activityData">The activity data to insert</param>
    /// <returns>True (always succeeds as it only enqueues)</returns>
    public Task<bool> InsertActivityAsync(ActivityDataModel activityData)
    {
        if (_disposed)
            return Task.FromResult(false);

        try
        {
            // Enqueue the activity data for batch processing
            _pendingInserts.Enqueue(activityData);
            _logger.LogDebug("Activity data enqueued for batch insertion: {Activity}", activityData.ToString());

            // If we've reached the batch size limit, trigger immediate processing
            if (_pendingInserts.Count >= _maxBatchSize)
            {
                _ = Task.Run(async () => await ProcessBatchInserts());
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing activity data for batch insertion");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Processes pending inserts in batches
    /// </summary>
    private async Task ProcessBatchInserts()
    {
        if (_disposed || _pendingInserts.IsEmpty)
            return;

        await _batchSemaphore.WaitAsync();
        try
        {
            var batch = new List<ActivityDataModel>();

            // Dequeue up to MaxBatchSize items
            while (batch.Count < _maxBatchSize && _pendingInserts.TryDequeue(out var item))
            {
                batch.Add(item);
            }

            if (batch.Count > 0)
            {
                await ExecuteBatchInsert(batch);
                _logger.LogDebug("Processed batch of {Count} activity records", batch.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch inserts");
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    /// <summary>
    /// Executes a batch insert operation using a single transaction
    /// </summary>
    /// <param name="batch">The batch of activity data to insert</param>
    private async Task ExecuteBatchInsert(List<ActivityDataModel> batch)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO ActivityLogs
                (Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus)
                VALUES (@timestamp, @username, @windowTitle, @processName, @activityStatus)
            ";

            // Add parameters
            command.Parameters.Add("@timestamp", SqliteType.Text);
            command.Parameters.Add("@username", SqliteType.Text);
            command.Parameters.Add("@windowTitle", SqliteType.Text);
            command.Parameters.Add("@processName", SqliteType.Text);
            command.Parameters.Add("@activityStatus", SqliteType.Text);

            foreach (var activity in batch)
            {
                command.Parameters["@timestamp"].Value = activity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters["@username"].Value = activity.WindowsUsername ?? string.Empty;
                command.Parameters["@windowTitle"].Value = activity.ActiveWindowTitle ?? string.Empty;
                command.Parameters["@processName"].Value = activity.ApplicationProcessName ?? string.Empty;
                command.Parameters["@activityStatus"].Value = activity.ActivityStatus.ToString();

                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            _logger.LogDebug("Successfully inserted batch of {Count} activity records", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch insert for {Count} records", batch.Count);
            throw;
        }
    }

    /// <summary>
    /// Retrieves recent activity records from the database
    /// </summary>
    /// <param name="count">Number of recent records to retrieve</param>
    /// <returns>List of recent activity records</returns>
    public async Task<List<ActivityDataModel>> GetRecentActivitiesAsync(int count = 10)
    {
        var activities = new List<ActivityDataModel>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus
                FROM ActivityLogs
                ORDER BY Timestamp DESC
                LIMIT @count
            ";
            command.Parameters.AddWithValue("@count", count);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var activity = new ActivityDataModel
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    WindowsUsername = reader.GetString(1),
                    ActiveWindowTitle = reader.GetString(2),
                    ApplicationProcessName = reader.GetString(3),
                    ActivityStatus = Enum.Parse<ActivityStatus>(reader.GetString(4))
                };
                activities.Add(activity);
            }

            _logger.LogDebug("Retrieved {Count} recent activities", activities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activities");
        }

        return activities;
    }

    /// <summary>
    /// Gets the total count of activity records in the database
    /// </summary>
    /// <returns>Total number of activity records</returns>
    public async Task<long> GetActivityCountAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ActivityLogs";

            var result = await command.ExecuteScalarAsync();
            var count = Convert.ToInt64(result);

            _logger.LogDebug("Total activity count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity count");
            return 0;
        }
    }

    /// <summary>
    /// Disposes resources and ensures all pending inserts are processed
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Stop the timer first
                _batchInsertTimer?.Dispose();

                // Process any remaining pending inserts BEFORE setting disposed flag
                if (!_pendingInserts.IsEmpty)
                {
                    _logger.LogInformation("Processing {Count} remaining pending inserts during disposal", _pendingInserts.Count);
                    ProcessBatchInserts().Wait(TimeSpan.FromSeconds(30)); // Wait up to 30 seconds
                }

                // Now set the disposed flag
                _disposed = true;

                _batchSemaphore?.Dispose();
                _logger.LogInformation("OptimizedSQLiteDataAccess disposed successfully");
            }
            catch (Exception ex)
            {
                _disposed = true; // Ensure we mark as disposed even if there's an error
                _logger.LogError(ex, "Error during OptimizedSQLiteDataAccess disposal");
            }
        }
    }
}
