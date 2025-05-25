namespace TimeTracker.DesktopApp.Interfaces;

/// <summary>
/// Interface for data access operations
/// </summary>
public interface IDataAccess : IDisposable
{
    /// <summary>
    /// Inserts activity data into storage
    /// </summary>
    /// <param name="activityData">The activity data to insert</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> InsertActivityAsync(ActivityDataModel activityData);

    /// <summary>
    /// Retrieves recent activity records
    /// </summary>
    /// <param name="count">Number of recent records to retrieve</param>
    /// <returns>List of recent activity records</returns>
    Task<List<ActivityDataModel>> GetRecentActivitiesAsync(int count = 10);

    /// <summary>
    /// Gets the total count of activity records
    /// </summary>
    /// <returns>Total number of activity records</returns>
    Task<int> GetActivityCountAsync();

    /// <summary>
    /// Retrieves unsynced activity records from the database for batch processing
    /// </summary>
    /// <param name="maxCount">Maximum number of records to retrieve</param>
    /// <returns>List of unsynced activity records</returns>
    Task<List<ActivityDataModel>> GetUnsyncedActivitiesAsync(int maxCount = 100);

    /// <summary>
    /// Marks a list of activity records as synced with the specified batch ID
    /// </summary>
    /// <param name="records">The activity records to mark as synced</param>
    /// <param name="batchId">The batch ID to associate with the synced records</param>
    /// <returns>True if all records were successfully marked as synced</returns>
    Task<bool> MarkActivitiesAsSyncedAsync(List<ActivityDataModel> records, Guid batchId);

    /// <summary>
    /// Deletes synced records by batch ID after successful upload
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteSyncedRecordsByBatchIdAsync(Guid batchId);
}
