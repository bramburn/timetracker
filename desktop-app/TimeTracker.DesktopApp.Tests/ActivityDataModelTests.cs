using TimeTracker.DesktopApp;

namespace TimeTracker.DesktopApp.Tests;

[TestFixture]
public class ActivityDataModelTests
{
    [Test]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var model = new ActivityDataModel();

        // Assert
        Assert.That(model.Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(model.WindowsUsername, Is.EqualTo(string.Empty));
        Assert.That(model.ActiveWindowTitle, Is.EqualTo(string.Empty));
        Assert.That(model.ApplicationProcessName, Is.EqualTo(string.Empty));
        Assert.That(model.ActivityStatus, Is.EqualTo(ActivityStatus.Inactive));
        Assert.That(model.ActiveWindowHandle, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void Clone_ReturnsDeepCopy()
    {
        // Arrange
        var original = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            WindowsUsername = "TestUser",
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe",
            ActivityStatus = ActivityStatus.Active,
            ActiveWindowHandle = new IntPtr(12345)
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.That(clone, Is.Not.SameAs(original));
        Assert.That(clone.Timestamp, Is.EqualTo(original.Timestamp));
        Assert.That(clone.WindowsUsername, Is.EqualTo(original.WindowsUsername));
        Assert.That(clone.ActiveWindowTitle, Is.EqualTo(original.ActiveWindowTitle));
        Assert.That(clone.ApplicationProcessName, Is.EqualTo(original.ApplicationProcessName));
        Assert.That(clone.ActivityStatus, Is.EqualTo(original.ActivityStatus));
        Assert.That(clone.ActiveWindowHandle, Is.EqualTo(original.ActiveWindowHandle));
    }

    [Test]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new ActivityDataModel
        {
            WindowsUsername = "OriginalUser",
            ActiveWindowTitle = "Original Window"
        };
        var clone = original.Clone();

        // Act
        clone.WindowsUsername = "ModifiedUser";
        clone.ActiveWindowTitle = "Modified Window";

        // Assert
        Assert.That(original.WindowsUsername, Is.EqualTo("OriginalUser"));
        Assert.That(original.ActiveWindowTitle, Is.EqualTo("Original Window"));
        Assert.That(clone.WindowsUsername, Is.EqualTo("ModifiedUser"));
        Assert.That(clone.ActiveWindowTitle, Is.EqualTo("Modified Window"));
    }

    [Test]
    public void HasSignificantChanges_WithNullOther_ReturnsTrue()
    {
        // Arrange
        var model = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };

        // Act & Assert
        Assert.That(model.HasSignificantChanges(null), Is.True);
    }

    [Test]
    public void HasSignificantChanges_WithSameValues_ReturnsFalse()
    {
        // Arrange
        var model1 = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };
        var model2 = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };

        // Act & Assert
        Assert.That(model1.HasSignificantChanges(model2), Is.False);
    }

    [Test]
    public void HasSignificantChanges_WithDifferentWindowTitle_ReturnsTrue()
    {
        // Arrange
        var model1 = new ActivityDataModel
        {
            ActiveWindowTitle = "Window 1",
            ApplicationProcessName = "TestApp.exe"
        };
        var model2 = new ActivityDataModel
        {
            ActiveWindowTitle = "Window 2",
            ApplicationProcessName = "TestApp.exe"
        };

        // Act & Assert
        Assert.That(model1.HasSignificantChanges(model2), Is.True);
    }

    [Test]
    public void HasSignificantChanges_WithDifferentProcessName_ReturnsTrue()
    {
        // Arrange
        var model1 = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "App1.exe"
        };
        var model2 = new ActivityDataModel
        {
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "App2.exe"
        };

        // Act & Assert
        Assert.That(model1.HasSignificantChanges(model2), Is.True);
    }

    [Test]
    public void HasSignificantChanges_IgnoresTimestampAndUsername()
    {
        // Arrange
        var model1 = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow,
            WindowsUsername = "User1",
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };
        var model2 = new ActivityDataModel
        {
            Timestamp = DateTime.UtcNow.AddMinutes(5),
            WindowsUsername = "User2",
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe"
        };

        // Act & Assert
        Assert.That(model1.HasSignificantChanges(model2), Is.False);
    }

    [Test]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var model = new ActivityDataModel
        {
            Timestamp = timestamp,
            WindowsUsername = "TestUser",
            ActiveWindowTitle = "Test Window",
            ApplicationProcessName = "TestApp.exe",
            ActivityStatus = ActivityStatus.Active
        };

        // Act
        var result = model.ToString();

        // Assert
        var expected = "[2024-01-15 10:30:45] TestUser - TestApp.exe: Test Window (Active)";
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_WithInactiveStatus_ReturnsCorrectFormat()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 14, 22, 33, DateTimeKind.Utc);
        var model = new ActivityDataModel
        {
            Timestamp = timestamp,
            WindowsUsername = "User123",
            ActiveWindowTitle = "Notepad",
            ApplicationProcessName = "notepad.exe",
            ActivityStatus = ActivityStatus.Inactive
        };

        // Act
        var result = model.ToString();

        // Assert
        var expected = "[2024-01-15 14:22:33] User123 - notepad.exe: Notepad (Inactive)";
        Assert.That(result, Is.EqualTo(expected));
    }
}
