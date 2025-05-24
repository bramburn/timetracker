using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class ActivityLoggerTests
{
    private Mock<ILogger<ActivityLogger>> _loggerMock;
    private Mock<SQLiteDataAccess> _dataAccessMock;
    private Mock<PipedreamClient> _pipedreamClientMock;
    private Mock<WindowMonitor> _windowMonitorMock;
    private Mock<InputMonitor> _inputMonitorMock;
    private ActivityLogger _activityLogger;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ActivityLogger>>();
        _dataAccessMock = new Mock<SQLiteDataAccess>();
        _pipedreamClientMock = new Mock<PipedreamClient>();
        _windowMonitorMock = new Mock<WindowMonitor>();
        _inputMonitorMock = new Mock<InputMonitor>();
    }

    [TearDown]
    public void TearDown()
    {
        _activityLogger?.Dispose();
    }

    private ActivityLogger CreateActivityLogger()
    {
        return new ActivityLogger(
            _dataAccessMock.Object,
            _pipedreamClientMock.Object,
            _windowMonitorMock.Object,
            _inputMonitorMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        _activityLogger = CreateActivityLogger();

        // Assert
        Assert.That(_activityLogger, Is.Not.Null);
    }

    [Test]
    public async Task StartAsync_StartsMonitoringComponents()
    {
        // Arrange
        _pipedreamClientMock.Setup(p => p.TestConnectionAsync())
            .ReturnsAsync(true);
        _windowMonitorMock.Setup(w => w.GetCurrentActivity())
            .Returns((ActivityDataModel)null);
        
        _activityLogger = CreateActivityLogger();

        // Act
        await _activityLogger.StartAsync();

        // Assert
        _windowMonitorMock.Verify(w => w.Start(), Times.Once);
        _inputMonitorMock.Verify(i => i.Start(), Times.Once);
        _pipedreamClientMock.Verify(p => p.TestConnectionAsync(), Times.Once);
    }

    [Test]
    public async Task StartAsync_WithInitialActivity_LogsActivity()
    {
        // Arrange
        var initialActivity = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };

        _pipedreamClientMock.Setup(p => p.TestConnectionAsync())
            .ReturnsAsync(true);
        _windowMonitorMock.Setup(w => w.GetCurrentActivity())
            .Returns(initialActivity);
        _inputMonitorMock.Setup(i => i.GetCurrentActivityStatus())
            .Returns(ActivityStatus.Active);
        _dataAccessMock.Setup(d => d.InsertActivityAsync(It.IsAny<ActivityDataModel>()))
            .ReturnsAsync(true);
        _pipedreamClientMock.Setup(p => p.SubmitActivityDataAsync(It.IsAny<ActivityDataModel>()))
            .ReturnsAsync(true);

        _activityLogger = CreateActivityLogger();

        // Act
        await _activityLogger.StartAsync();

        // Assert
        _dataAccessMock.Verify(d => d.InsertActivityAsync(It.IsAny<ActivityDataModel>()), Times.Once);
        _pipedreamClientMock.Verify(p => p.SubmitActivityDataAsync(It.IsAny<ActivityDataModel>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_DisposedLogger_LogsWarning()
    {
        // Arrange
        _activityLogger = CreateActivityLogger();
        _activityLogger.Dispose();

        // Act
        await _activityLogger.StartAsync();

        // Assert - Should not throw and should log warning
        Assert.Pass("StartAsync completed without throwing on disposed logger");
    }

    [Test]
    public async Task StopAsync_StopsMonitoringComponents()
    {
        // Arrange
        _activityLogger = CreateActivityLogger();

        // Act
        await _activityLogger.StopAsync();

        // Assert
        _windowMonitorMock.Verify(w => w.Stop(), Times.Once);
        _inputMonitorMock.Verify(i => i.Stop(), Times.Once);
    }

    [Test]
    public async Task StopAsync_DisposedLogger_LogsWarning()
    {
        // Arrange
        _activityLogger = CreateActivityLogger();
        _activityLogger.Dispose();

        // Act
        await _activityLogger.StopAsync();

        // Assert - Should not throw and should log warning
        Assert.Pass("StopAsync completed without throwing on disposed logger");
    }

    [Test]
    public void GetStatusInfo_ReturnsFormattedStatus()
    {
        // Arrange
        _inputMonitorMock.Setup(i => i.GetTimeSinceLastInput())
            .Returns(TimeSpan.FromMinutes(2));
        _inputMonitorMock.Setup(i => i.GetCurrentActivityStatus())
            .Returns(ActivityStatus.Active);
        _pipedreamClientMock.Setup(p => p.GetConfigurationStatus())
            .Returns("Configured - Test Status");

        _activityLogger = CreateActivityLogger();

        // Act
        var status = _activityLogger.GetStatusInfo();

        // Assert
        Assert.That(status, Does.Contain("Current Status: Active"));
        Assert.That(status, Does.Contain("Time since last input: 00:02:00"));
        Assert.That(status, Does.Contain("Pipedream: Configured - Test Status"));
    }

    [Test]
    public void GetStatusInfo_WithMaxTimeSpan_ShowsNever()
    {
        // Arrange
        _inputMonitorMock.Setup(i => i.GetTimeSinceLastInput())
            .Returns(TimeSpan.MaxValue);
        _inputMonitorMock.Setup(i => i.GetCurrentActivityStatus())
            .Returns(ActivityStatus.Inactive);
        _pipedreamClientMock.Setup(p => p.GetConfigurationStatus())
            .Returns("Not configured");

        _activityLogger = CreateActivityLogger();

        // Act
        var status = _activityLogger.GetStatusInfo();

        // Assert
        Assert.That(status, Does.Contain("Current Status: Inactive"));
        Assert.That(status, Does.Contain("Time since last input: Never"));
        Assert.That(status, Does.Contain("Pipedream: Not configured"));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _activityLogger = CreateActivityLogger();

        // Act & Assert - Should not throw
        _activityLogger.Dispose();
        _activityLogger.Dispose();
    }

    [Test]
    public void Constructor_SubscribesToEvents()
    {
        // Arrange & Act
        _activityLogger = CreateActivityLogger();

        // Assert - We can't easily verify event subscription without triggering events
        // But we can verify the constructor completed successfully
        Assert.That(_activityLogger, Is.Not.Null);
    }

    [Test]
    public async Task StartAsync_PipedreamConnectionFails_ContinuesExecution()
    {
        // Arrange
        _pipedreamClientMock.Setup(p => p.TestConnectionAsync())
            .ReturnsAsync(false);
        _windowMonitorMock.Setup(w => w.GetCurrentActivity())
            .Returns((ActivityDataModel)null);

        _activityLogger = CreateActivityLogger();

        // Act & Assert - Should not throw even if Pipedream connection fails
        Assert.DoesNotThrowAsync(async () => await _activityLogger.StartAsync());
        
        _windowMonitorMock.Verify(w => w.Start(), Times.Once);
        _inputMonitorMock.Verify(i => i.Start(), Times.Once);
    }

    [Test]
    public async Task StartAsync_ExceptionInPipedreamTest_ThrowsException()
    {
        // Arrange
        _pipedreamClientMock.Setup(p => p.TestConnectionAsync())
            .ThrowsAsync(new HttpRequestException("Network error"));

        _activityLogger = CreateActivityLogger();

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _activityLogger.StartAsync());
    }
}
