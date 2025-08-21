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
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<PerformanceMetric> PerformanceMetrics => Set<PerformanceMetric>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<Backup> Backups => Set<Backup>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<AdminConfiguration> AdminConfigurations => Set<AdminConfiguration>();
    public DbSet<AdminConfigurationHistory> AdminConfigurationHistory => Set<AdminConfigurationHistory>();
    
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<FileAttachmentAccess> FileAttachmentAccesses => Set<FileAttachmentAccess>();
    public DbSet<FileQuarantine> FileQuarantines => Set<FileQuarantine>();
    
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditReport> AuditReports => Set<AuditReport>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<UsageQuota> UsageQuotas => Set<UsageQuota>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<UserLanguagePreference> UserLanguagePreferences => Set<UserLanguagePreference>();
    public DbSet<AccessibilityAudit> AccessibilityAudits => Set<AccessibilityAudit>();
    public DbSet<AccessibilityIssue> AccessibilityIssues => Set<AccessibilityIssue>();
    
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureFlagHistory> FeatureFlagHistory => Set<FeatureFlagHistory>();
    public DbSet<TenantState> TenantStates => Set<TenantState>();
    public DbSet<TenantStateHistory> TenantStateHistory => Set<TenantStateHistory>();
    public DbSet<ImpersonationSession> ImpersonationSessions => Set<ImpersonationSession>();
    public DbSet<ImpersonationAction> ImpersonationActions => Set<ImpersonationAction>();
    public DbSet<SystemHealth> SystemHealth => Set<SystemHealth>();
    public DbSet<SlaViolation> SlaViolations => Set<SlaViolation>();
    public DbSet<EscalationAction> EscalationActions => Set<EscalationAction>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<SlaPolicy> SlaPolicy => Set<SlaPolicy>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(BarqDbContext).Assembly);

        AddTenantFilter(builder);      // TenantEntity: TenantId == current tenant
        AddSoftDeleteFilter(builder);  // BaseEntity: !IsDeleted (except special cases like AuditLog if not soft-deleted)
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
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => tenantId != Guid.Empty && e.TenantId == tenantId);
    }

    private void AddSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;

            var isIdentityTable =
                typeof(IdentityUser<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityRole<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserClaim<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserRole<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserLogin<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityRoleClaim<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserToken<Guid>).IsAssignableFrom(clr);

            var isAuditLog = typeof(AuditLog).IsAssignableFrom(clr);

            if (isIdentityTable || isAuditLog) continue;

            if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var param = System.Linq.Expressions.Expression.Parameter(clr, "e");
                var isDeletedProp = System.Linq.Expressions.Expression.PropertyOrField(param, nameof(BaseEntity.IsDeleted));
                var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProp);
                var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, param);
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
