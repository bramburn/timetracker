namespace TimeTracker.DesktopApp.Interfaces;

/// <summary>
/// Interface for input monitoring operations
/// </summary>
public interface IInputMonitor : IDisposable
{
    /// <summary>
    /// Event fired when activity status changes
    /// </summary>
    event Action<ActivityStatus> ActivityStatusChanged;

    /// <summary>
    /// Starts input monitoring
    /// </summary>
    void Start();

    /// <summary>
    /// Stops input monitoring
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current activity status
    /// </summary>
    /// <returns>Current activity status</returns>
    ActivityStatus GetCurrentActivityStatus();

    /// <summary>
    /// Gets the time since last input
    /// </summary>
    /// <returns>Time since last input or TimeSpan.MaxValue if never</returns>
    TimeSpan GetTimeSinceLastInput();
}
