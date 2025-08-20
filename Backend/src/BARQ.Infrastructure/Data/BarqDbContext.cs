using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BARQ.Core.Entities;
using BARQ.Core.Services;

namespace BARQ.Infrastructure.Data;

public sealed class BarqDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid,
                        IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>,
                        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    private readonly ITenantProvider _tenantProvider;

    public BarqDbContext(DbContextOptions<BarqDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AIProvider> AIProviders => Set<AIProvider>();
    public DbSet<AIAgent> AIAgents => Set<AIAgent>();
    public DbSet<BARQ.Core.Entities.Task> Tasks => Set<BARQ.Core.Entities.Task>();
    public DbSet<TaskExecution> TaskExecutions => Set<TaskExecution>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<TaskDocument> TaskDocuments => Set<TaskDocument>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<PerformanceMetric> PerformanceMetrics => Set<PerformanceMetric>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<Backup> Backups => Set<Backup>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<AdminConfiguration> AdminConfigurations => Set<AdminConfiguration>();
    public DbSet<AdminConfigurationHistory> AdminConfigurationHistory => Set<AdminConfigurationHistory>();
    
    // PR #6 DbSets (Notifications & Preferences)
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<UserLanguagePreference> UserLanguagePreferences => Set<UserLanguagePreference>();
    public DbSet<AccessibilityAudit> AccessibilityAudits => Set<AccessibilityAudit>();
    public DbSet<AccessibilityIssue> AccessibilityIssues => Set<AccessibilityIssue>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(BarqDbContext).Assembly);

        AddTenantFilter(builder);
        AddSoftDeleteFilter(builder);
    }

    private void AddTenantFilter(ModelBuilder modelBuilder)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var setGlobalQuery = typeof(BarqDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = setGlobalQuery.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder, tenantId });
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, Guid tenantId) where TEntity : TenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => tenantId != Guid.Empty && e.TenantId == tenantId && !e.IsDeleted);
    }

    private void AddSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)
                && !typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var isDeletedProp = System.Linq.Expressions.Expression.PropertyOrField(parameter, nameof(BaseEntity.IsDeleted));
                var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProp);
                var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);
                entityType.SetQueryFilter(lambda);
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyEntityAudits();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyEntityAudits();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyEntityAudits()
    {
        var now = DateTime.UtcNow;
        var tenantId = _tenantProvider.GetTenantId();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default(DateTime))
                {
                    entry.Entity.CreatedAt = now;
                }
                if (entry.Entity is TenantEntity te && tenantId != Guid.Empty)
                {
                    te.TenantId = te.TenantId == Guid.Empty ? tenantId : te.TenantId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
