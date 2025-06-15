using System.ComponentModel.DataAnnotations;

namespace TimeTracker.API.Models
{
    public class Screenshot
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        [StringLength(500)]
        public string OriginalImageUrl { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string ThumbnailUrl { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string SessionId { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
    }
}
