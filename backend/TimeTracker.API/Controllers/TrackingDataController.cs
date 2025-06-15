using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TimeTracker.API.Data;
using TimeTracker.API.Models;
using TimeTracker.API.Services;

namespace TimeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrackingDataController : ControllerBase
    {
        private readonly TimeTrackerDbContext _context;
        private readonly IS3Service _s3Service;
        private readonly ILogger<TrackingDataController> _logger;

        public TrackingDataController(
            TimeTrackerDbContext context, 
            IS3Service s3Service,
            ILogger<TrackingDataController> logger)
        {
            _context = context;
            _s3Service = s3Service;
            _logger = logger;
        }

        [HttpPost("activity")]
        public async Task<IActionResult> UploadActivity([FromBody] List<ActivityLogDto> activityLogs)
        {
            try
            {
                if (!activityLogs.Any())
                {
                    return BadRequest("No activity logs provided");
                }

                var entities = activityLogs.Select(dto => new ActivityLog
                {
                    Timestamp = dto.Timestamp,
                    EventType = dto.EventType,
                    Details = dto.Details,
                    UserId = dto.UserId,
                    SessionId = dto.SessionId
                }).ToList();

                await _context.ActivityLogs.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved {Count} activity logs for user {UserId}", 
                    entities.Count, entities.First().UserId);

                return Ok(new { message = "Activity logs saved successfully", count = entities.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save activity logs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("screenshots")]
        public async Task<IActionResult> UploadScreenshot([FromForm] IFormFile file, [FromForm] string userId, [FromForm] string sessionId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("UserId is required");
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest("Only JPEG and PNG files are allowed");
                }

                // Upload to S3
                var (originalUrl, thumbnailUrl) = await _s3Service.UploadScreenshotAsync(file, userId);

                // Save metadata to database
                var screenshot = new Screenshot
                {
                    Timestamp = DateTime.UtcNow,
                    OriginalImageUrl = originalUrl,
                    ThumbnailUrl = thumbnailUrl,
                    UserId = userId,
                    SessionId = sessionId,
                    FileSize = file.Length
                };

                await _context.Screenshots.AddAsync(screenshot);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed screenshot for user {UserId}", userId);

                return Ok(new 
                { 
                    message = "Screenshot uploaded successfully",
                    id = screenshot.Id,
                    originalUrl = originalUrl,
                    thumbnailUrl = thumbnailUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload screenshot for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("idletime")]
        public async Task<IActionResult> UploadIdleTime([FromBody] IdleSessionDto idleSession)
        {
            try
            {
                if (idleSession == null)
                {
                    return BadRequest(new ErrorResponseDto { Error = "No idle session data provided" });
                }

                // Validate the idle session data
                if (string.IsNullOrEmpty(idleSession.UserId) ||
                    idleSession.StartTime >= idleSession.EndTime)
                {
                    return BadRequest(new ErrorResponseDto { Error = "Invalid idle session data" });
                }

                // Calculate duration
                var durationSeconds = (int)(idleSession.EndTime - idleSession.StartTime).TotalSeconds;

                var entity = new IdleSession
                {
                    StartTime = idleSession.StartTime,
                    EndTime = idleSession.EndTime,
                    Reason = idleSession.Reason ?? "Idle",
                    Note = idleSession.Note ?? string.Empty,
                    UserId = idleSession.UserId,
                    SessionId = idleSession.SessionId ?? string.Empty,
                    DurationSeconds = durationSeconds,
                    IsRemoteSession = idleSession.IsRemoteSession,
                    ActiveApplication = idleSession.ActiveApplication ?? string.Empty
                };

                await _context.IdleSessions.AddAsync(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved idle session for user {UserId}, duration {Duration}s",
                    entity.UserId, durationSeconds);

                return Ok(new IdleSessionResponseDto
                {
                    Message = "Idle session saved successfully",
                    Id = entity.Id,
                    Duration = durationSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save idle session");
                return StatusCode(500, new ErrorResponseDto { Error = "Internal server error" });
            }
        }
    }

    // DTOs
    public class ActivityLogDto
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class IdleSessionDto
    {
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [StringLength(50)]
        public string? Reason { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SessionId { get; set; }

        public bool IsRemoteSession { get; set; }

        [StringLength(100)]
        public string? ActiveApplication { get; set; }
    }

    public class IdleSessionResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int Id { get; set; }
        public int Duration { get; set; }
    }

    public class ErrorResponseDto
    {
        public string Error { get; set; } = string.Empty;
    }
}
