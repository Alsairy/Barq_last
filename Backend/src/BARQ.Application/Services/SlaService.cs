using BARQ.Application.Interfaces;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services;

public class SlaService : ISlaService
{
    private readonly BarqDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SlaService> _logger;

    public SlaService(BarqDbContext context, ITenantProvider tenantProvider, ILogger<SlaService> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<PagedResult<SlaPolicy>> GetSlaPoliciesAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SlaPolicies
            .Where(s => s.TenantId == _tenantProvider.GetTenantId() && 
                       (string.IsNullOrEmpty(search) || s.Name.Contains(search) || s.Description.Contains(search)))
            .Include(s => s.BusinessCalendar);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SlaPolicy>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async System.Threading.Tasks.Task<SlaPolicy?> GetSlaPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SlaPolicies
            .Include(s => s.BusinessCalendar)
            .Include(s => s.EscalationRules)
            .FirstOrDefaultAsync(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<SlaPolicy> CreateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        slaPolicy.TenantId = _tenantProvider.GetTenantId();
        slaPolicy.CreatedAt = DateTime.UtcNow;
        slaPolicy.CreatedBy = null;

        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created SLA policy {PolicyId} for tenant {TenantId}", slaPolicy.Id, slaPolicy.TenantId);
        return slaPolicy;
    }

    public async System.Threading.Tasks.Task<SlaPolicy> UpdateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SlaPolicies
            .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == slaPolicy.Id)
            .FirstOrDefaultAsync();
        if (existing == null)
            throw new InvalidOperationException($"SLA policy {slaPolicy.Id} not found");

