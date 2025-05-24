using Microsoft.Extensions.Logging;
using NUnit.Framework;
using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.IntegrationTests;

[TestFixture]
public class SQLiteDataAccessIntegrationTests
{
    private ILogger<SQLiteDataAccess> _logger;
    private string _testDatabasePath;
    private SQLiteDataAccess _dataAccess;

    [SetUp]
    public void SetUp()
    {
        _logger = new LoggerFactory().CreateLogger<SQLiteDataAccess>();
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"integration_test_timetracker_{Guid.NewGuid()}.db");
        _dataAccess = new SQLiteDataAccess(_testDatabasePath, _logger);
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
    public async Task DatabaseOperations_FullWorkflow_WorksCorrectly()
    {
        // Arrange
        var testActivities = new[]
        {
            new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                WindowsUsername = "IntegrationTestUser1",
                ActiveWindowTitle = "Visual Studio Code - Integration Tests",
                ApplicationProcessName = "Code.exe",
                ActivityStatus = ActivityStatus.Active
            },
            new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                WindowsUsername = "IntegrationTestUser1",
                ActiveWindowTitle = "Google Chrome - TimeTracker Documentation",
                ApplicationProcessName = "chrome.exe",
                ActivityStatus = ActivityStatus.Active
            },
            new ActivityDataModel
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-2),
                WindowsUsername = "IntegrationTestUser1",
                ActiveWindowTitle = "Notepad - notes.txt",
                ApplicationProcessName = "notepad.exe",
                ActivityStatus = ActivityStatus.Inactive
            }
        };

        // Act - Insert all activities
        foreach (var activity in testActivities)
        {
            var insertResult = await _dataAccess.InsertActivityAsync(activity);
            Assert.That(insertResult, Is.True, $"Failed to insert activity: {activity.ActiveWindowTitle}");
        }

        // Assert - Verify count
        var totalCount = await _dataAccess.GetActivityCountAsync();
        Assert.That(totalCount, Is.EqualTo(3));

        // Assert - Verify recent activities retrieval
        var recentActivities = await _dataAccess.GetRecentActivitiesAsync(2);
        Assert.That(recentActivities.Count, Is.EqualTo(2));
        
        // Verify order (most recent first)
        Assert.That(recentActivities[0].ActiveWindowTitle, Is.EqualTo("Notepad - notes.txt"));
        Assert.That(recentActivities[1].ActiveWindowTitle, Is.EqualTo("Google Chrome - TimeTracker Documentation"));

        // Assert - Verify all activities retrieval
        var allActivities = await _dataAccess.GetRecentActivitiesAsync(10);
        Assert.That(allActivities.Count, Is.EqualTo(3));
        
        // Verify all data integrity
        Assert.That(allActivities[0].WindowsUsername, Is.EqualTo("IntegrationTestUser1"));
        Assert.That(allActivities[1].ApplicationProcessName, Is.EqualTo("chrome.exe"));
        Assert.That(allActivities[2].ActivityStatus, Is.EqualTo(ActivityStatus.Active));
    }

    [Test]
    public async Task DatabaseOperations_LargeDataSet_PerformsWell()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var activities = new List<ActivityDataModel>();

        for (int i = 0; i < 100; i++)
        {
            activities.Add(new ActivityDataModel
            {
                Timestamp = startTime.AddMinutes(i),
                WindowsUsername = $"TestUser{i % 5}",
                ActiveWindowTitle = $"Test Window {i}",
                ApplicationProcessName = $"TestApp{i % 10}.exe",
                ActivityStatus = i % 2 == 0 ? ActivityStatus.Active : ActivityStatus.Inactive
            });
        }

        // Act - Insert all activities and measure time
        var insertStartTime = DateTime.UtcNow;
        foreach (var activity in activities)
        {
            var result = await _dataAccess.InsertActivityAsync(activity);
            Assert.That(result, Is.True);
        }
        var insertDuration = DateTime.UtcNow - insertStartTime;

        // Assert - Verify performance (should complete within reasonable time)
        Assert.That(insertDuration.TotalSeconds, Is.LessThan(30), "Insert operations took too long");

        // Assert - Verify count
        var count = await _dataAccess.GetActivityCountAsync();
        Assert.That(count, Is.EqualTo(100));

        // Assert - Verify retrieval performance
        var retrievalStartTime = DateTime.UtcNow;
        var recentActivities = await _dataAccess.GetRecentActivitiesAsync(50);
        var retrievalDuration = DateTime.UtcNow - retrievalStartTime;

        Assert.That(retrievalDuration.TotalSeconds, Is.LessThan(5), "Retrieval operations took too long");
        Assert.That(recentActivities.Count, Is.EqualTo(50));
    }

    [Test]
    public async Task DatabaseOperations_SpecialCharacters_HandledCorrectly()
    {
        // Arrange
        var activityWithSpecialChars = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow,
            WindowsUsername = "Test'User\"With\\Special/Chars",
            ActiveWindowTitle = "Window with 'quotes' and \"double quotes\" and \\ backslashes",
            ApplicationProcessName = "app-with-dashes_and_underscores.exe",
            ActivityStatus = ActivityStatus.Active
        };

        // Act
        var insertResult = await _dataAccess.InsertActivityAsync(activityWithSpecialChars);
        
        // Assert
        Assert.That(insertResult, Is.True);

        var retrievedActivities = await _dataAccess.GetRecentActivitiesAsync(1);
        Assert.That(retrievedActivities.Count, Is.EqualTo(1));

        var retrievedActivity = retrievedActivities[0];
        Assert.That(retrievedActivity.WindowsUsername, Is.EqualTo(activityWithSpecialChars.WindowsUsername));
        Assert.That(retrievedActivity.ActiveWindowTitle, Is.EqualTo(activityWithSpecialChars.ActiveWindowTitle));
        Assert.That(retrievedActivity.ApplicationProcessName, Is.EqualTo(activityWithSpecialChars.ApplicationProcessName));
    }

    [Test]
    public async Task DatabaseOperations_ConcurrentAccess_HandledSafely()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        var random = new Random();

        // Act - Create multiple concurrent insert operations
        for (int i = 0; i < 20; i++)
        {
            var activityIndex = i;
            var task = Task.Run(async () =>
            {
                await Task.Delay(random.Next(0, 100)); // Random delay to simulate real-world timing
                
                var activity = new ActivityDataModel
                {
                    Timestamp = DateTime.UtcNow.AddMilliseconds(activityIndex),
                    WindowsUsername = $"ConcurrentUser{activityIndex}",
                    ActiveWindowTitle = $"Concurrent Window {activityIndex}",
                    ApplicationProcessName = $"ConcurrentApp{activityIndex}.exe",
                    ActivityStatus = ActivityStatus.Active
                };

                return await _dataAccess.InsertActivityAsync(activity);
            });
            
            tasks.Add(task);
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        Assert.That(results.All(r => r), Is.True, "Some concurrent insert operations failed");

        var finalCount = await _dataAccess.GetActivityCountAsync();
        Assert.That(finalCount, Is.EqualTo(20));
    }

    [Test]
    public async Task DatabaseOperations_EmptyStrings_HandledCorrectly()
    {
        // Arrange
        var activityWithEmptyStrings = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow,
            WindowsUsername = "",
            ActiveWindowTitle = "",
            ApplicationProcessName = "",
            ActivityStatus = ActivityStatus.Inactive
        };

        // Act
        var insertResult = await _dataAccess.InsertActivityAsync(activityWithEmptyStrings);

        // Assert
        Assert.That(insertResult, Is.True);

        var retrievedActivities = await _dataAccess.GetRecentActivitiesAsync(1);
        Assert.That(retrievedActivities.Count, Is.EqualTo(1));

        var retrievedActivity = retrievedActivities[0];
        Assert.That(retrievedActivity.WindowsUsername, Is.EqualTo(""));
        Assert.That(retrievedActivity.ActiveWindowTitle, Is.EqualTo(""));
        Assert.That(retrievedActivity.ApplicationProcessName, Is.EqualTo(""));
        Assert.That(retrievedActivity.ActivityStatus, Is.EqualTo(ActivityStatus.Inactive));
    }

    [Test]
    public async Task DatabaseOperations_TimestampPrecision_PreservedCorrectly()
    {
        // Arrange
        var preciseTimestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        var activity = new ActivityDataModel
        {
            Timestamp = preciseTimestamp,
            WindowsUsername = "TimestampTestUser",
            ActiveWindowTitle = "Timestamp Test Window",
            ApplicationProcessName = "TimestampTest.exe",
            ActivityStatus = ActivityStatus.Active
        };

        // Act
        var insertResult = await _dataAccess.InsertActivityAsync(activity);

        // Assert
        Assert.That(insertResult, Is.True);

        var retrievedActivities = await _dataAccess.GetRecentActivitiesAsync(1);
        Assert.That(retrievedActivities.Count, Is.EqualTo(1));

        var retrievedActivity = retrievedActivities[0];
        
        // SQLite stores datetime with second precision, so we compare with tolerance
        var timeDifference = Math.Abs((retrievedActivity.Timestamp - preciseTimestamp).TotalSeconds);
        Assert.That(timeDifference, Is.LessThan(1), "Timestamp precision not preserved within acceptable range");
    }
}
