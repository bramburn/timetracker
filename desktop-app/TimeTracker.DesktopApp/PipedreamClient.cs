using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Manages data transmission to the Pipedream endpoint for testing purposes.
/// Handles JSON serialization, HTTP communication, and robust error handling with retry logic.
/// </summary>
public class PipedreamClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PipedreamClient> _logger;
    private readonly string _endpointUrl;
    private readonly int _retryAttempts;
    private readonly int _retryDelayMs;
    private bool _disposed = false;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PipedreamClient(IConfiguration configuration, ILogger<PipedreamClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        // Configure HTTP client
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TimeTracker.DesktopApp/1.0");

        // Load configuration
        _endpointUrl = configuration["TimeTracker:PipedreamEndpointUrl"] ?? string.Empty;
        _retryAttempts = configuration.GetValue<int>("TimeTracker:RetryAttempts", 3);
        _retryDelayMs = configuration.GetValue<int>("TimeTracker:RetryDelayMs", 5000);

        if (string.IsNullOrEmpty(_endpointUrl))
        {
            _logger.LogWarning("Pipedream endpoint URL not configured. Data submission will be disabled.");
        }
        else
        {
            _logger.LogInformation("Pipedream client initialized with endpoint: {EndpointUrl}", _endpointUrl);
        }
    }

    /// <summary>
    /// Submits activity data to the Pipedream endpoint with retry logic
    /// </summary>
    /// <param name="activityData">The activity data to submit</param>
    /// <returns>True if submission was successful, false otherwise</returns>
    public async Task<bool> SubmitActivityDataAsync(ActivityDataModel activityData)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to submit data using disposed PipedreamClient");
            return false;
        }

        if (string.IsNullOrEmpty(_endpointUrl))
        {
            _logger.LogDebug("Pipedream endpoint not configured, skipping submission");
            return false;
        }

        for (int attempt = 1; attempt <= _retryAttempts; attempt++)
        {
            try
            {
                var success = await SubmitDataAttemptAsync(activityData);
                if (success)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Data submission succeeded on attempt {Attempt}", attempt);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Data submission attempt {Attempt} failed: {Message}", attempt, ex.Message);
            }

            // Wait before retry (except on last attempt)
            if (attempt < _retryAttempts)
            {
                var delay = _retryDelayMs * attempt; // Exponential backoff
                _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt}", delay, attempt + 1);
                await Task.Delay(delay);
            }
        }

        _logger.LogError("All {RetryAttempts} submission attempts failed for activity data", _retryAttempts);
        return false;
    }

    /// <summary>
    /// Performs a single attempt to submit data to Pipedream
    /// </summary>
    /// <param name="activityData">The activity data to submit</param>
    /// <returns>True if submission was successful, false otherwise</returns>
    private async Task<bool> SubmitDataAttemptAsync(ActivityDataModel activityData)
    {
        try
        {
            // Serialize activity data to JSON
            var jsonPayload = JsonSerializer.Serialize(activityData, JsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogDebug("Submitting activity data to Pipedream: {JsonPayload}", jsonPayload);

            // Send HTTP POST request
            var response = await _httpClient.PostAsync(_endpointUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Activity data submitted successfully. Status: {StatusCode}", response.StatusCode);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Pipedream submission failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP request failed during Pipedream submission");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout during Pipedream submission");
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization failed for activity data");
            return false;
        }
    }

    /// <summary>
    /// Tests the connection to the Pipedream endpoint
    /// </summary>
    /// <returns>True if the endpoint is reachable, false otherwise</returns>
    public async Task<bool> TestConnectionAsync()
    {
        if (_disposed || string.IsNullOrEmpty(_endpointUrl))
        {
            return false;
        }

        try
        {
            var testData = new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow,
                WindowsUsername = "TEST_USER",
                ActiveWindowTitle = "Connection Test",
                ApplicationProcessName = "TimeTracker.Test",
                ActivityStatus = ActivityStatus.Active
            };

            var result = await SubmitActivityDataAsync(testData);
            _logger.LogInformation("Pipedream connection test result: {Result}", result ? "Success" : "Failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipedream connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Gets the current configuration status
    /// </summary>
    /// <returns>Configuration status information</returns>
    public string GetConfigurationStatus()
    {
        if (string.IsNullOrEmpty(_endpointUrl))
        {
            return "Not configured - Pipedream endpoint URL is missing";
        }

        return $"Configured - Endpoint: {_endpointUrl}, Retry attempts: {_retryAttempts}, Retry delay: {_retryDelayMs}ms";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
            _logger.LogInformation("PipedreamClient disposed");
        }
    }
}
