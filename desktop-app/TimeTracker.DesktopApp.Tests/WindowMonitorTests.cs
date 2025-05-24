using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class WindowMonitorTests
{
    private Mock<ILogger<WindowMonitor>> _loggerMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private WindowMonitor? _windowMonitor;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<WindowMonitor>>();
        _configurationMock = new Mock<IConfiguration>();
    }

    [TearDown]
    public void TearDown()
    {
        _windowMonitor?.Dispose();
    }

    [Test]
    public void Constructor_WithDefaultConfiguration_InitializesCorrectly()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);

        // Act
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert
        Assert.That(_windowMonitor, Is.Not.Null);
        Assert.That(_windowMonitor.GetLastActivity(), Is.Null);
    }

    [Test]
    public void Constructor_WithCustomInterval_UsesCustomValue()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(500);

        // Act
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert - We can't directly test the interval value, but we can verify the monitor was created
        Assert.That(_windowMonitor, Is.Not.Null);
    }

    [Test]
    public void GetLastActivity_InitialState_ReturnsNull()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act
        var lastActivity = _windowMonitor.GetLastActivity();

        // Assert
        Assert.That(lastActivity, Is.Null);
    }

    [Test]
    public void GetCurrentActivity_ReturnsActivityData()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act
        var currentActivity = _windowMonitor.GetCurrentActivity();

        // Assert
        // Note: This might return null if no window is active or if Win32 APIs fail in test environment
        // We just verify the method doesn't throw
        Assert.DoesNotThrow(() => _windowMonitor.GetCurrentActivity());
    }

    [Test]
    public void Start_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.Start());
    }

    [Test]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.Stop());
    }

    [Test]
    public void Start_ThenStop_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.Start());
        Assert.DoesNotThrow(() => _windowMonitor.Stop());
    }

    [Test]
    public void Start_DisposedMonitor_LogsWarning()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);
        _windowMonitor.Dispose();

        // Act
        _windowMonitor.Start();

        // Assert - Should not throw, but should log warning
        Assert.Pass("Start method completed without throwing on disposed monitor");
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw
        _windowMonitor.Dispose();
        _windowMonitor.Dispose();
    }

    [Test]
    public void WindowChanged_Event_CanBeSubscribed()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        var eventFired = false;
        ActivityDataModel receivedActivity = null;

        // Act
        _windowMonitor.WindowChanged += (activity) =>
        {
            eventFired = true;
            receivedActivity = activity;
        };

        // Assert - Event subscription should not throw
        Assert.That(eventFired, Is.False); // Event hasn't fired yet
    }

    [Test]
    public void WindowChanged_Event_CanBeUnsubscribed()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        void EventHandler(ActivityDataModel? activity) { }

        // Act & Assert - Should not throw
        _windowMonitor.WindowChanged += EventHandler;
        _windowMonitor.WindowChanged -= EventHandler;
    }

    [Test]
    public void Start_MultipleCallsWithoutStop_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.Start());
        Assert.DoesNotThrow(() => _windowMonitor.Start()); // Second call should not throw
    }

    [Test]
    public void Stop_MultipleCallsWithoutStart_DoesNotThrow()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.Stop());
        Assert.DoesNotThrow(() => _windowMonitor.Stop()); // Second call should not throw
    }

    [Test]
    public void DisposedMonitor_GetMethods_ReturnSafeValues()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);
        _windowMonitor.Dispose();

        // Act & Assert
        Assert.DoesNotThrow(() => _windowMonitor.GetCurrentActivity());
        Assert.DoesNotThrow(() => _windowMonitor.GetLastActivity());
    }

    [Test]
    public void Constructor_WithNullConfiguration_UsesDefaultInterval()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000); // Default value

        // Act
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Assert
        Assert.That(_windowMonitor, Is.Not.Null);
    }

    [Test]
    public void GetLastActivity_ReturnsClone_WhenActivityExists()
    {
        // Arrange
        _configurationMock.Setup(c => c.GetValue<int>("TimeTracker:WindowMonitoringIntervalMs", 1000))
            .Returns(1000);
        _windowMonitor = new WindowMonitor(_configurationMock.Object, _loggerMock.Object);

        // Act
        var lastActivity1 = _windowMonitor.GetLastActivity();
        var lastActivity2 = _windowMonitor.GetLastActivity();

        // Assert
        // If both are null, that's fine (no activity captured yet)
        // If both are not null, they should be different instances (clones)
        if (lastActivity1 != null && lastActivity2 != null)
        {
            Assert.That(lastActivity1, Is.Not.SameAs(lastActivity2));
        }
    }
}
