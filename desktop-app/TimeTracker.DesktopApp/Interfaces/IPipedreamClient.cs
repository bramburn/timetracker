namespace TimeTracker.DesktopApp.Interfaces;

/// <summary>
/// Interface for Pipedream client operations
/// </summary>
public interface IPipedreamClient : IDisposable
{
    /// <summary>
    /// Submits activity data to Pipedream endpoint
    /// </summary>
    /// <param name="activityData">The activity data to submit</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SubmitActivityDataAsync(ActivityDataModel activityData);

    /// <summary>
    /// Submits a batch of activity data to Pipedream endpoint as a JSON array
    /// </summary>
    /// <param name="activityDataBatch">The batch of activity data to submit</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SubmitBatchDataAsync(IEnumerable<ActivityDataModel> activityDataBatch);

    /// <summary>
    /// Tests the connection to Pipedream endpoint
    /// </summary>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Gets the current configuration status
    /// </summary>
    /// <returns>Configuration status string</returns>
    string GetConfigurationStatus();
}
