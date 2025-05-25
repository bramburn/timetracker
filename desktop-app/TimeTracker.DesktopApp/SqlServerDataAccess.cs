using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// SQL Server Express data access implementation with optimized bulk operations.
/// Provides significantly better performance than SQLite for concurrent operations.
/// </summary>
public class SqlServerDataAccess : IDataAccess
{
    private readonly ILogger<SqlServerDataAccess> _logger;
    private readonly string _connectionString;
    private readonly ConcurrentQueue<ActivityDataModel> _pendingInserts = new();
    private readonly System.Threading.Timer _batchInsertTimer;
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    private readonly int _maxBatchSize;
    private readonly int _batchInsertIntervalMs;
    private readonly bool _enableBulkOperations;
    private bool _disposed = false;

    public SqlServerDataAccess(IConfiguration configuration, ILogger<SqlServerDataAccess> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("DefaultConnection connection string is required");

        _maxBatchSize = configuration.GetValue<int>("TimeTracker:MaxBatchSize", 100);
        _batchInsertIntervalMs = configuration.GetValue<int>("TimeTracker:BatchInsertIntervalMs", 30000);
        _enableBulkOperations = configuration.GetValue<bool>("TimeTracker:EnableBulkOperations", true);

        _logger.LogInformation("SqlServerDataAccess initialized with batch size: {BatchSize}, interval: {Interval}ms, bulk operations: {BulkEnabled}",
            _maxBatchSize, _batchInsertIntervalMs, _enableBulkOperations);

        // Initialize database
        InitializeDatabaseAsync().GetAwaiter().GetResult();

        // Start batch processing timer
        _batchInsertTimer = new System.Threading.Timer(async _ => await ProcessBatchInserts(), null,
            TimeSpan.FromMilliseconds(_batchInsertIntervalMs),
            TimeSpan.FromMilliseconds(_batchInsertIntervalMs));
    }

    /// <summary>
    /// Initializes the database and creates necessary tables
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var createTableCommand = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActivityLogs' AND xtype='U')
                BEGIN
                    CREATE TABLE ActivityLogs (
                        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        Timestamp DATETIME2(3) NOT NULL,
                        WindowsUsername NVARCHAR(256) NOT NULL,
                        ActiveWindowTitle NVARCHAR(512) NOT NULL,
                        ApplicationProcessName NVARCHAR(256) NOT NULL,
                        ActivityStatus NVARCHAR(20) NOT NULL,
                        IsSynced BIT DEFAULT 0,
                        BatchId UNIQUEIDENTIFIER NULL,
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
                    );

                    CREATE NONCLUSTERED INDEX IX_ActivityLogs_Timestamp ON ActivityLogs(Timestamp DESC);
                    CREATE NONCLUSTERED INDEX IX_ActivityLogs_Sync ON ActivityLogs(IsSynced, BatchId) WHERE IsSynced = 0;
                    CREATE NONCLUSTERED INDEX IX_ActivityLogs_Username ON ActivityLogs(WindowsUsername);
                    CREATE NONCLUSTERED INDEX IX_ActivityLogs_Process ON ActivityLogs(ApplicationProcessName);
                END", connection);

