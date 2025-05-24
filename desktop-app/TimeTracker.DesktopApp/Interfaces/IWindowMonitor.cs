namespace TimeTracker.DesktopApp.Interfaces;

/// <summary>
/// Interface for window monitoring operations
/// </summary>
public interface IWindowMonitor : IDisposable
{
    /// <summary>
    /// Event fired when the active window changes
    /// </summary>
    event Action<ActivityDataModel?> WindowChanged;

    /// <summary>
    /// Starts window monitoring
    /// </summary>
    void Start();

    /// <summary>
    /// Stops window monitoring
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current activity data
    /// </summary>
    /// <returns>Current activity data or null if none available</returns>
    ActivityDataModel? GetCurrentActivity();

    /// <summary>
    /// Gets the last recorded activity data
    /// </summary>
    /// <returns>Last activity data or null if none available</returns>
    ActivityDataModel? GetLastActivity();
}
