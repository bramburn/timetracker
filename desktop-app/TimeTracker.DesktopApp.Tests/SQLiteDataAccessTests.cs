using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class SQLiteDataAccessTests
{
    private Mock<ILogger<SQLiteDataAccess>> _loggerMock;
    private string _testDatabasePath;
    private SQLiteDataAccess _dataAccess;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<SQLiteDataAccess>>();
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_timetracker_{Guid.NewGuid()}.db");
        _dataAccess = new SQLiteDataAccess(_testDatabasePath, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dataAccess?.Dispose();
        if (File.Exists(_testDatabasePath))
        {
            File.Delete(_testDatabasePath);
        }
    }

    [Test]
    public async Task Constructor_CreatesDatabase()
    {
        // Assert
        Assert.That(File.Exists(_testDatabasePath), Is.True);

        // Verify we can get count (table exists)
        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task InsertActivityAsync_ValidData_ReturnsTrue()
    {
        // Arrange
        var activity = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow,
            WindowsUsername = "TestUser",
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe",
            ActivityStatus = ActivityStatus.Active
        };

        // Act
        var result = await _dataAccess.InsertActivityAsync(activity);

        // Assert
        Assert.That(result, Is.True);
        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task InsertActivityAsync_NullData_ReturnsFalse()
    {
        // Act
        var result = await _dataAccess.InsertActivityAsync(null!);

        // Assert
        Assert.That(result, Is.False);
        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task InsertActivityAsync_MultipleRecords_AllInserted()
    {
        // Arrange
        var activities = new[]
        {
            new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-2),
                WindowsUsername = "User1",
                ActiveWindowTitle = "Window 1",
                ApplicationProcessName = "App1.exe",
                ActivityStatus = ActivityStatus.Active
            },
            new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-1),
                WindowsUsername = "User2",
                ActiveWindowTitle = "Window 2",
                ApplicationProcessName = "App2.exe",
                ActivityStatus = ActivityStatus.Inactive
            }
        };

        // Act
        foreach (var activity in activities)
        {
            var result = await _dataAccess.InsertActivityAsync(activity);
            Assert.That(result, Is.True);
        }

        // Assert
        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetRecentActivitiesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var activities = await _dataAccess.GetRecentActivitiesAsync(10);

        // Assert
        Assert.That(activities, Is.Not.Null);
        Assert.That(activities, Is.Empty);
    }

    [Test]
    public async Task GetRecentActivitiesAsync_WithData_ReturnsCorrectCount()
    {
        // Arrange
        var testActivities = new[]
        {
            new ActivityDataModel { WindowsUsername = "User1", ActiveWindowTitle = "Window 1", ApplicationProcessName = "App1.exe" },
            new ActivityDataModel { WindowsUsername = "User2", ActiveWindowTitle = "Window 2", ApplicationProcessName = "App2.exe" },
            new ActivityDataModel { WindowsUsername = "User3", ActiveWindowTitle = "Window 3", ApplicationProcessName = "App3.exe" }
        };

        foreach (var activity in testActivities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }

        // Act
        var activities = await _dataAccess.GetRecentActivitiesAsync(2);

        // Assert
        Assert.That(activities.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetRecentActivitiesAsync_ReturnsInDescendingOrder()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var testActivities = new[]
        {
            new ActivityDataModel { Timestamp = baseTime.AddMinutes(-3), WindowsUsername = "User1", ActiveWindowTitle = "First" },
            new ActivityDataModel { Timestamp = baseTime.AddMinutes(-2), WindowsUsername = "User2", ActiveWindowTitle = "Second" },
            new ActivityDataModel { Timestamp = baseTime.AddMinutes(-1), WindowsUsername = "User3", ActiveWindowTitle = "Third" }
        };

        foreach (var activity in testActivities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }

        // Act
        var activities = await _dataAccess.GetRecentActivitiesAsync(3);

        // Assert
        Assert.That(activities.Count, Is.EqualTo(3));
        Assert.That(activities[0].ActiveWindowTitle, Is.EqualTo("Third")); // Most recent first
        Assert.That(activities[1].ActiveWindowTitle, Is.EqualTo("Second"));
        Assert.That(activities[2].ActiveWindowTitle, Is.EqualTo("First"));
    }

    [Test]
    public async Task GetActivityCountAsync_EmptyDatabase_ReturnsZero()
    {
        // Act
        var count = await _dataAccess.GetActivityCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetActivityCountAsync_WithData_ReturnsCorrectCount()
    {
        // Arrange
        var activities = new[]
        {
            new ActivityDataModel { WindowsUsername = "User1", ActiveWindowTitle = "Window 1" },
            new ActivityDataModel { WindowsUsername = "User2", ActiveWindowTitle = "Window 2" },
            new ActivityDataModel { WindowsUsername = "User3", ActiveWindowTitle = "Window 3" },
            new ActivityDataModel { WindowsUsername = "User4", ActiveWindowTitle = "Window 4" },
            new ActivityDataModel { WindowsUsername = "User5", ActiveWindowTitle = "Window 5" }
        };

        foreach (var activity in activities)
        {
            await _dataAccess.InsertActivityAsync(activity);
        }

        // Act
        var count = await _dataAccess.GetActivityCountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(5));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        _dataAccess.Dispose();
        _dataAccess.Dispose();
    }

    [Test]
    public async Task DisposedDataAccess_OperationsReturnSafeValues()
    {
        // Arrange
        _dataAccess.Dispose();

        // Act & Assert
        var insertResult = await _dataAccess.InsertActivityAsync(new ActivityDataModel());
        Assert.That(insertResult, Is.False);

        var activities = await _dataAccess.GetRecentActivitiesAsync(10);
        Assert.That(activities, Is.Empty);

        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(0));
    }
}
