using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BARQ.Application.Services;

public class EscalationService : IEscalationService
{
    private readonly BarqDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly INotificationService _notificationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(
        BarqDbContext context, 
        ITenantProvider tenantProvider,
        INotificationService notificationService,
        IHttpClientFactory httpClientFactory,
        ILogger<EscalationService> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _notificationService = notificationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<PagedResult<EscalationRule>> GetEscalationRulesAsync(Guid? slaPolicyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.EscalationRules
            .Include(r => r.SlaPolicy)
            .Where(r => r.TenantId == tenantId)
            .AsQueryable();

        if (slaPolicyId.HasValue)
        {
            query = query.Where(r => r.SlaPolicyId == slaPolicyId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Level)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EscalationRule>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async System.Threading.Tasks.Task<EscalationRule?> GetEscalationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EscalationRules
            .Include(r => r.SlaPolicy)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<EscalationRule> CreateEscalationRuleAsync(EscalationRule rule, CancellationToken cancellationToken = default)
    {
        rule.TenantId = _tenantProvider.GetTenantId();
        rule.CreatedAt = DateTime.UtcNow;
        rule.CreatedBy = null;

        _context.EscalationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created escalation rule {RuleId} for SLA policy {PolicyId}", rule.Id, rule.SlaPolicyId);
        return rule;
    }

    public async System.Threading.Tasks.Task<EscalationRule> UpdateEscalationRuleAsync(EscalationRule rule, CancellationToken cancellationToken = default)
    {
        var existing = await _context.EscalationRules.FindAsync(rule.Id);
        if (existing == null)
            throw new InvalidOperationException($"Escalation rule {rule.Id} not found");
        if (existing.TenantId != _tenantProvider.GetTenantId())
            throw new UnauthorizedAccessException("Cross-tenant update blocked");

        existing.Level = rule.Level;
        existing.TriggerAfterMinutes = rule.TriggerAfterMinutes;
        existing.ActionType = rule.ActionType;
        existing.ActionConfig = rule.ActionConfig;
        existing.IsActive = rule.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated escalation rule {RuleId}", rule.Id);
        return existing;
    }

    public async System.Threading.Tasks.Task DeleteEscalationRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.EscalationRules.FindAsync(id);
        if (rule != null)
        {
            if (rule.TenantId != _tenantProvider.GetTenantId())
                throw new UnauthorizedAccessException("Cross-tenant delete blocked");
            rule.IsDeleted = true;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted escalation rule {RuleId}", id);
        }
    }

    public async System.Threading.Tasks.Task<PagedResult<EscalationAction>> GetEscalationActionsAsync(Guid? violationId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.EscalationActions
            .Include(a => a.SlaViolation)
            .Include(a => a.EscalationRule)
            .Where(a => a.TenantId == tenantId)
            .AsQueryable();

        if (violationId.HasValue)
        {
            query = query.Where(a => a.SlaViolationId == violationId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EscalationAction>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async System.Threading.Tasks.Task<EscalationAction> CreateEscalationActionAsync(EscalationAction action, CancellationToken cancellationToken = default)
    {
        action.TenantId = _tenantProvider.GetTenantId();
        action.CreatedAt = DateTime.UtcNow;
        action.CreatedBy = null;

        _context.EscalationActions.Add(action);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created escalation action {ActionId} for violation {ViolationId}", action.Id, action.SlaViolationId);
        return action;
    }

    public async System.Threading.Tasks.Task<EscalationAction> UpdateEscalationActionAsync(EscalationAction action, CancellationToken cancellationToken = default)
    {
        var existing = await _context.EscalationActions.FindAsync(action.Id);
        if (existing == null)
            throw new InvalidOperationException($"Escalation action {action.Id} not found");

        existing.Status = action.Status;
        existing.Result = action.Result;
        existing.ErrorMessage = action.ErrorMessage;
        existing.RetryCount = action.RetryCount;
        existing.NextRetryAt = action.NextRetryAt;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated escalation action {ActionId}", action.Id);
        return existing;
    }

    public async System.Threading.Tasks.Task ProcessEscalationsAsync(CancellationToken cancellationToken = default)
    {
        var openViolations = await _context.SlaViolations
            .Include(v => v.SlaPolicy)
            .ThenInclude(p => p.EscalationRules.Where(r => r.IsActive))
            .Where(v => v.Status == "Open")
            .ToListAsync(cancellationToken);

        foreach (var violation in openViolations)
        {
            if (await ShouldEscalateAsync(violation.Id, cancellationToken))
            {
                await CreateEscalationActionsForViolationAsync(violation, cancellationToken);
            }
        }

        var pendingActions = await _context.EscalationActions
            .Where(a => a.Status == "Pending" && 
                       (a.NextRetryAt == null || a.NextRetryAt <= DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        foreach (var action in pendingActions)
        {
            await ExecuteEscalationActionAsync(action.Id, cancellationToken);
        }
    }

    public async System.Threading.Tasks.Task ExecuteEscalationActionAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        var action = await _context.EscalationActions
            .Include(a => a.SlaViolation)
            .ThenInclude(v => v.Task)
            .Include(a => a.EscalationRule)
            .FirstOrDefaultAsync(a => a.Id == actionId, cancellationToken);

        if (action == null)
        {
            _logger.LogWarning("Escalation action {ActionId} not found", actionId);
            return;
        }

        try
        {
            action.Status = "Executing";
            await _context.SaveChangesAsync(cancellationToken);

            var success = await ExecuteActionByTypeAsync(action, cancellationToken);

            action.Status = success ? "Executed" : "Failed";
            action.ExecutedAt = DateTime.UtcNow;
            
            if (!success)
            {
                action.RetryCount++;
                if (action.RetryCount < 3)
                {
                    action.Status = "Pending";
                    action.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, action.RetryCount));
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Executed escalation action {ActionId} with status {Status}", actionId, action.Status);
        }
        catch (Exception ex)
        {
            action.Status = "Failed";
            action.ErrorMessage = ex.Message;
            action.RetryCount++;
            
            if (action.RetryCount < 3)
            {
                action.Status = "Pending";
                action.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, action.RetryCount));
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to execute escalation action {ActionId}", actionId);
        }
    }

    public async System.Threading.Tasks.Task<bool> ShouldEscalateAsync(Guid violationId, CancellationToken cancellationToken = default)
    {
        var violation = await _context.SlaViolations
            .Include(v => v.SlaPolicy)
            .ThenInclude(p => p.EscalationRules.Where(r => r.IsActive))
            .FirstOrDefaultAsync(v => v.Id == violationId, cancellationToken);

        if (violation == null || violation.Status != "Open")
            return false;

        var minutesSinceViolation = (DateTime.UtcNow - violation.ViolationTime).TotalMinutes;
        var nextEscalationLevel = violation.EscalationLevel + 1;

        var nextRule = violation.SlaPolicy.EscalationRules
            .Where(r => r.Level == nextEscalationLevel)
            .FirstOrDefault();

        if (nextRule == null)
            return false;

        return minutesSinceViolation >= nextRule.TriggerAfterMinutes;
    }

    private async System.Threading.Tasks.Task CreateEscalationActionsForViolationAsync(SlaViolation violation, CancellationToken cancellationToken)
    {
        var nextEscalationLevel = violation.EscalationLevel + 1;
        var rules = violation.SlaPolicy.EscalationRules
            .Where(r => r.Level == nextEscalationLevel && r.IsActive)
            .ToList();

        foreach (var rule in rules)
        {
            var action = new EscalationAction
            {
                Id = Guid.NewGuid(),
                SlaViolationId = violation.Id,
                EscalationRuleId = rule.Id,
                ActionType = rule.ActionType,
                ActionConfig = rule.ActionConfig,
                Status = "Pending",
                TenantId = violation.TenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null
            };

            await CreateEscalationActionAsync(action, cancellationToken);
        }

        violation.EscalationLevel = nextEscalationLevel;
        violation.UpdatedAt = DateTime.UtcNow;
        violation.UpdatedBy = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async System.Threading.Tasks.Task<bool> ExecuteActionByTypeAsync(EscalationAction action, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, object>>(action.ActionConfig) ?? new Dictionary<string, object>();

        switch (action.ActionType.ToLower())
        {
            case "notify":
                return await ExecuteNotifyActionAsync(action, config, cancellationToken);
            case "reassign":
                return await ExecuteReassignActionAsync(action, config, cancellationToken);
            case "autotransition":
                return await ExecuteAutoTransitionActionAsync(action, config, cancellationToken);
            case "webhook":
                return await ExecuteWebhookActionAsync(action, config, cancellationToken);
            default:
                _logger.LogWarning("Unknown escalation action type: {ActionType}", action.ActionType);
                return false;
        }
    }

    private async System.Threading.Tasks.Task<bool> ExecuteNotifyActionAsync(EscalationAction action, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        var recipients = config.GetValueOrDefault("recipients", "").ToString()?.Split(',') ?? Array.Empty<string>();
        var message = config.GetValueOrDefault("message", "SLA violation escalation").ToString() ?? "SLA violation escalation";

        var validRecipientIds = recipients
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => Guid.TryParse(r.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        foreach (var recipientId in validRecipientIds)
        {
            await _notificationService.CreateNotificationAsync(recipientId, new CreateNotificationRequest
            {
                UserId = recipientId,
                Type = "SlaEscalation",
                Title = "SLA Violation Escalation",
                Message = message,
                Priority = "High",
                ActionData = JsonSerializer.Serialize(new { ViolationId = action.SlaViolationId, ActionId = action.Id })
            });
        }

        action.Result = $"Notified {validRecipientIds.Count} recipients";
        return true;
    }

    private async System.Threading.Tasks.Task<bool> ExecuteReassignActionAsync(EscalationAction action, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        var newAssigneeId = config.GetValueOrDefault("assigneeId", "").ToString();
        var backupRole = config.GetValueOrDefault("backupRole", "").ToString();

        if (string.IsNullOrEmpty(newAssigneeId) && string.IsNullOrEmpty(backupRole))
        {
            action.ErrorMessage = "No assignee or backup role specified";
            return false;
        }

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == action.SlaViolation.TaskId, cancellationToken);
        if (task == null)
        {
            action.ErrorMessage = "Task not found";
            return false;
        }

        if (!string.IsNullOrEmpty(newAssigneeId) && Guid.TryParse(newAssigneeId, out var assigneeGuid))
        {
            task.AssignedToId = assigneeGuid;
        }
        else if (!string.IsNullOrEmpty(backupRole))
        {
            var tenantId = action.SlaViolation.TenantId;
            var backupUser = await _context.Users
                .Where(u => u.TenantId == tenantId && u.UserRoles.Any(ur => ur.Role.Name == backupRole))
                .FirstOrDefaultAsync(cancellationToken);

            if (backupUser == null)
            {
                action.ErrorMessage = $"No user with role '{backupRole}' in tenant {tenantId}";
                return false;
            }

            task.AssignedToId = backupUser.Id;
        }

        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = null;
        await _context.SaveChangesAsync(cancellationToken);

        action.Result = $"Reassigned task to {task.AssignedToId}";
        return true;
    }

    private async System.Threading.Tasks.Task<bool> ExecuteAutoTransitionActionAsync(EscalationAction action, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        var newStatus = config.GetValueOrDefault("status", "").ToString();
        if (string.IsNullOrEmpty(newStatus))
        {
            action.ErrorMessage = "No target status specified";
            return false;
        }

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == action.SlaViolation.TaskId, cancellationToken);
        if (task == null)
        {
            action.ErrorMessage = "Task not found";
            return false;
        }

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = null;
        await _context.SaveChangesAsync(cancellationToken);

        action.Result = $"Transitioned task to {newStatus}";
        return true;
    }

    private async System.Threading.Tasks.Task<bool> ExecuteWebhookActionAsync(EscalationAction action, Dictionary<string, object> config, CancellationToken cancellationToken)
    {
        var webhookUrl = config.GetValueOrDefault("url", "").ToString();
        if (string.IsNullOrEmpty(webhookUrl))
        {
            action.ErrorMessage = "No webhook URL specified";
            return false;
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient("SlaEscalations");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var payload = new
            {
                ViolationId = action.SlaViolationId,
                ActionId = action.Id,
                ActionType = action.ActionType,
                Timestamp = DateTime.UtcNow,
                Config = config
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, content, cancellationToken);
            
            action.Result = $"Webhook called: {response.StatusCode}";
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            action.ErrorMessage = $"Webhook failed: {ex.Message}";
            return false;
        }
    }
}
