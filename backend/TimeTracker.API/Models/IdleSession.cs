using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTracker.API.Models
{
    [Table("idle_sessions")]
    public class IdleSession
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Note { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string SessionId { get; set; } = string.Empty;
        
        public int DurationSeconds { get; set; }
        
        public bool IsRemoteSession { get; set; }
        
        [StringLength(100)]
        public string ActiveApplication { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
