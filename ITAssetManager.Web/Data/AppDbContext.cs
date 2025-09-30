// Data/AppDbContext.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using ITAssetManager.Web.Models;

namespace ITAssetManager.Web.Data
{
    /// <summary>
    /// Design-time factory so EF tools can create the DbContext without running Program.cs.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true) // uncomment if you want
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection")
                     ?? "Server=(localdb)\\MSSQLLocalDB;Database=ITAssetManager;Trusted_Connection=true;MultipleActiveResultSets=true";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs, sqlServerOptions =>
                {
                    // Enable retry on failure for SQL Server
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                })
                .Options;

            return new AppDbContext(options);
        }
    }

    /// <summary>
    /// Primary EF Core DbContext. If you are NOT using Identity, change the base to DbContext.
    /// </summary>
    public class AppDbContext : IdentityDbContext<IdentityUser> // or: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<Person> People => Set<Person>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<SoftwareProduct> SoftwareProducts => Set<SoftwareProduct>();
        public DbSet<SoftwareLicense> SoftwareLicenses { get; set; } = null!;
        public DbSet<LicenseAssignment> LicenseAssignments => Set<LicenseAssignment>();
        public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
        public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Avoid cascade cycles (safe default for CRUD apps)
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            // Sample precision config (optional but nice)
            b.Entity<Asset>().Property(a => a.PurchaseCost).HasPrecision(18, 2);
            b.Entity<SoftwareProduct>().Property(s => s.UnitCost).HasPrecision(18, 2);

            // Simple relationships (EF can infer these, but being explicit helps readability)
            b.Entity<LicenseAssignment>()
                .HasOne(x => x.SoftwareProduct)
                .WithMany(s => s.Assignments)
                .HasForeignKey(x => x.SoftwareProductId);

            b.Entity<LicenseAssignment>()
                .HasOne(x => x.Asset)
                .WithMany(a => a.LicenseAssignments)
                .HasForeignKey(x => x.AssetId);

            b.Entity<LicenseAssignment>()
                .HasOne(x => x.Person)
                .WithMany()
                .HasForeignKey(x => x.PersonId);

            b.Entity<MaintenanceTicket>()
                .HasOne(t => t.Asset)
                .WithMany(a => a.Tickets)
                .HasForeignKey(t => t.AssetId);

            // SQL Server specific configurations (optional)
            b.Entity<ChangeLog>()
                .Property(c => c.At)
                .HasDefaultValueSql("GETUTCDATE()"); // SQL Server function for UTC timestamp

            // Indexes for better performance (optional but recommended)
            b.Entity<Asset>()
                .HasIndex(a => a.SerialNumber)
                .IsUnique();

            b.Entity<SoftwareProduct>()
                .HasIndex(s => new { s.Name, s.Version })
                .IsUnique();

            b.Entity<LicenseAssignment>()
                .HasIndex(la => new { la.SoftwareProductId, la.AssetId })
                .IsUnique();
        }

        /// <summary>
        /// Lightweight audit log. Guarded so it won't break the very first migration.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var logs = new List<ChangeLog>();
            var username = "system"; // wire your identity user name in controllers later

            foreach (var e in ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged))
            {
                var entity = e.Entity.GetType().Name;
                var key = e.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";
                logs.Add(new ChangeLog
                {
                    Entity = entity,
                    Key = key,
                    Action = e.State.ToString(),
                    UserName = username,
                    At = DateTime.UtcNow
                });
            }

            var result = await base.SaveChangesAsync(ct);

            if (logs.Count > 0)
            {
                try
                {
                    ChangeLogs.AddRange(logs);
                    await base.SaveChangesAsync(ct);
                }
                catch
                {
                    // Swallow errors during initial migration/creation when ChangeLogs table may not exist yet.
                    // Remove this catch if you prefer strict failures.
                }
            }

            return result;
        }
    }
}