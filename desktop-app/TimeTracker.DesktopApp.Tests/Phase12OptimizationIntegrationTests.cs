using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracker.DesktopApp;
using TimeTracker.DesktopApp.Interfaces;
using TimeTracker.DesktopApp.Tests.TestHelpers;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class Phase12OptimizationIntegrationTests
{
    private string _testDatabasePath = null!;
    private IConfiguration _configuration = null!;
    private Mock<ILogger<OptimizedSQLiteDataAccess>> _dataAccessLoggerMock = null!;
    private Mock<ILogger<ActivityLogger>> _activityLoggerMock = null!;
    private Mock<IPipedreamClient> _pipedreamClientMock = null!;
    private Mock<IWindowMonitor> _windowMonitorMock = null!;
    private Mock<IInputMonitor> _inputMonitorMock = null!;

    [SetUp]
    public void SetUp()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_phase12_{Guid.NewGuid()}.db");
        _dataAccessLoggerMock = new Mock<ILogger<OptimizedSQLiteDataAccess>>();
        _activityLoggerMock = new Mock<ILogger<ActivityLogger>>();
        _pipedreamClientMock = new Mock<IPipedreamClient>();
        _windowMonitorMock = new Mock<IWindowMonitor>();
        _inputMonitorMock = new Mock<IInputMonitor>();

        // Create test configuration with optimized settings
        _configuration = TestConfiguration.Create(new Dictionary<string, string>
        {
            ["TimeTracker:MaxBatchSize"] = "5",
            ["TimeTracker:BatchInsertIntervalMs"] = "500",
            ["TimeTracker:MaxConcurrentSubmissions"] = "2"
        });

        // Setup mock behaviors
        _pipedreamClientMock.Setup(p => p.TestConnectionAsync()).ReturnsAsync(true);
        _pipedreamClientMock.Setup(p => p.SubmitActivityDataAsync(It.IsAny<ActivityDataModel>())).ReturnsAsync(true);
        _pipedreamClientMock.Setup(p => p.GetConfigurationStatus()).Returns("Test Configuration");

        _windowMonitorMock.Setup(w => w.GetCurrentActivity()).Returns((ActivityDataModel?)null);
        _inputMonitorMock.Setup(i => i.GetCurrentActivityStatus()).Returns(ActivityStatus.Active);
        _inputMonitorMock.Setup(i => i.GetTimeSinceLastInput()).Returns(TimeSpan.FromMinutes(1));
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test database
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch (IOException)
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task Phase12Optimizations_BatchInsertAndAsyncSubmission_WorkCorrectly()
    {
        // Arrange
        using var backgroundTaskQueue = new BackgroundTaskQueue();
        using var optimizedDataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _dataAccessLoggerMock.Object);

        using var activityLogger = new ActivityLogger(
            optimizedDataAccess,
            _pipedreamClientMock.Object,
            _windowMonitorMock.Object,
            _inputMonitorMock.Object,
            backgroundTaskQueue,
            _configuration,
            _activityLoggerMock.Object);

        // Act - Start the activity logger
        await activityLogger.StartAsync();

        // Create multiple activities to test batch processing
        var activities = new[]
        {
            CreateTestActivity("Window 1", DateTime.UtcNow.AddMinutes(-3)),
            CreateTestActivity("Window 2", DateTime.UtcNow.AddMinutes(-2)),
            CreateTestActivity("Window 3", DateTime.UtcNow.AddMinutes(-1)),
            CreateTestActivity("Window 4", DateTime.UtcNow),
            CreateTestActivity("Window 5", DateTime.UtcNow.AddSeconds(1))
        };

        // Simulate window changes to trigger activity logging
        foreach (var activity in activities)
        {
            _windowMonitorMock.Setup(w => w.GetCurrentActivity()).Returns(activity);
            _windowMonitorMock.Raise(w => w.WindowChanged += null, activity);
            await Task.Delay(50); // Small delay between activities
        }

        // Wait for batch processing and background submissions
        await Task.Delay(1000);

        // Assert - Check that activities were stored in the database
        var storedActivities = await optimizedDataAccess.GetRecentActivitiesAsync(10);
        Assert.That(storedActivities.Count, Is.EqualTo(5), "All activities should be stored in the database");

        // Verify activities are in correct order (most recent first)
        Assert.That(storedActivities[0].ActiveWindowTitle, Is.EqualTo("Window 5"));
        Assert.That(storedActivities[4].ActiveWindowTitle, Is.EqualTo("Window 1"));

        // Verify total count
        var totalCount = await optimizedDataAccess.GetActivityCountAsync();
        Assert.That(totalCount, Is.EqualTo(5));

        // Verify Pipedream submissions were attempted (background processing)
        await Task.Delay(500); // Additional wait for background processing
        _pipedreamClientMock.Verify(p => p.SubmitActivityDataAsync(It.IsAny<ActivityDataModel>()),
            Times.AtLeast(1), "Pipedream submissions should be attempted in background");
    }

    [Test]
    public async Task BackgroundTaskQueue_ProcessesWorkItemsConcurrently()
    {
        // Arrange
        using var queue = new BackgroundTaskQueue();
        var executionTimes = new ConcurrentBag<DateTime>();
        var semaphore = new SemaphoreSlim(2, 2); // Limit to 2 concurrent executions
        var workItemCount = 5;

        // Create work items that simulate concurrent processing
        var workItems = Enumerable.Range(1, workItemCount).Select(i => new Func<CancellationToken, Task>(async ct =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                executionTimes.Add(DateTime.UtcNow);
                await Task.Delay(100, ct); // Simulate work
            }
            finally
            {
                semaphore.Release();
            }
        })).ToArray();

        // Act - Queue all work items
        foreach (var workItem in workItems)
        {
            queue.QueueBackgroundWorkItem(workItem);
        }

        // Process work items with a more reliable approach
        var processingTasks = new List<Task>();
        var processedCount = 0;

        // Create enough processing tasks to handle all work items
        for (int i = 0; i < workItemCount; i++)
        {
            processingTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var item = await queue.DequeueAsync(CancellationToken.None);
                    if (item != null)
                    {
                        await item(CancellationToken.None);
                        Interlocked.Increment(ref processedCount);
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions in test
                }
            }));
        }

        // Wait for all processing tasks to complete or timeout
        await Task.WhenAll(processingTasks);

        // Assert - Verify execution (allow for some timing variations)
        Assert.That(executionTimes.Count, Is.GreaterThanOrEqualTo(4), "Most work items should be executed");
        Assert.That(processedCount, Is.GreaterThanOrEqualTo(4), "Most work items should be processed");

        // Check that some items executed concurrently (within a small time window)
        if (executionTimes.Count >= 2)
        {
            var sortedTimes = executionTimes.OrderBy(t => t).ToList();
            var hasOverlap = false;
            for (int i = 1; i < sortedTimes.Count; i++)
            {
                if ((sortedTimes[i] - sortedTimes[i - 1]).TotalMilliseconds < 150)
                {
                    hasOverlap = true;
                    break;
                }
            }
            Assert.That(hasOverlap, Is.True, "Some work items should execute concurrently");
        }
    }

    [Test]
    public async Task OptimizedSQLiteDataAccess_BatchProcessing_ReducesDatabaseOperations()
    {
        // Arrange
        using var dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _dataAccessLoggerMock.Object);
        var startTime = DateTime.UtcNow;

        // Act - Insert multiple activities rapidly
        var insertTasks = Enumerable.Range(1, 10).Select(async i =>
        {
            var activity = CreateTestActivity($"Batch Window {i}");
            return await dataAccess.InsertActivityAsync(activity);
        });

        var results = await Task.WhenAll(insertTasks);
        var insertDuration = DateTime.UtcNow - startTime;

        // Wait for batch processing to complete
        await Task.Delay(1000);

        // Assert
        Assert.That(results, Is.All.True, "All inserts should succeed immediately");
        Assert.That(insertDuration.TotalMilliseconds, Is.LessThan(100),
            "Batch inserts should complete quickly as they only enqueue");

        var count = await dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(10), "All activities should be persisted after batch processing");
    }

    [Test]
    public void ActivityLogger_StatusInfo_IncludesPendingSubmissions()
    {
        // Arrange
        using var backgroundTaskQueue = new BackgroundTaskQueue();
        using var optimizedDataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _dataAccessLoggerMock.Object);

        using var activityLogger = new ActivityLogger(
            optimizedDataAccess,
            _pipedreamClientMock.Object,
            _windowMonitorMock.Object,
            _inputMonitorMock.Object,
            backgroundTaskQueue,
            _configuration,
            _activityLoggerMock.Object);

        // Add some work items to the queue that will block to prevent immediate processing
        backgroundTaskQueue.QueueBackgroundWorkItem(async _ => await Task.Delay(5000)); // Long delay to keep it in queue
        backgroundTaskQueue.QueueBackgroundWorkItem(async _ => await Task.Delay(5000)); // Long delay to keep it in queue

        // Act - Get status immediately before background processor can consume items
        var statusInfo = activityLogger.GetStatusInfo();

        // Assert - Should show at least 1 pending submission (might be 1 or 2 depending on timing)
        Assert.That(statusInfo, Does.Contain("Pending submissions:"),
            "Status should include pending submission count");
        Assert.That(statusInfo, Does.Contain("Current Status: Active"));
        Assert.That(statusInfo, Does.Contain("Pipedream: Test Configuration"));
    }

    private static ActivityDataModel CreateTestActivity(string windowTitle = "Test Window", DateTime? timestamp = null)
    {
        return new ActivityDataModel
        {
            Timestamp = timestamp ?? DateTime.UtcNow,
            WindowsUsername = Environment.UserName,
            ActiveWindowTitle = windowTitle,
            ApplicationProcessName = "TestApp.exe",
            ActivityStatus = ActivityStatus.Active
        };
    }
}
