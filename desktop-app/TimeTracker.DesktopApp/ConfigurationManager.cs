using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Manages user-specific configuration settings for the TimeTracker application.
/// Handles reading from appsettings.json and writing to user-specific configuration files.
/// </summary>
public class ConfigurationManager
{
    private readonly ILogger _logger;
    private readonly string _userConfigPath;
    private readonly string _appSettingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set up configuration file paths
        var userDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimeTracker");

        Directory.CreateDirectory(userDataPath);

        _userConfigPath = Path.Combine(userDataPath, "user-settings.json");
        _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        _logger.LogDebug("ConfigurationManager initialized with user config path: {UserConfigPath}", _userConfigPath);
    }

    /// <summary>
    /// Loads configuration from both app settings and user settings
    /// </summary>
    /// <returns>Merged configuration object</returns>
    public UserConfiguration LoadConfiguration()
    {
        try
        {
            // Start with default configuration
            var config = GetDefaultConfiguration();

            // Load app settings if available
            if (File.Exists(_appSettingsPath))
            {
                var appConfig = LoadAppSettings();
                if (appConfig != null)
                {
                    MergeAppSettings(config, appConfig);
                }
            }

            // Load user settings if available (these override app settings)
            if (File.Exists(_userConfigPath))
            {
                var userConfig = LoadUserSettings();
                if (userConfig != null)
                {
                    MergeUserSettings(config, userConfig);
                }
            }

            _logger.LogDebug("Configuration loaded successfully");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration, using defaults");
            return GetDefaultConfiguration();
        }
    }

    /// <summary>
    /// Saves user configuration to the user-specific settings file
    /// </summary>
    /// <param name="configuration">Configuration to save</param>
    public async Task SaveConfigurationAsync(UserConfiguration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, JsonOptions);
            await File.WriteAllTextAsync(_userConfigPath, json);

            _logger.LogInformation("User configuration saved to: {UserConfigPath}", _userConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user configuration");
            throw;
        }
    }

    /// <summary>
    /// Gets the default configuration values
    /// </summary>
    private UserConfiguration GetDefaultConfiguration()
    {
        return new UserConfiguration
        {
            AutoStartWithWindows = false,
            ActivityTimeoutMs = 60000, // 60 seconds
            PipedreamEndpointUrl = "",
            BatchIntervalMs = 30000, // 30 seconds
            MaxConcurrentSubmissions = 3,
            RetryAttempts = 3,
            DebounceThresholdMs = 100,
            FallbackCheckIntervalMs = 10000
        };
    }

    /// <summary>
    /// Loads app settings from appsettings.json
    /// </summary>
    private AppSettingsConfiguration? LoadAppSettings()
    {
        try
        {
            var json = File.ReadAllText(_appSettingsPath);
            var appSettings = JsonSerializer.Deserialize<AppSettingsRoot>(json, JsonOptions);
            return appSettings?.TimeTracker;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load app settings from: {AppSettingsPath}", _appSettingsPath);
            return null;
        }
    }

    /// <summary>
    /// Loads user settings from user-settings.json
    /// </summary>
    private UserConfiguration? LoadUserSettings()
    {
        try
        {
            var json = File.ReadAllText(_userConfigPath);
            return JsonSerializer.Deserialize<UserConfiguration>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user settings from: {UserConfigPath}", _userConfigPath);
            return null;
        }
    }

    /// <summary>
    /// Merges app settings into the configuration
    /// </summary>
    private void MergeAppSettings(UserConfiguration config, AppSettingsConfiguration appConfig)
    {
        if (!string.IsNullOrEmpty(appConfig.PipedreamEndpointUrl))
            config.PipedreamEndpointUrl = appConfig.PipedreamEndpointUrl;

        if (appConfig.ActivityTimeoutMs > 0)
            config.ActivityTimeoutMs = appConfig.ActivityTimeoutMs;

        if (appConfig.BatchIntervalMs > 0)
            config.BatchIntervalMs = appConfig.BatchIntervalMs;

        if (appConfig.MaxConcurrentSubmissions > 0)
            config.MaxConcurrentSubmissions = appConfig.MaxConcurrentSubmissions;

        if (appConfig.RetryAttempts > 0)
            config.RetryAttempts = appConfig.RetryAttempts;

        if (appConfig.DebounceThresholdMs > 0)
            config.DebounceThresholdMs = appConfig.DebounceThresholdMs;

        if (appConfig.FallbackCheckIntervalMs > 0)
            config.FallbackCheckIntervalMs = appConfig.FallbackCheckIntervalMs;
    }

    /// <summary>
    /// Merges user settings into the configuration (user settings take precedence)
    /// </summary>
    private void MergeUserSettings(UserConfiguration config, UserConfiguration userConfig)
    {
        // User settings override everything
        config.AutoStartWithWindows = userConfig.AutoStartWithWindows;

        if (!string.IsNullOrEmpty(userConfig.PipedreamEndpointUrl))
            config.PipedreamEndpointUrl = userConfig.PipedreamEndpointUrl;

        if (userConfig.ActivityTimeoutMs > 0)
            config.ActivityTimeoutMs = userConfig.ActivityTimeoutMs;

        if (userConfig.BatchIntervalMs > 0)
            config.BatchIntervalMs = userConfig.BatchIntervalMs;

        if (userConfig.MaxConcurrentSubmissions > 0)
            config.MaxConcurrentSubmissions = userConfig.MaxConcurrentSubmissions;

        if (userConfig.RetryAttempts > 0)
            config.RetryAttempts = userConfig.RetryAttempts;

        if (userConfig.DebounceThresholdMs > 0)
            config.DebounceThresholdMs = userConfig.DebounceThresholdMs;

        if (userConfig.FallbackCheckIntervalMs > 0)
            config.FallbackCheckIntervalMs = userConfig.FallbackCheckIntervalMs;
    }

    /// <summary>
    /// Deletes the user configuration file
    /// </summary>
    public void ResetUserConfiguration()
    {
        try
        {
            if (File.Exists(_userConfigPath))
            {
                File.Delete(_userConfigPath);
                _logger.LogInformation("User configuration file deleted: {UserConfigPath}", _userConfigPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user configuration file");
            throw;
        }
    }
}

/// <summary>
/// User-specific configuration settings
/// </summary>
public class UserConfiguration
{
    public bool AutoStartWithWindows { get; set; }
    public string PipedreamEndpointUrl { get; set; } = "";
    public int ActivityTimeoutMs { get; set; }
    public int BatchIntervalMs { get; set; }
    public int MaxConcurrentSubmissions { get; set; }
    public int RetryAttempts { get; set; }
    public int DebounceThresholdMs { get; set; }
    public int FallbackCheckIntervalMs { get; set; }
}

/// <summary>
/// App settings configuration from appsettings.json
/// </summary>
public class AppSettingsConfiguration
{
    public string PipedreamEndpointUrl { get; set; } = "";
    public int ActivityTimeoutMs { get; set; }
    public int BatchIntervalMs { get; set; }
    public int MaxConcurrentSubmissions { get; set; }
    public int RetryAttempts { get; set; }
    public int DebounceThresholdMs { get; set; }
    public int FallbackCheckIntervalMs { get; set; }
}

/// <summary>
/// Root object for appsettings.json
/// </summary>
public class AppSettingsRoot
{
    public AppSettingsConfiguration? TimeTracker { get; set; }
}
