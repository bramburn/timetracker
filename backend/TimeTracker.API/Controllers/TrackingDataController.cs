using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}
