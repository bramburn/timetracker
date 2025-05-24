using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;
using TimeTracker.DesktopApp.Tests.TestHelpers;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class PipedreamClientSimpleTests
{
    private ILogger<PipedreamClient> _logger = null!;
    private PipedreamClient? _pipedreamClient;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerFactory().CreateLogger<PipedreamClient>();
    }

    [TearDown]
    public void TearDown()
    {
        _pipedreamClient?.Dispose();
    }

    [Test]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.pipedream.net",
            ["TimeTracker:RetryAttempts"] = "5",
            ["TimeTracker:RetryDelayMs"] = "2000"
        });

        // Act
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("https://test.pipedream.net"));
        Assert.That(status, Does.Contain("Retry attempts: 5"));
        Assert.That(status, Does.Contain("Retry delay: 2000ms"));
    }

    [Test]
    public void Constructor_WithMissingEndpointUrl_LogsWarning()
    {
        // Arrange
        var config = new TestConfiguration();

        // Act
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Not configured"));
    }

    [Test]
    public void Constructor_WithEmptyEndpointUrl_LogsWarning()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = ""
        });

        // Act
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Not configured"));
    }

    [Test]
    public async Task SubmitActivityDataAsync_WithoutEndpointUrl_ReturnsFalse()
    {
        // Arrange
        var config = new TestConfiguration();
        _pipedreamClient = new PipedreamClient(config, _logger);

        var activityData = new ActivityDataModel
        {
            WindowsUsername = "TestUser",
            ActiveWindowTitle = "Test Window"
        };

        // Act
        var result = await _pipedreamClient.SubmitActivityDataAsync(activityData);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task SubmitActivityDataAsync_DisposedClient_ReturnsFalse()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.pipedream.net"
        });
        _pipedreamClient = new PipedreamClient(config, _logger);
        _pipedreamClient.Dispose();

        var activityData = new ActivityDataModel
        {
            WindowsUsername = "TestUser",
            ActiveWindowTitle = "Test Window"
        };

        // Act
        var result = await _pipedreamClient.SubmitActivityDataAsync(activityData);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TestConnectionAsync_WithoutEndpointUrl_ReturnsFalse()
    {
        // Arrange
        var config = new TestConfiguration();
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Act
        var result = await _pipedreamClient.TestConnectionAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TestConnectionAsync_DisposedClient_ReturnsFalse()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.pipedream.net"
        });
        _pipedreamClient = new PipedreamClient(config, _logger);
        _pipedreamClient.Dispose();

        // Act
        var result = await _pipedreamClient.TestConnectionAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetConfigurationStatus_WithValidConfiguration_ReturnsCorrectStatus()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://example.pipedream.net",
            ["TimeTracker:RetryAttempts"] = "3",
            ["TimeTracker:RetryDelayMs"] = "5000"
        });
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Act
        var status = _pipedreamClient.GetConfigurationStatus();

        // Assert
        Assert.That(status, Does.StartWith("Configured"));
        Assert.That(status, Does.Contain("https://example.pipedream.net"));
        Assert.That(status, Does.Contain("Retry attempts: 3"));
        Assert.That(status, Does.Contain("Retry delay: 5000ms"));
    }

    [Test]
    public void GetConfigurationStatus_WithoutEndpointUrl_ReturnsNotConfigured()
    {
        // Arrange
        var config = new TestConfiguration();
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Act
        var status = _pipedreamClient.GetConfigurationStatus();

        // Assert
        Assert.That(status, Does.StartWith("Not configured"));
        Assert.That(status, Does.Contain("Pipedream endpoint URL is missing"));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.pipedream.net"
        });
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Act & Assert - Should not throw
        _pipedreamClient.Dispose();
        _pipedreamClient.Dispose();
    }

    [Test]
    public void Constructor_UsesDefaultValues_WhenConfigurationMissing()
    {
        // Arrange
        var config = new TestConfiguration(new Dictionary<string, string>
        {
            ["TimeTracker:PipedreamEndpointUrl"] = "https://test.pipedream.net"
            // RetryAttempts and RetryDelayMs not specified, should use defaults
        });

        // Act
        _pipedreamClient = new PipedreamClient(config, _logger);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Retry attempts: 3")); // Default value
        Assert.That(status, Does.Contain("Retry delay: 5000ms")); // Default value
    }
}
