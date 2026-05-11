using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Shared.Data
{
    public class LifeTrackDbContext : DbContext
    {
        public LifeTrackDbContext(
            DbContextOptions<LifeTrackDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Protocol> Protocols { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<AdverseEvent> AdverseEvents { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            // ── User ──────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("User");
                e.HasKey(u => u.UserID);
                e.Property(u => u.IsActive)
                    .HasDefaultValue(true);
                //e.Ignore(u => u.RoleID);
                // RoleID stays — connected to Role table
            });

            // ── Role ──────────────────────────────────────
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Role");
                e.HasKey(r => r.RoleID);
            });

            // ── Patient ───────────────────────────────────
            modelBuilder.Entity<Patient>(e =>
            {
                e.ToTable("Patient");
                e.HasKey(p => p.PatientID);
                e.Property(p => p.EnrollmentStatus)
                    .HasDefaultValue("Pending");
            });

            // ── Protocol ──────────────────────────────────
            modelBuilder.Entity<Protocol>(e =>
            {
                e.ToTable("Protocol");
                e.HasKey(p => p.ProtocolID);
            });

            // ── Site ──────────────────────────────────────
            modelBuilder.Entity<Site>(e =>
            {
                e.ToTable("Site");
                e.HasKey(s => s.SiteID);
            });

            // ── Visit ─────────────────────────────────────
            modelBuilder.Entity<Visit>(e =>
            {
                e.ToTable("Visit");
                e.HasKey(v => v.VisitID);
            });

            // ── AdverseEvent ──────────────────────────────
            modelBuilder.Entity<AdverseEvent>(e =>
            {
                e.ToTable("AdverseEvent");
                e.HasKey(a => a.EventID);
            });

            // ── Deviation ─────────────────────────────────
            modelBuilder.Entity<Deviation>(e =>
            {
                e.ToTable("Deviation");
                e.HasKey(d => d.DeviationID);
            });

            // ── Notification ──────────────────────────────
            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("Notification");
                e.HasKey(n => n.NotificationID);
            });
        }
    }
}