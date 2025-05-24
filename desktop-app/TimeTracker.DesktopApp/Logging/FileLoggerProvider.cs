using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TimeTracker.DesktopApp;

/// <summary>
/// File logger provider for writing logs to a file
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _lock = new();
    private bool _disposed = false;

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _filePath, _lock));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _loggers.Clear();
        }
    }
}

/// <summary>
/// File logger implementation
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private readonly object _lock;

    public FileLogger(string categoryName, string filePath, object lockObject)
    {
        _categoryName = categoryName;
        _filePath = filePath;
        _lock = lockObject;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            lock (_lock)
            {
                var message = formatter(state, exception);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";

                if (exception != null)
                {
                    logEntry += Environment.NewLine + exception.ToString();
                }

                File.AppendAllText(_filePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Ignore file logging errors to prevent cascading failures
        }
    }
}
