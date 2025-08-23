using System.Reflection;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using BARQ.Core.Entities;
using BARQ.Core.Services;

namespace BARQ.Infrastructure.Data;

public sealed class BarqDbContext
    : IdentityDbContext<ApplicationUser, Role, Guid,
                        IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>,
                        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    private readonly ITenantProvider _tenantProvider;
    
    internal Guid CurrentTenantId => _tenantProvider.GetTenantId();

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
    public DbSet<SystemHealth> SystemHealths => Set<SystemHealth>();
    
    public DbSet<BusinessCalendar> BusinessCalendars => Set<BusinessCalendar>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<SlaViolation> SlaViolations => Set<SlaViolation>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<EscalationAction> EscalationActions => Set<EscalationAction>();
    
    public DbSet<TechnologyConstraint> TechnologyConstraints => Set<TechnologyConstraint>();
    public DbSet<TemplateValidation> TemplateValidations => Set<TemplateValidation>();
    public DbSet<ConstraintViolation> ConstraintViolations => Set<ConstraintViolation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BARQ.Core.Entities.Task>()
            .HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<BARQ.Core.Entities.Task>()
            .HasOne(t => t.Creator)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ApplyConfigurationsFromAssembly(typeof(BarqDbContext).Assembly);

        AddTenantFilter(builder);      // TenantEntity: TenantId == current tenant
        AddSoftDeleteFilter(builder);  // BaseEntity: !IsDeleted (except special cases like AuditLog if not soft-deleted)
    }

    private void AddTenantFilter(ModelBuilder modelBuilder)
    {
        var setGlobalQuery = typeof(BarqDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = setGlobalQuery.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantEntity
    {
        // tenant + soft-delete combined for TenantEntity
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => CurrentTenantId != Guid.Empty
                                 && e.TenantId == CurrentTenantId
                                 && !e.IsDeleted);
    }

    private void AddSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;

            var isIdentity =
                typeof(IdentityUser<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityRole<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserClaim<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserRole<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserLogin<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityRoleClaim<Guid>).IsAssignableFrom(clr) ||
                typeof(IdentityUserToken<Guid>).IsAssignableFrom(clr);

            var isAuditLog = typeof(AuditLog).IsAssignableFrom(clr);

            if (isIdentity || isAuditLog) continue;

            if (typeof(TenantEntity).IsAssignableFrom(clr)) continue;

            if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var param = Expression.Parameter(clr, "e");
                var isDeletedProp = Expression.PropertyOrField(param, nameof(BaseEntity.IsDeleted));
                var notDeleted = Expression.Not(isDeletedProp);

                var existing = entityType.GetQueryFilter();
                Expression body = notDeleted;
                if (existing != null)
                {
                    var replaced = ParameterReplacer.Replace(existing.Parameters[0], param, existing.Body);
                    body = Expression.AndAlso(replaced!, notDeleted);
                }

                modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body, param));
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

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;
    private ParameterReplacer(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
    public static Expression? Replace(ParameterExpression from, ParameterExpression to, Expression? node)
        => node is null ? null : new ParameterReplacer(from, to).Visit(node);
    protected override Expression VisitParameter(ParameterExpression node)
        => node == _from ? _to : base.VisitParameter(node);
}
