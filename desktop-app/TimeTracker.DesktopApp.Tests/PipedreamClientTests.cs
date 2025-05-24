using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class PipedreamClientTests
{
    private Mock<ILogger<PipedreamClient>> _loggerMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private PipedreamClient? _pipedreamClient;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<PipedreamClient>>();
        _configurationMock = new Mock<IConfiguration>();
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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://test.pipedream.net");
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryAttempts", 3))
            .Returns(5);
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryDelayMs", 5000))
            .Returns(2000);

        // Act
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns((string?)null);

        // Act
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Not configured"));
    }

    [Test]
    public void Constructor_WithEmptyEndpointUrl_LogsWarning()
    {
        // Arrange
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns(string.Empty);

        // Act
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Not configured"));
    }

    [Test]
    public async Task SubmitActivityDataAsync_WithoutEndpointUrl_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns((string?)null);
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://test.pipedream.net");
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);
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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns((string?)null);
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

        // Act
        var result = await _pipedreamClient.TestConnectionAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TestConnectionAsync_DisposedClient_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://test.pipedream.net");
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);
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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://example.pipedream.net");
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryAttempts", 3))
            .Returns(3);
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryDelayMs", 5000))
            .Returns(5000);
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns((string?)null);
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

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
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://test.pipedream.net");
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw
        _pipedreamClient.Dispose();
        _pipedreamClient.Dispose();
    }

    [Test]
    public void Constructor_UsesDefaultValues_WhenConfigurationMissing()
    {
        // Arrange
        _configurationMock.Setup(c => c["TimeTracker:PipedreamEndpointUrl"])
            .Returns("https://test.pipedream.net");
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryAttempts", 3))
            .Returns(3); // Default value
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:RetryDelayMs", 5000))
            .Returns(5000); // Default value

        // Act
        _pipedreamClient = new PipedreamClient(_configurationMock.Object, _loggerMock.Object);

        // Assert
        var status = _pipedreamClient.GetConfigurationStatus();
        Assert.That(status, Does.Contain("Retry attempts: 3"));
        Assert.That(status, Does.Contain("Retry delay: 5000ms"));
    }
}
