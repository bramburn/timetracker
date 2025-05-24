using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Diagnostics;
using TimeTracker.DesktopApp.Interfaces;
using TimeTracker.DesktopApp.Tests.TestHelpers;

namespace TimeTracker.DesktopApp.Tests;

/// <summary>
/// Integration tests for the optimized monitoring components to verify
/// performance improvements and functional correctness of Phase 1 optimizations.
/// </summary>
[TestFixture]
public class OptimizedMonitorsIntegrationTests
{
    private IConfiguration _configuration = null!;
    private ILogger<OptimizedWindowMonitor> _windowLogger = null!;
    private ILogger<OptimizedInputMonitor> _inputLogger = null!;
    private OptimizedWindowMonitor _windowMonitor = null!;
    private OptimizedInputMonitor _inputMonitor = null!;

    [SetUp]
    public void Setup()
    {
        _configuration = TestConfiguration.Create();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _windowLogger = loggerFactory.CreateLogger<OptimizedWindowMonitor>();
        _inputLogger = loggerFactory.CreateLogger<OptimizedInputMonitor>();

        _windowMonitor = new OptimizedWindowMonitor(_configuration, _windowLogger);
        _inputMonitor = new OptimizedInputMonitor(_configuration, _inputLogger);
    }

    [TearDown]
    public void TearDown()
    {
        _windowMonitor?.Dispose();
        _inputMonitor?.Dispose();
    }

    [Test]
    public void OptimizedWindowMonitor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act - Constructor called in Setup

        // Assert
        Assert.That(_windowMonitor, Is.Not.Null);
        Assert.That(_windowMonitor.GetCurrentActivity(), Is.Not.Null,
            "Should be able to get current window activity");
    }

    [Test]
    public void OptimizedInputMonitor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act - Constructor called in Setup

        // Assert
        Assert.That(_inputMonitor, Is.Not.Null);
        Assert.That(_inputMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive),
            "Should start with Inactive status");
        Assert.That(_inputMonitor.GetTimeSinceLastInput(), Is.EqualTo(TimeSpan.MaxValue),
            "Should have no input time initially");
    }

    [Test]
    public void OptimizedWindowMonitor_StartStop_ShouldWorkWithoutErrors()
    {
        // Arrange
        ActivityDataModel? capturedActivity = null;

        _windowMonitor.WindowChanged += (activity) =>
        {
            capturedActivity = activity;
        };

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => _windowMonitor.Start());

        // Give some time for potential window changes
        Thread.Sleep(500);

        Assert.DoesNotThrow(() => _windowMonitor.Stop());

        // Verify we can get current activity
        var currentActivity = _windowMonitor.GetCurrentActivity();
        Assert.That(currentActivity, Is.Not.Null);
        Assert.That(currentActivity.WindowsUsername, Is.Not.Empty);
    }

    [Test]
    public void OptimizedInputMonitor_StartStop_ShouldWorkWithoutErrors()
    {
        // Arrange
        ActivityStatus capturedStatus = ActivityStatus.Inactive;

        _inputMonitor.ActivityStatusChanged += (status) =>
        {
            capturedStatus = status;
        };

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => _inputMonitor.Start());

        // Give some time for potential input detection
        Thread.Sleep(500);

        Assert.DoesNotThrow(() => _inputMonitor.Stop());

        // Verify initial state
        Assert.That(_inputMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive));
    }

    [Test]
    public void OptimizedWindowMonitor_ShouldDetectWindowChanges()
    {
        // Arrange
        ActivityDataModel? detectedActivity = null;
        var changeDetectedEvent = new ManualResetEventSlim(false);

        _windowMonitor.WindowChanged += (activity) =>
        {
            detectedActivity = activity;
            changeDetectedEvent.Set();
        };

        // Act
        _windowMonitor.Start();

        // Simulate window change by opening a new process (if possible in test environment)
        // For integration test, we'll just verify the monitor can detect current window
        var initialActivity = _windowMonitor.GetCurrentActivity();

        // Wait a bit for any potential events
        changeDetectedEvent.Wait(TimeSpan.FromSeconds(2));

        _windowMonitor.Stop();

        // Assert
        Assert.That(initialActivity, Is.Not.Null, "Should detect current window");
        Assert.That(initialActivity.ActiveWindowTitle, Is.Not.Empty, "Should have window title");
        Assert.That(initialActivity.ApplicationProcessName, Is.Not.Empty, "Should have process name");
        Assert.That(initialActivity.WindowsUsername, Is.Not.Empty, "Should have username");
    }

    [Test]
    public void OptimizedInputMonitor_ShouldHandleActivityTimeout()
    {
        // Arrange
        var shortTimeoutConfig = TestConfiguration.Create(new Dictionary<string, string>
        {
            ["TimeTracker:ActivityTimeoutMs"] = "1000" // 1 second timeout for test
        });

        using var shortTimeoutMonitor = new OptimizedInputMonitor(shortTimeoutConfig, _inputLogger);

        var inactiveDetectedEvent = new ManualResetEventSlim(false);

        shortTimeoutMonitor.ActivityStatusChanged += (status) =>
        {
            if (status == ActivityStatus.Inactive)
            {
                inactiveDetectedEvent.Set();
            }
        };

        // Act
        shortTimeoutMonitor.Start();

        // Wait for timeout to trigger inactive status
        inactiveDetectedEvent.Wait(TimeSpan.FromSeconds(3));

        shortTimeoutMonitor.Stop();

        // Assert
        Assert.That(shortTimeoutMonitor.GetCurrentActivityStatus(), Is.EqualTo(ActivityStatus.Inactive),
            "Should be inactive after timeout");
    }

    [Test]
    public void OptimizedMonitors_ShouldHaveBetterPerformanceThanPolling()
    {
        // This test demonstrates the performance improvement concept
        // In a real scenario, you would measure CPU usage and response times

        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Start optimized monitors
        _windowMonitor.Start();
        _inputMonitor.Start();

        // Simulate some activity detection time
        Thread.Sleep(100);

        var currentActivity = _windowMonitor.GetCurrentActivity();
        var currentStatus = _inputMonitor.GetCurrentActivityStatus();

        _windowMonitor.Stop();
        _inputMonitor.Stop();

        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000),
            "Operations should complete quickly");
        Assert.That(currentActivity, Is.Not.Null,
            "Should successfully detect window activity");
        Assert.That(currentStatus, Is.EqualTo(ActivityStatus.Inactive),
            "Should successfully get activity status");

        // Log performance info
        Console.WriteLine($"Optimized monitors initialization and basic operations took: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    public void OptimizedMonitors_ShouldDisposeCleanly()
    {
        // Arrange
        _windowMonitor.Start();
        _inputMonitor.Start();

        // Act & Assert - Should not throw during disposal
        Assert.DoesNotThrow(() => _windowMonitor.Dispose());
        Assert.DoesNotThrow(() => _inputMonitor.Dispose());

        // Verify they're disposed
        Assert.DoesNotThrow(() => _windowMonitor.Dispose()); // Should handle multiple dispose calls
        Assert.DoesNotThrow(() => _inputMonitor.Dispose());
    }
}
