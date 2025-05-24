using System.Text.Json.Serialization;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Represents the data structure for captured activity records.
/// This model is used throughout the application for data transfer between components
/// and serialization to both SQLite database and JSON for Pipedream submission.
/// </summary>
public class ActivityDataModel
{
    /// <summary>
    /// UTC timestamp when the activity was recorded
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Windows username of the currently logged-in user
    /// </summary>
    [JsonPropertyName("windowsUsername")]
    public string WindowsUsername { get; set; } = string.Empty;

    /// <summary>
    /// Title of the currently active/foreground window
    /// </summary>
    [JsonPropertyName("activeWindowTitle")]
    public string ActiveWindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// Name of the process associated with the active window
    /// </summary>
    [JsonPropertyName("applicationProcessName")]
    public string ApplicationProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Binary activity status indicating user engagement
    /// </summary>
    [JsonPropertyName("activityStatus")]
    public ActivityStatus ActivityStatus { get; set; } = ActivityStatus.Inactive;

    /// <summary>
    /// Handle of the active window (for internal tracking, not serialized)
    /// </summary>
    [JsonIgnore]
    public IntPtr ActiveWindowHandle { get; set; } = IntPtr.Zero;

    /// <summary>
    /// Creates a copy of the current activity data model
    /// </summary>
    /// <returns>A new instance with the same property values</returns>
    public ActivityDataModel Clone()
    {
        return new ActivityDataModel
        {
            Timestamp = this.Timestamp,
            WindowsUsername = this.WindowsUsername,
            ActiveWindowTitle = this.ActiveWindowTitle,
            ApplicationProcessName = this.ApplicationProcessName,
            ActivityStatus = this.ActivityStatus,
            ActiveWindowHandle = this.ActiveWindowHandle
        };
    }

    /// <summary>
    /// Determines if this activity data represents a significant change from another
    /// </summary>
    /// <param name="other">The other activity data to compare against</param>
    /// <returns>True if there are significant differences that warrant logging</returns>
    public bool HasSignificantChanges(ActivityDataModel? other)
    {
        if (other == null) return true;

        return !string.Equals(this.ActiveWindowTitle, other.ActiveWindowTitle, StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(this.ApplicationProcessName, other.ApplicationProcessName, StringComparison.OrdinalIgnoreCase) ||
               this.ActivityStatus != other.ActivityStatus;
    }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {WindowsUsername} - {ApplicationProcessName}: {ActiveWindowTitle} ({ActivityStatus})";
    }
}

/// <summary>
/// Enumeration representing the binary activity status of the user
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityStatus
{
    /// <summary>
    /// User is not actively providing input (no keyboard/mouse activity)
    /// </summary>
    Inactive,

    /// <summary>
    /// User is actively providing input (keyboard/mouse activity detected)
    /// </summary>
    Active
}
