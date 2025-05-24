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
