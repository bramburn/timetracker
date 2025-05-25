using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace TimeTracker.DesktopApp.Tests.TestHelpers;

/// <summary>
/// Simple test configuration implementation for unit tests
/// </summary>
public class TestConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _data;

    public TestConfiguration(Dictionary<string, string>? data = null)
    {
        _data = data ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Creates a test configuration with default values for TimeTracker
    /// </summary>
    public static TestConfiguration Create(Dictionary<string, string>? additionalData = null)
    {
        var defaultData = new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=TimeTrackerTestDB;Integrated Security=true;Connection Timeout=30;",
            ["TimeTracker:ActivityTimeoutMs"] = "30000",
            ["TimeTracker:WindowMonitoringIntervalMs"] = "1000",
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.example.com",
            ["TimeTracker:RetryAttempts"] = "3",
            ["TimeTracker:RetryDelayMs"] = "5000",
            ["TimeTracker:MaxBatchSize"] = "50",
            ["TimeTracker:BatchInsertIntervalMs"] = "10000",
            ["TimeTracker:EnableBulkOperations"] = "true"
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                defaultData[kvp.Key] = kvp.Value;
            }
        }

        return new TestConfiguration(defaultData);
    }

    public string? this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : null;
        set => _data[key] = value ?? string.Empty;
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return Enumerable.Empty<IConfigurationSection>();
    }

    public IChangeToken GetReloadToken()
    {
        return new ConfigurationReloadToken();
    }

    public IConfigurationSection GetSection(string key)
    {
        return new TestConfigurationSection(key, _data);
    }

    private class ConfigurationReloadToken : IChangeToken
    {
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new EmptyDisposable();
    }

    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class TestConfigurationSection : IConfigurationSection
    {
        private readonly string _key;
        private readonly Dictionary<string, string> _data;

        public TestConfigurationSection(string key, Dictionary<string, string> data)
        {
            _key = key;
            _data = data;
        }

        public string? this[string key]
        {
            get => _data.TryGetValue($"{_key}:{key}", out var value) ? value : null;
            set => _data[$"{_key}:{key}"] = value ?? string.Empty;
        }

        public string Key => _key;
        public string Path => _key;
        public string? Value
        {
            get => _data.TryGetValue(_key, out var value) ? value : null;
            set => _data[_key] = value ?? string.Empty;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Enumerable.Empty<IConfigurationSection>();
        }

        public IChangeToken GetReloadToken()
        {
            return new ConfigurationReloadToken();
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection($"{_key}:{key}", _data);
        }
    }
}