            await createTableCommand.ExecuteNonQueryAsync();
            _logger.LogInformation("SQL Server database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQL Server database");
            throw;
        }
    }

    /// <summary>
    /// Inserts activity data by enqueuing it for batch processing
    /// </summary>
    public Task<bool> InsertActivityAsync(ActivityDataModel activityData)
    {
        if (_disposed)
            return Task.FromResult(false);

        try
        {
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
            _logger.LogError(ex, "Failed to enqueue activity data");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Processes pending inserts in batches using bulk operations for optimal performance
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
                if (_enableBulkOperations && batch.Count > 10)
                {
                    await ExecuteBulkInsert(batch);
                }
                else
                {
                    await ExecuteBatchInsert(batch);
                }
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
    /// Executes bulk insert using SqlBulkCopy for maximum performance
    /// </summary>
    private async Task ExecuteBulkInsert(List<ActivityDataModel> batch)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "ActivityLogs",
                BatchSize = batch.Count,
                BulkCopyTimeout = 30
            };

            // Map columns
            bulkCopy.ColumnMappings.Add("Timestamp", "Timestamp");
            bulkCopy.ColumnMappings.Add("WindowsUsername", "WindowsUsername");
            bulkCopy.ColumnMappings.Add("ActiveWindowTitle", "ActiveWindowTitle");
            bulkCopy.ColumnMappings.Add("ApplicationProcessName", "ApplicationProcessName");
            bulkCopy.ColumnMappings.Add("ActivityStatus", "ActivityStatus");
            bulkCopy.ColumnMappings.Add("IsSynced", "IsSynced");
            bulkCopy.ColumnMappings.Add("BatchId", "BatchId");

            var dataTable = ConvertToDataTable(batch);
            await bulkCopy.WriteToServerAsync(dataTable);

            _logger.LogDebug("Bulk inserted {Count} records using SqlBulkCopy", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute bulk insert for {Count} records", batch.Count);
            // Fallback to regular batch insert
            await ExecuteBatchInsert(batch);
        }
    }

    /// <summary>
    /// Converts activity data to DataTable for bulk operations
    /// </summary>
    private DataTable ConvertToDataTable(List<ActivityDataModel> activities)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Timestamp", typeof(DateTime));
        dataTable.Columns.Add("WindowsUsername", typeof(string));
        dataTable.Columns.Add("ActiveWindowTitle", typeof(string));
        dataTable.Columns.Add("ApplicationProcessName", typeof(string));
        dataTable.Columns.Add("ActivityStatus", typeof(string));
        dataTable.Columns.Add("IsSynced", typeof(bool));
        dataTable.Columns.Add("BatchId", typeof(Guid));

        foreach (var activity in activities)
        {
            dataTable.Rows.Add(
                activity.Timestamp,
                activity.WindowsUsername ?? string.Empty,
                activity.ActiveWindowTitle ?? string.Empty,
                activity.ApplicationProcessName ?? string.Empty,
                activity.ActivityStatus.ToString(),
                activity.IsSynced,
                activity.BatchId ?? (object)DBNull.Value
            );
        }

        return dataTable;
    }

    /// <summary>
    /// Executes a batch insert operation using parameterized queries
    /// </summary>
    private async Task ExecuteBatchInsert(List<ActivityDataModel> batch)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(@"
                INSERT INTO ActivityLogs
                (Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus, IsSynced, BatchId)
                VALUES (@timestamp, @username, @windowTitle, @processName, @activityStatus, @isSynced, @batchId)",
                connection, transaction);

            // Add parameters
            command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
            command.Parameters.Add("@username", SqlDbType.NVarChar, 256);
            command.Parameters.Add("@windowTitle", SqlDbType.NVarChar, 512);
            command.Parameters.Add("@processName", SqlDbType.NVarChar, 256);
            command.Parameters.Add("@activityStatus", SqlDbType.NVarChar, 20);
            command.Parameters.Add("@isSynced", SqlDbType.Bit);
            command.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier);

            foreach (var activity in batch)
            {
                command.Parameters["@timestamp"].Value = activity.Timestamp;
                command.Parameters["@username"].Value = activity.WindowsUsername ?? string.Empty;
                command.Parameters["@windowTitle"].Value = activity.ActiveWindowTitle ?? string.Empty;
                command.Parameters["@processName"].Value = activity.ApplicationProcessName ?? string.Empty;
                command.Parameters["@activityStatus"].Value = activity.ActivityStatus.ToString();
                command.Parameters["@isSynced"].Value = activity.IsSynced;
                command.Parameters["@batchId"].Value = activity.BatchId ?? (object)DBNull.Value;

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogDebug("Successfully inserted batch of {Count} activity records", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch insert for {Count} records", batch.Count);
            throw;
        }
    }

    public async Task<List<ActivityDataModel>> GetRecentActivitiesAsync(int count = 10)
    {
        var activities = new List<ActivityDataModel>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@count) Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus
                FROM ActivityLogs
                ORDER BY Timestamp DESC", connection);

            command.Parameters.AddWithValue("@count", count);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                activities.Add(new ActivityDataModel
                {
                    Timestamp = reader.GetDateTime("Timestamp"),
                    WindowsUsername = reader.GetString("WindowsUsername"),
                    ActiveWindowTitle = reader.GetString("ActiveWindowTitle"),
                    ApplicationProcessName = reader.GetString("ApplicationProcessName"),
                    ActivityStatus = Enum.Parse<ActivityStatus>(reader.GetString("ActivityStatus"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent activities");
        }

        return activities;
    }

    public async Task<int> GetActivityCountAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("SELECT COUNT(*) FROM ActivityLogs", connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity count");
            return 0;
        }
    }

    public async Task<List<ActivityDataModel>> GetUnsyncedActivitiesAsync(int maxCount = 100)
    {
        var activities = new List<ActivityDataModel>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@maxCount) Id, Timestamp, WindowsUsername, ActiveWindowTitle,
                       ApplicationProcessName, ActivityStatus, IsSynced, BatchId
                FROM ActivityLogs
                WHERE IsSynced = 0
                ORDER BY Timestamp ASC", connection);

            command.Parameters.AddWithValue("@maxCount", maxCount);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var activity = new ActivityDataModel
                {
                    Timestamp = reader.GetDateTime("Timestamp"),
                    WindowsUsername = reader.GetString("WindowsUsername"),
                    ActiveWindowTitle = reader.GetString("ActiveWindowTitle"),
                    ApplicationProcessName = reader.GetString("ApplicationProcessName"),
                    ActivityStatus = Enum.Parse<ActivityStatus>(reader.GetString("ActivityStatus")),
                    IsSynced = reader.GetBoolean("IsSynced")
                };

                if (!reader.IsDBNull("BatchId"))
                {
                    activity.BatchId = reader.GetGuid("BatchId");
                }

                activities.Add(activity);
            }

            _logger.LogDebug("Retrieved {Count} unsynced activities", activities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unsynced activities");
        }

        return activities;
    }

    public async Task<bool> MarkActivitiesAsSyncedAsync(List<ActivityDataModel> records, Guid batchId)
    {
        if (!records.Any())
            return true;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(@"
                UPDATE ActivityLogs
                SET IsSynced = 1, BatchId = @batchId
                WHERE Timestamp = @timestamp
                  AND WindowsUsername = @username
                  AND ActiveWindowTitle = @windowTitle
                  AND ApplicationProcessName = @processName", connection, transaction);

            command.Parameters.Add("@batchId", SqlDbType.UniqueIdentifier);
            command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
            command.Parameters.Add("@username", SqlDbType.NVarChar, 256);
            command.Parameters.Add("@windowTitle", SqlDbType.NVarChar, 512);
            command.Parameters.Add("@processName", SqlDbType.NVarChar, 256);

            int updatedCount = 0;
            foreach (var record in records)
            {
                command.Parameters["@batchId"].Value = batchId;
                command.Parameters["@timestamp"].Value = record.Timestamp;
                command.Parameters["@username"].Value = record.WindowsUsername ?? string.Empty;
                command.Parameters["@windowTitle"].Value = record.ActiveWindowTitle ?? string.Empty;
                command.Parameters["@processName"].Value = record.ApplicationProcessName ?? string.Empty;

                var rowsAffected = await command.ExecuteNonQueryAsync();
                updatedCount += rowsAffected;

                // Update the record object to reflect the changes
                record.IsSynced = true;
                record.BatchId = batchId;
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Marked {UpdatedCount} activities as synced with batch ID: {BatchId}", updatedCount, batchId);
            return updatedCount == records.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark activities as synced for batch ID: {BatchId}", batchId);
            return false;
        }
    }

    public async Task<bool> DeleteSyncedRecordsByBatchIdAsync(Guid batchId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(@"
                DELETE FROM ActivityLogs
                WHERE BatchId = @batchId AND IsSynced = 1", connection, transaction);

            command.Parameters.AddWithValue("@batchId", batchId);

            var deletedCount = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deleted {Count} synced records for batch {BatchId}", deletedCount, batchId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting synced records for batch {BatchId}", batchId);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Process any remaining items before disposing
            if (!_pendingInserts.IsEmpty)
            {
                ProcessBatchInserts().GetAwaiter().GetResult();
            }

            _batchInsertTimer?.Dispose();
            _batchSemaphore?.Dispose();
            _logger.LogInformation("SqlServerDataAccess disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SqlServerDataAccess disposal");
        }
    }
}
