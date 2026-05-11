using System.Reflection.Emit;
using LifeTrack.AuditService.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.AuditService.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(
            DbContextOptions<AuditDbContext> options)
            : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLog");
                e.HasKey(a => a.AuditID);
                e.Property(a => a.AuditID)
                    .ValueGeneratedOnAdd();
                e.Property(a => a.Timestamp)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}