        existing.Name = slaPolicy.Name;
        existing.Description = slaPolicy.Description;
        existing.TaskType = slaPolicy.TaskType;
        existing.Priority = slaPolicy.Priority;
        existing.ResponseTimeHours = slaPolicy.ResponseTimeHours;
        existing.ResolutionTimeHours = slaPolicy.ResolutionTimeHours;
        existing.BusinessCalendarId = slaPolicy.BusinessCalendarId;
        existing.IsActive = slaPolicy.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated SLA policy {PolicyId}", slaPolicy.Id);
        return existing;
    }

    public async System.Threading.Tasks.Task DeleteSlaPolicyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slaPolicy = await _context.SlaPolicies
            .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == id)
            .FirstOrDefaultAsync();
        if (slaPolicy != null)
        {
            slaPolicy.IsDeleted = true;
            slaPolicy.UpdatedAt = DateTime.UtcNow;
            slaPolicy.UpdatedBy = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted SLA policy {PolicyId}", id);
        }
    }

    public async System.Threading.Tasks.Task<PagedResult<SlaViolation>> GetSlaViolationsAsync(int page = 1, int pageSize = 10, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SlaViolations
            .Where(v => v.TenantId == _tenantProvider.GetTenantId() && 
                       (string.IsNullOrEmpty(status) || v.Status == status))
            .Include(v => v.SlaPolicy)
            .Include(v => v.Task);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.ViolationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SlaViolation>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async System.Threading.Tasks.Task<SlaViolation?> GetSlaViolationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SlaViolations
            .Include(v => v.SlaPolicy)
            .Include(v => v.Task)
            .Include(v => v.EscalationActions)
            .FirstOrDefaultAsync(v => v.TenantId == _tenantProvider.GetTenantId() && v.Id == id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<SlaViolation> CreateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default)
    {
        violation.TenantId = _tenantProvider.GetTenantId();
        violation.CreatedAt = DateTime.UtcNow;
        violation.CreatedBy = null;

        _context.SlaViolations.Add(violation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Created SLA violation {ViolationId} for task {TaskId}", violation.Id, violation.TaskId);
        return violation;
    }

    public async System.Threading.Tasks.Task<SlaViolation> UpdateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SlaViolations
            .Where(v => v.TenantId == _tenantProvider.GetTenantId() && v.Id == violation.Id)
            .FirstOrDefaultAsync();
        if (existing == null)
            throw new InvalidOperationException($"SLA violation {violation.Id} not found");

        existing.Status = violation.Status;
        existing.Resolution = violation.Resolution;
        existing.ResolvedTime = violation.ResolvedTime;
        existing.EscalationLevel = violation.EscalationLevel;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated SLA violation {ViolationId}", violation.Id);
        return existing;
    }

    public async System.Threading.Tasks.Task<DateTime> CalculateDueDateAsync(Guid slaPolicyId, DateTime startTime, CancellationToken cancellationToken = default)
    {
        var policy = await _context.SlaPolicies
            .Include(p => p.BusinessCalendar)
            .ThenInclude(c => c!.Holidays)
            .FirstOrDefaultAsync(p => p.TenantId == _tenantProvider.GetTenantId() && p.Id == slaPolicyId, cancellationToken);

        if (policy == null)
            throw new InvalidOperationException($"SLA policy {slaPolicyId} not found");

        var calendar = policy.BusinessCalendar;
        if (calendar == null)
        {
            return startTime.AddHours(policy.ResponseTimeHours);
        }

        var workDays = calendar.WorkDays.Split(',').Select(int.Parse).ToList();
        var holidays = calendar.Holidays.Where(h => h.Date >= startTime.Date).Select(h => h.Date.Date).ToHashSet();

        var currentTime = startTime;
        var remainingHours = policy.ResponseTimeHours;

        while (remainingHours > 0)
        {
            if (workDays.Contains((int)currentTime.DayOfWeek) && !holidays.Contains(currentTime.Date))
            {
                var workStart = currentTime.Date.Add(calendar.WorkDayStart);
                var workEnd = currentTime.Date.Add(calendar.WorkDayEnd);

                if (currentTime < workStart)
                    currentTime = workStart;

                if (currentTime >= workEnd)
                {
                    currentTime = currentTime.Date.AddDays(1).Add(calendar.WorkDayStart);
                    continue;
                }

                var availableHours = (workEnd - currentTime).TotalHours;
                if (remainingHours <= availableHours)
                {
                    return currentTime.AddHours(remainingHours);
                }

                remainingHours -= (int)availableHours;
                currentTime = currentTime.Date.AddDays(1).Add(calendar.WorkDayStart);
            }
            else
            {
                currentTime = currentTime.Date.AddDays(1).Add(calendar.WorkDayStart);
            }
        }

        return currentTime;
    }

    public async System.Threading.Tasks.Task<bool> IsViolationAsync(Guid taskId, Guid slaPolicyId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Where(t => t.TenantId == _tenantProvider.GetTenantId() && t.Id == taskId)
            .FirstOrDefaultAsync();
        var policy = await _context.SlaPolicies
            .Where(s => s.TenantId == _tenantProvider.GetTenantId() && s.Id == slaPolicyId)
            .FirstOrDefaultAsync();

        if (task == null || policy == null)
            return false;

        var dueDate = await CalculateDueDateAsync(slaPolicyId, task.CreatedAt, cancellationToken);
        return DateTime.UtcNow > dueDate && task.Status != "Completed";
    }

    public async System.Threading.Tasks.Task CheckAndCreateViolationsAsync(CancellationToken cancellationToken = default)
    {
        var activePolicies = await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var policy in activePolicies)
        {
            var tasks = await _context.Tasks
                .Where(t => t.Status != "Completed" && 
                           !_context.SlaViolations.Any(v => v.TaskId == t.Id && v.SlaPolicyId == policy.Id))
                .ToListAsync(cancellationToken);

            foreach (var task in tasks)
            {
                if (await IsViolationAsync(task.Id, policy.Id, cancellationToken))
                {
                    var violation = new SlaViolation
                    {
                        Id = Guid.NewGuid(),
                        SlaPolicyId = policy.Id,
                        TaskId = task.Id,
                        ViolationType = "Response",
                        ViolationTime = DateTime.UtcNow,
                        Status = "Open",
                        TenantId = policy.TenantId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = null
                    };

                    await CreateSlaViolationAsync(violation, cancellationToken);
                }
            }
        }
    }
}
