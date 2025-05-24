using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Handles all interactions with the local SQLite database for activity logging.
/// Manages database creation, schema initialization, and data insertion operations.
/// </summary>
public class SQLiteDataAccess : IDataAccess
{
    private readonly string _connectionString;
    private readonly ILogger<SQLiteDataAccess> _logger;
    private bool _disposed = false;

    public SQLiteDataAccess(string databasePath, ILogger<SQLiteDataAccess> logger)
    {
        _logger = logger;

        // Ensure the database directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath}";

        // Initialize database schema
        InitializeDatabase();
    }

    /// <summary>
    /// Creates the database file and schema if they don't exist
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
    /// Inserts a new activity record into the database
    /// </summary>
    /// <param name="activityData">The activity data to insert</param>
    /// <returns>True if insertion was successful, false otherwise</returns>
    public async Task<bool> InsertActivityAsync(ActivityDataModel activityData)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to insert data into disposed SQLiteDataAccess");
            return false;
        }

        if (activityData == null)
        {
            _logger.LogWarning("Attempted to insert null activity data");
            return false;
        }

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ActivityLogs
                (Timestamp, WindowsUsername, ActiveWindowTitle, ApplicationProcessName, ActivityStatus)
                VALUES (@timestamp, @username, @windowTitle, @processName, @activityStatus)
            ";

            command.Parameters.AddWithValue("@timestamp", activityData.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@username", activityData.WindowsUsername ?? string.Empty);
            command.Parameters.AddWithValue("@windowTitle", activityData.ActiveWindowTitle ?? string.Empty);
            command.Parameters.AddWithValue("@processName", activityData.ApplicationProcessName ?? string.Empty);
            command.Parameters.AddWithValue("@activityStatus", activityData.ActivityStatus.ToString());

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogDebug("Activity data inserted successfully: {ActivityData}", activityData.ToString());
                return true;
            }
            else
            {
                _logger.LogWarning("No rows affected when inserting activity data");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert activity data: {ActivityData}", activityData.ToString());
            return false;
        }
    }

    /// <summary>
    /// Retrieves recent activity records for testing/debugging purposes
    /// </summary>
    /// <param name="count">Number of recent records to retrieve</param>
    /// <returns>List of recent activity records</returns>
    public async Task<List<ActivityDataModel>> GetRecentActivitiesAsync(int count = 10)
    {
        var activities = new List<ActivityDataModel>();

        if (_disposed)
        {
            _logger.LogWarning("Attempted to query disposed SQLiteDataAccess");
            return activities;
        }

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent activities");
        }

        return activities;
    }

    /// <summary>
    /// Gets the total count of activity records in the database
    /// </summary>
    /// <returns>Total number of activity records</returns>
    public async Task<long> GetActivityCountAsync()
    {
        if (_disposed) return 0;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ActivityLogs";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity count");
            return 0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogInformation("SQLiteDataAccess disposed");
        }
    }
}
