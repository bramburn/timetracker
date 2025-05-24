namespace TimeTracker.DesktopApp;

/// <summary>
/// Represents a captured input event for the buffered queue processing.
/// Contains minimal data needed for activity detection and debouncing.
/// </summary>
internal class InputEvent
{
    /// <summary>
    /// The timestamp when the input event was captured
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// The type of input that was detected
    /// </summary>
    public InputType Type { get; }

    /// <summary>
    /// Creates a new InputEvent instance
    /// </summary>
    /// <param name="timestamp">The timestamp of the input event</param>
    /// <param name="type">The type of input detected</param>
    public InputEvent(DateTime timestamp, InputType type)
    {
        Timestamp = timestamp;
        Type = type;
    }

    public override string ToString()
    {
        return $"InputEvent: {Type} at {Timestamp:yyyy-MM-dd HH:mm:ss.fff}";
    }
}

/// <summary>
/// Enumeration of input types for activity monitoring
/// </summary>
internal enum InputType
{
    /// <summary>
    /// Keyboard input detected
    /// </summary>
    Keyboard,

    /// <summary>
    /// Mouse input detected
    /// </summary>
    Mouse,

    /// <summary>
    /// Generic input type when specific type is not determined
    /// </summary>
    Generic
}
