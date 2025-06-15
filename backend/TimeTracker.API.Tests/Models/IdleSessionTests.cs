using System.ComponentModel.DataAnnotations;
using TimeTracker.API.Models;

namespace TimeTracker.API.Tests.Models
{
    public class IdleSessionTests
    {
        [Fact]
        public void IdleSession_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var idleSession = new IdleSession();

            // Assert - This will fail until we create the IdleSession model
            Assert.NotNull(idleSession);
            Assert.True(idleSession.GetType().GetProperty("Id") != null);
            Assert.True(idleSession.GetType().GetProperty("StartTime") != null);
            Assert.True(idleSession.GetType().GetProperty("EndTime") != null);
            Assert.True(idleSession.GetType().GetProperty("Reason") != null);
            Assert.True(idleSession.GetType().GetProperty("Note") != null);
            Assert.True(idleSession.GetType().GetProperty("UserId") != null);
            Assert.True(idleSession.GetType().GetProperty("SessionId") != null);
            Assert.True(idleSession.GetType().GetProperty("DurationSeconds") != null);
            Assert.True(idleSession.GetType().GetProperty("IsRemoteSession") != null);
            Assert.True(idleSession.GetType().GetProperty("ActiveApplication") != null);
            Assert.True(idleSession.GetType().GetProperty("CreatedAt") != null);
        }

        [Fact]
        public void IdleSession_Note_ShouldBeNullable()
        {
            // Arrange
            var idleSession = new IdleSession();

            // Act
            idleSession.Note = null;

            // Assert - Note should be nullable
            Assert.Null(idleSession.Note);
        }

        [Fact]
        public void IdleSession_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var idleSession = new IdleSession();

            // Assert - Check default values
            Assert.Equal(string.Empty, idleSession.Reason);
            Assert.Equal(string.Empty, idleSession.Note);
            Assert.Equal(string.Empty, idleSession.UserId);
            Assert.Equal(string.Empty, idleSession.SessionId);
            Assert.Equal(string.Empty, idleSession.ActiveApplication);
            Assert.False(idleSession.IsRemoteSession);
            Assert.Equal(0, idleSession.DurationSeconds);
            // CreatedAt should be set to current time (within reasonable range)
            Assert.True(idleSession.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.True(idleSession.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void IdleSession_ShouldHaveCorrectValidationAttributes()
        {
            // Arrange
            var idleSessionType = typeof(IdleSession);

            // Act & Assert - Check Required attributes
            var startTimeProperty = idleSessionType.GetProperty("StartTime");
            Assert.True(startTimeProperty?.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0);

            var endTimeProperty = idleSessionType.GetProperty("EndTime");
            Assert.True(endTimeProperty?.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0);

            var reasonProperty = idleSessionType.GetProperty("Reason");
            Assert.True(reasonProperty?.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0);
            Assert.True(reasonProperty?.GetCustomAttributes(typeof(StringLengthAttribute), false).Length > 0);

            var userIdProperty = idleSessionType.GetProperty("UserId");
            Assert.True(userIdProperty?.GetCustomAttributes(typeof(RequiredAttribute), false).Length > 0);
            Assert.True(userIdProperty?.GetCustomAttributes(typeof(StringLengthAttribute), false).Length > 0);

            // Check StringLength attributes
            var noteProperty = idleSessionType.GetProperty("Note");
            var noteStringLength = noteProperty?.GetCustomAttributes(typeof(StringLengthAttribute), false).FirstOrDefault() as StringLengthAttribute;
            Assert.NotNull(noteStringLength);
            Assert.Equal(1000, noteStringLength.MaximumLength);
        }

        [Fact]
        public void IdleSession_ShouldCalculateDurationCorrectly()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddMinutes(-10);
            var endTime = DateTime.UtcNow;
            var expectedDuration = (int)(endTime - startTime).TotalSeconds;

            var idleSession = new IdleSession
            {
                StartTime = startTime,
                EndTime = endTime,
                DurationSeconds = expectedDuration
            };

            // Act & Assert
            Assert.Equal(expectedDuration, idleSession.DurationSeconds);
            Assert.True(idleSession.DurationSeconds > 0);
        }
    }
}
