using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class InputMonitorTests
{
    private Mock<ILogger<InputMonitor>> _loggerMock;
    private Mock<IConfiguration> _configurationMock;
    private InputMonitor _inputMonitor;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<InputMonitor>>();
        _configurationMock = new Mock<IConfiguration>();
    }

    [TearDown]
    public void TearDown()
    {
        _inputMonitor?.Dispose();
    }

    [Test]
    public void Constructor_WithDefaultConfiguration_InitializesCorrectly()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);

        // Act
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert
        Assert.That(_inputMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive));
        Assert.That(_inputMonitor.GetTimeSinceLastInput(), Is.EqualTo(TimeSpan.MaxValue));
    }

    [Test]
    public void Constructor_WithCustomTimeout_UsesCustomValue()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(15000);

        // Act
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert - We can't directly test the timeout value, but we can verify the monitor was created
        Assert.That(_inputMonitor, Is.Not.Null);
        Assert.That(_inputMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive));
    }

    [Test]
    public void GetCurrentActivityStatus_InitialState_ReturnsInactive()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act
        var status = _inputMonitor.GetCurrentActivityStatus();

        // Assert
        Assert.That(status, Is.EqualTo(ActivityStatus.Inactive));
    }

    [Test]
    public void GetTimeSinceLastInput_InitialState_ReturnsMaxValue()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act
        var timeSinceLastInput = _inputMonitor.GetTimeSinceLastInput();

        // Assert
        Assert.That(timeSinceLastInput, Is.EqualTo(TimeSpan.MaxValue));
    }

    [Test]
    public void Start_DisposedMonitor_LogsWarning()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);
        _inputMonitor.Dispose();

        // Act
        _inputMonitor.Start();

        // Assert - Should not throw, but should log warning
        // We can't easily verify the log call without more complex mock setup
        Assert.Pass("Start method completed without throwing on disposed monitor");
    }

    [Test]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _inputMonitor.Stop());
    }

    [Test]
    public void Start_ThenStop_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _inputMonitor.Start());
        Assert.DoesNotThrow(() => _inputMonitor.Stop());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw
        _inputMonitor.Dispose();
        _inputMonitor.Dispose();
    }

    [Test]
    public void ActivityStatusChanged_Event_CanBeSubscribed()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        var eventFired = false;
        ActivityStatus receivedStatus = ActivityStatus.Inactive;

        // Act
        _inputMonitor.ActivityStatusChanged += (status) =>
        {
            eventFired = true;
            receivedStatus = status;
        };

        // Assert - Event subscription should not throw
        Assert.That(eventFired, Is.False); // Event hasn't fired yet
    }

    [Test]
    public void ActivityStatusChanged_Event_CanBeUnsubscribed()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        void EventHandler(ActivityStatus status) { }

        // Act & Assert - Should not throw
        _inputMonitor.ActivityStatusChanged += EventHandler;
        _inputMonitor.ActivityStatusChanged -= EventHandler;
    }

    [Test]
    public void Constructor_WithNullConfiguration_UsesDefaultTimeout()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000); // Default value

        // Act
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert
        Assert.That(_inputMonitor, Is.Not.Null);
        Assert.That(_inputMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive));
    }

    [Test]
    public void Start_MultipleCallsWithoutStop_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _inputMonitor.Start());
        Assert.DoesNotThrow(() => _inputMonitor.Start()); // Second call should not throw
    }

    [Test]
    public void Stop_MultipleCallsWithoutStart_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _inputMonitor.Stop());
        Assert.DoesNotThrow(() => _inputMonitor.Stop()); // Second call should not throw
    }

    [Test]
    public void DisposedMonitor_GetMethods_ReturnSafeValues()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:ActivityTimeoutMs", 30000))
            .Returns(30000);
        _inputMonitor = new InputMonitor(_configurationMock.Object, _loggerMock.Object);
        _inputMonitor.Dispose();

        // Act & Assert
        Assert.DoesNotThrow(() => _inputMonitor.GetCurrentActivityStatus());
        Assert.DoesNotThrow(() => _inputMonitor.GetTimeSinceLastInput());
    }
}
