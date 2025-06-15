using System.ComponentModel.DataAnnotations;

namespace TimeTracker.API.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string SessionId { get; set; } = string.Empty;
    }
}
