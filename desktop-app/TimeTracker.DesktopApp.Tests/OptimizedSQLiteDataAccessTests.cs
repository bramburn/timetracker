using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracker.DesktopApp;
using TimeTracker.DesktopApp.Tests.TestHelpers;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class OptimizedSQLiteDataAccessTests
{
    private OptimizedSQLiteDataAccess? _dataAccess;
    private string _testDatabasePath = null!;
    private Mock<ILogger<OptimizedSQLiteDataAccess>> _loggerMock = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void SetUp()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_timetracker_{Guid.NewGuid()}.db");
        _loggerMock = new Mock<ILogger<OptimizedSQLiteDataAccess>>();

        // Create test configuration
        _configuration = TestConfiguration.Create(new Dictionary<string, string>
        {
            ["TimeTracker:MaxBatchSize"] = "10",
            ["TimeTracker:BatchInsertIntervalMs"] = "1000"
        });
    }

    [TearDown]
    public void TearDown()
    {
        _dataAccess?.Dispose();

        // Wait a bit for any background operations to complete
        Thread.Sleep(100);

        // Force garbage collection to release any remaining handles
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Try to delete the file with retries
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (File.Exists(_testDatabasePath))
                {
                    File.Delete(_testDatabasePath);
                }
                break;
            }
            catch (IOException)
            {
                if (i == 4) throw; // Re-throw on final attempt
                Thread.Sleep(100);
            }
        }
    }

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);

        // Assert
        Assert.That(_dataAccess, Is.Not.Null);
        Assert.That(File.Exists(_testDatabasePath), Is.True);
    }

    [Test]
    public async Task InsertActivityAsync_EnqueuesActivity()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);
        var activity = CreateTestActivity();

        // Act
        var result = await _dataAccess.InsertActivityAsync(activity);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InsertActivityAsync_MultipleActivities_EnqueuesAll()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);
        var activities = new[]
        {
            CreateTestActivity("Window 1"),
            CreateTestActivity("Window 2"),
            CreateTestActivity("Window 3")
        };

        // Act
        var results = new List<bool>();
        foreach (var activity in activities)
        {
            results.Add(await _dataAccess.InsertActivityAsync(activity));
        }

        // Assert
        Assert.That(results, Is.All.True);
    }

    [Test]
    public async Task BatchProcessing_ProcessesActivitiesInBatches()
    {
        // Arrange
        var smallBatchConfig = TestConfiguration.Create(new Dictionary<string, string>
        {
            ["TimeTracker:MaxBatchSize"] = "2",
            ["TimeTracker:BatchInsertIntervalMs"] = "100"
        });

        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, smallBatchConfig, _loggerMock.Object);

        var activities = new[]
        {
            CreateTestActivity("Window 1"),
            CreateTestActivity("Window 2"),
            CreateTestActivity("Window 3")
        };

        // Act - Insert activities to trigger batch processing
        foreach (var activity in activities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }

        // Wait for batch processing to complete
        await Task.Delay(500);

        // Assert - Check that activities were persisted to database
        var recentActivities = await _dataAccess.GetRecentActivitiesAsync(10);
        Assert.That(recentActivities.Count, Is.GreaterThanOrEqualTo(2)); // At least one batch should be processed
    }

    [Test]
    public async Task GetRecentActivitiesAsync_ReturnsActivitiesInDescendingOrder()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);

        var activities = new[]
        {
            CreateTestActivity("Window 1", DateTime.UtcNow.AddMinutes(-3)),
            CreateTestActivity("Window 2", DateTime.UtcNow.AddMinutes(-2)),
            CreateTestActivity("Window 3", DateTime.UtcNow.AddMinutes(-1))
        };

        // Insert activities and wait for batch processing
        foreach (var activity in activities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }
        await Task.Delay(1500); // Wait for batch processing

        // Act
        var recentActivities = await _dataAccess.GetRecentActivitiesAsync(3);

        // Assert
        Assert.That(recentActivities.Count, Is.EqualTo(3));
        Assert.That(recentActivities[0].ActiveWindowTitle, Is.EqualTo("Window 3")); // Most recent first
        Assert.That(recentActivities[1].ActiveWindowTitle, Is.EqualTo("Window 2"));
        Assert.That(recentActivities[2].ActiveWindowTitle, Is.EqualTo("Window 1"));
    }

    [Test]
    public async Task GetActivityCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);

        var activities = new[]
        {
            CreateTestActivity("Window 1"),
            CreateTestActivity("Window 2"),
            CreateTestActivity("Window 3")
        };

        // Insert activities and wait for batch processing
        foreach (var activity in activities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }
        await Task.Delay(1500); // Wait for batch processing

        // Act
        var count = await _dataAccess.GetActivityCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task Dispose_ProcessesPendingInserts()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);

        var activity = CreateTestActivity();
        await _dataAccess.InsertActivityAsync(activity);

        // Act - Dispose immediately without waiting for timer
        _dataAccess.Dispose();
        _dataAccess = null; // Clear reference to avoid double disposal in TearDown

        // Wait a bit for disposal to complete
        await Task.Delay(200);

        // Create new instance to check if data was persisted
        using var newDataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);
        var count = await newDataAccess.GetActivityCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);

        // Act & Assert - Should not throw
        _dataAccess.Dispose();
        _dataAccess.Dispose();
    }

    [Test]
    public async Task InsertActivityAsync_DisposedInstance_ReturnsFalse()
    {
        // Arrange
        _dataAccess = new OptimizedSQLiteDataAccess(_testDatabasePath, _configuration, _loggerMock.Object);
        _dataAccess.Dispose();
        var activity = CreateTestActivity();

        // Act
        var result = await _dataAccess.InsertActivityAsync(activity);

        // Assert
        Assert.That(result, Is.False);
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
