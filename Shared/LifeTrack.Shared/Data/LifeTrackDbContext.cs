// ============================================================
// Shared.CL / Data / LifeTrackDbContext.cs
// Main DB — points to LifeTrackDB
// This context owns ALL migrations
// ============================================================

using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.Shared.Data
{
    public class LifeTrackDbContext : DbContext
    {
        public LifeTrackDbContext(
            DbContextOptions<LifeTrackDbContext> options)
            : base(options) { }

        // ── DbSets ───────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Protocol> Protocols { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SiteProtocol> SiteProtocols { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<AdverseEvent> AdverseEvents { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<KPIReport> KPIReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Seed Roles ───────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Admin" },
                new Role { RoleID = 2, RoleName = "ClinicalTrialManager" },
                new Role { RoleID = 3, RoleName = "Investigator" },
                new Role { RoleID = 4, RoleName = "Patient" },
                new Role { RoleID = 5, RoleName = "RegulatoryOfficer" },
                new Role { RoleID = 6, RoleName = "DataManager" }
            );

            // ── User ─────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ── Patient ──────────────────────────────────────
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // ── SiteProtocol ─────────────────────────────────
            modelBuilder.Entity<SiteProtocol>()
                .HasOne(sp => sp.Site)
                .WithMany(s => s.SiteProtocols)
                .HasForeignKey(sp => sp.SiteID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SiteProtocol>()
                .HasOne(sp => sp.Protocol)
                .WithMany(p => p.SiteProtocols)
                .HasForeignKey(sp => sp.ProtocolID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SiteProtocol>()
                .HasOne(sp => sp.Investigator)
                .WithMany()
                .HasForeignKey(sp => sp.InvestigatorID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Enrollment ───────────────────────────────────
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Patient)
                .WithMany(p => p.Enrollments)
                .HasForeignKey(e => e.PatientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.SiteProtocol)
                .WithMany(sp => sp.Enrollments)
                .HasForeignKey(e => e.SiteProtocolID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Visit ────────────────────────────────────────
            modelBuilder.Entity<Visit>()
                .HasOne(v => v.Enrollment)
                .WithMany(e => e.Visits)
                .HasForeignKey(v => v.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── AdverseEvent ─────────────────────────────────
            modelBuilder.Entity<AdverseEvent>()
                .HasOne(ae => ae.Patient)
                .WithMany(p => p.AdverseEvents)
                .HasForeignKey(ae => ae.PatientID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Deviation ────────────────────────────────────
            modelBuilder.Entity<Deviation>()
                .HasOne(d => d.SiteProtocol)
                .WithMany(sp => sp.Deviations)
                .HasForeignKey(d => d.SiteProtocolID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Document ─────────────────────────────────────
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Protocol)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProtocolID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Uploader)
                .WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Notification ─────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── KPIReport ────────────────────────────────────
            modelBuilder.Entity<KPIReport>()
                .HasOne(k => k.Protocol)
                .WithMany(p => p.KPIReports)
                .HasForeignKey(k => k.ProtocolID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KPIReport>()
                .Property(k => k.EnrollmentRate)
                .HasPrecision(18, 4);

            modelBuilder.Entity<KPIReport>()
                .Property(k => k.DropoutRate)
                .HasPrecision(18, 4);
        }
    }
}