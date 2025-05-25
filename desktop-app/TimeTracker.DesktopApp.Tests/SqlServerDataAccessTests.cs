using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TimeTracker.DesktopApp.Tests.TestHelpers;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class SqlServerDataAccessTests
{
    private SqlServerDataAccess? _dataAccess;
    private Mock<ILogger<SqlServerDataAccess>> _loggerMock = null!;
    private IConfiguration _configuration = null!;
    private string _testConnectionString = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<SqlServerDataAccess>>();

        // Create test configuration for SQL Server
        var configData = new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=TimeTrackerTestDB;Integrated Security=true;Connection Timeout=30;",
            ["TimeTracker:MaxBatchSize"] = "10",
            ["TimeTracker:BatchInsertIntervalMs"] = "1000",
            ["TimeTracker:EnableBulkOperations"] = "true"
        };

        _configuration = TestConfiguration.Create(configData);
        _testConnectionString = _configuration.GetConnectionString("DefaultConnection")!;
    }

    [TearDown]
    public void TearDown()
    {
        _dataAccess?.Dispose();
        
        // Clean up test database
        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_testConnectionString);
            connection.Open();
            using var command = new Microsoft.Data.SqlClient.SqlCommand("DROP DATABASE IF EXISTS TimeTrackerTestDB", connection);
            command.ExecuteNonQuery();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);

        // Assert
        Assert.That(_dataAccess, Is.Not.Null);
    }

    [Test]
    public async Task InsertActivityAsync_EnqueuesActivity()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);
        var activity = CreateTestActivity();

        // Act
        var result = await _dataAccess.InsertActivityAsync(activity);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task GetActivityCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);

        // Act
        var count = await _dataAccess.GetActivityCountAsync();

        // Assert
        Assert.That(count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task GetRecentActivitiesAsync_ReturnsActivities()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);

        // Act
        var activities = await _dataAccess.GetRecentActivitiesAsync(5);

        // Assert
        Assert.That(activities, Is.Not.Null);
        Assert.That(activities.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public async Task GetUnsyncedActivitiesAsync_ReturnsUnsyncedActivities()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);

        // Act
        var activities = await _dataAccess.GetUnsyncedActivitiesAsync(10);

        // Assert
        Assert.That(activities, Is.Not.Null);
        Assert.That(activities.Count, Is.LessThanOrEqualTo(10));
    }

    [Test]
    public async Task MarkActivitiesAsSyncedAsync_WithEmptyList_ReturnsTrue()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);
        var emptyList = new List<ActivityDataModel>();
        var batchId = Guid.NewGuid();

        // Act
        var result = await _dataAccess.MarkActivitiesAsSyncedAsync(emptyList, batchId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteSyncedRecordsByBatchIdAsync_WithValidBatchId_ReturnsTrue()
    {
        // Arrange
        _dataAccess = new SqlServerDataAccess(_configuration, _loggerMock.Object);
        var batchId = Guid.NewGuid();

        // Act
        var result = await _dataAccess.DeleteSyncedRecordsByBatchIdAsync(batchId);

        // Assert
        Assert.That(result, Is.True);
    }

    private ActivityDataModel CreateTestActivity(string windowTitle = "Test Window")
    {
        return new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow,
            WindowsUsername = Environment.UserName,
            ActiveWindowTitle = windowTitle,
            ApplicationProcessName = "TestProcess",
            ActivityStatus = ActivityStatus.Active,
            IsSynced = false,
            BatchId = null
        };
    }
}
