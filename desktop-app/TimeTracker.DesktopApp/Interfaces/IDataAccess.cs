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
    Task<long> GetActivityCountAsync();
}
