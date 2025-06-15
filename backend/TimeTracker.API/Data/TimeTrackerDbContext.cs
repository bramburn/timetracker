using Microsoft.EntityFrameworkCore;
using TimeTracker.API.Models;

namespace TimeTracker.API.Data
{
    public class TimeTrackerDbContext : DbContext
    {
        public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options) : base(options)
        {
        }

        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Screenshot> Screenshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Details).HasMaxLength(1000);
                entity.Property(e => e.UserId).HasMaxLength(100);
                entity.Property(e => e.SessionId).HasMaxLength(50);
                
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
            });

            modelBuilder.Entity<Screenshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.OriginalImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ThumbnailUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SessionId).HasMaxLength(50);
                
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
            });
        }
    }
}
