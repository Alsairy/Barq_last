using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data
{
    public class BarqDbContext : IdentityDbContext<ApplicationUser, Role, Guid, 
        Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>,
        UserRole,
        Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>,
        Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>,
        Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>
    {
        public BarqDbContext(DbContextOptions<BarqDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<AIProvider> AIProviders { get; set; }
        public DbSet<AIAgent> AIAgents { get; set; }
        public DbSet<BARQ.Core.Entities.Task> Tasks { get; set; }
        public DbSet<TaskExecution> TaskExecutions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TaskDocument> TaskDocuments { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SecurityEvent> SecurityEvents { get; set; }
        public DbSet<Backup> Backups { get; set; }
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<AdminConfiguration> AdminConfigurations { get; set; }
        public DbSet<AdminConfigurationHistory> AdminConfigurationHistory { get; set; }

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

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            });
        }

        private void ConfigureEntityRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BARQ.Core.Entities.Task>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BARQ.Core.Entities.Task>()
                .HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BARQ.Core.Entities.Task>()
                .HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Template)
                .WithMany(p => p.DerivedProjects)
                .HasForeignKey(p => p.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Template>()
                .HasOne(t => t.ParentTemplate)
                .WithMany(t => t.ChildTemplates)
                .HasForeignKey(t => t.ParentTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminConfiguration>()
                .HasOne(ac => ac.ValidatedByUser)
                .WithMany()
                .HasForeignKey(ac => ac.ValidatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdminConfigurationHistory>()
                .HasOne(ach => ach.ChangedByUser)
                .WithMany()
                .HasForeignKey(ach => ach.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<AIProvider>()
                .HasIndex(ap => new { ap.TenantId, ap.Name })
                .IsUnique();

            modelBuilder.Entity<AIAgent>()
                .HasIndex(aa => new { aa.TenantId, aa.Name })
                .IsUnique();

            modelBuilder.Entity<BARQ.Core.Entities.Task>()
                .HasIndex(t => new { t.TenantId, t.Status });

            modelBuilder.Entity<BARQ.Core.Entities.Task>()
                .HasIndex(t => new { t.TenantId, t.AssignedToId });

            modelBuilder.Entity<Project>()
                .HasIndex(p => new { p.TenantId, p.Status });

            modelBuilder.Entity<Document>()
                .HasIndex(d => new { d.TenantId, d.DocumentType });

            modelBuilder.Entity<Workflow>()
                .HasIndex(w => new { w.TenantId, w.WorkflowType });

            modelBuilder.Entity<WorkflowInstance>()
                .HasIndex(wi => new { wi.TenantId, wi.Status });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => new { al.TenantId, al.EntityType, al.EntityId });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Timestamp);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.TenantId, n.UserId, n.IsRead });

            modelBuilder.Entity<SecurityEvent>()
                .HasIndex(se => new { se.TenantId, se.EventType, se.Timestamp });

            modelBuilder.Entity<AdminConfiguration>()
                .HasIndex(ac => new { ac.TenantId, ac.ConfigurationKey })
                .IsUnique();
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
