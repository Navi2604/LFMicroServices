// ============================================================
// Shared.CL / Data / AuditDbContext.cs
// Separate context for LifeTrackAuditDB
// ============================================================

using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Shared.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(
            DbContextOptions<AuditDbContext> options)
            : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Index for fast filtering by user
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserID);

            // Index for date-range queries
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.ActionTime);

            // Index for action-type filtering
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Action);
        }
    }
}