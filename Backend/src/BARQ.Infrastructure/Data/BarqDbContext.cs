using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data
{
    public class BarqDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public BarqDbContext(DbContextOptions<BarqDbContext> options) : base(options)
        {
        }

        
        public DbSet<Language> Languages { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<UserLanguagePreference> UserLanguagePreferences { get; set; }
        public DbSet<AccessibilityAudit> AccessibilityAudits { get; set; }
        public DbSet<AccessibilityIssue> AccessibilityIssues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.AddSoftDeleteQueryFilter();
            
            ConfigureIdentityTables(modelBuilder);
            ConfigureEntityRelationships(modelBuilder);
            ConfigureIndexes(modelBuilder);
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BarqDbContext).Assembly);
        }

        private void ConfigureIdentityTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();
            });
        }

        private void ConfigureEntityRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Translation>()
                .HasOne<Language>()
                .WithMany(l => l.Translations)
                .HasForeignKey(t => t.LanguageCode)
                .HasPrincipalKey(l => l.Code)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserLanguagePreference>()
                .HasOne(ulp => ulp.User)
                .WithMany(u => u.LanguagePreferences)
                .HasForeignKey(ulp => ulp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccessibilityIssue>()
                .HasOne<AccessibilityAudit>()
                .WithMany(aa => aa.Issues)
                .HasForeignKey(ai => ai.AccessibilityAuditId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Language>()
                .HasIndex(l => l.Code)
                .IsUnique();

            modelBuilder.Entity<Language>()
                .HasIndex(l => new { l.TenantId, l.IsEnabled });

            modelBuilder.Entity<Translation>()
                .HasIndex(t => new { t.LanguageCode, t.Key })
                .IsUnique();

            modelBuilder.Entity<Translation>()
                .HasIndex(t => new { t.TenantId, t.Category, t.IsActive });

            modelBuilder.Entity<UserLanguagePreference>()
                .HasIndex(ulp => new { ulp.UserId, ulp.LanguageCode })
                .IsUnique();

            modelBuilder.Entity<UserLanguagePreference>()
                .HasIndex(ulp => new { ulp.TenantId, ulp.IsDefault });

            modelBuilder.Entity<AccessibilityAudit>()
                .HasIndex(aa => new { aa.TenantId, aa.Status });

            modelBuilder.Entity<AccessibilityAudit>()
                .HasIndex(aa => new { aa.AuditedBy, aa.AuditDate });

            modelBuilder.Entity<AccessibilityIssue>()
                .HasIndex(ai => new { ai.AccessibilityAuditId, ai.Severity });

            modelBuilder.Entity<AccessibilityIssue>()
                .HasIndex(ai => new { ai.TenantId, ai.Status, ai.Priority });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var auditableEntries = ChangeTracker.Entries<IAuditable>();

            foreach (var entry in auditableEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.Version = 1;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.Version++;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.Version++;
                        break;
                }
            }
        }
    }
}
