using BARQ.Application.Interfaces;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BARQ.Application.Services.Workflow;

public sealed class SlaService : ISlaService
{
    private readonly BarqDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SlaService(BarqDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }
    public async Task<PagedResult<SlaPolicy>> GetSlaPoliciesAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && 
                       (string.IsNullOrEmpty(search) || p.Name.Contains(search) || p.Description.Contains(search)));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SlaPolicy>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SlaPolicy?> GetSlaPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SlaPolicy> CreateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        slaPolicy.TenantId = _tenantProvider.GetTenantId();
        slaPolicy.CreatedAt = DateTime.UtcNow;
        slaPolicy.UpdatedAt = DateTime.UtcNow;
        
        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync(cancellationToken);
        
        return slaPolicy;
    }

    public async Task<SlaPolicy> UpdateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.Id == slaPolicy.Id)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (existing == null)
            throw new InvalidOperationException($"SLA Policy {slaPolicy.Id} not found");
            
        existing.Name = slaPolicy.Name;
        existing.Description = slaPolicy.Description;
        existing.ResponseTimeHours = slaPolicy.ResponseTimeHours;
        existing.ResolutionTimeHours = slaPolicy.ResolutionTimeHours;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async System.Threading.Tasks.Task DeleteSlaPolicyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (policy != null)
        {
            policy.IsDeleted = true;
            policy.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PagedResult<SlaViolation>> GetSlaViolationsAsync(int page = 1, int pageSize = 10, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SlaViolations
            .Where(v => v.TenantId == _tenantProvider.GetTenantId() && 
                       (string.IsNullOrEmpty(status) || v.Status == status));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SlaViolation>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SlaViolation?> GetSlaViolationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SlaViolations
            .Where(v => v.TenantId == _tenantProvider.GetTenantId() && v.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SlaViolation> CreateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default)
    {
        violation.TenantId = _tenantProvider.GetTenantId();
        violation.CreatedAt = DateTime.UtcNow;
        violation.UpdatedAt = DateTime.UtcNow;
        
        _context.SlaViolations.Add(violation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return violation;
    }

    public async Task<SlaViolation> UpdateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SlaViolations
            .Where(v => v.TenantId == _tenantProvider.GetTenantId() && v.Id == violation.Id)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (existing == null)
            throw new InvalidOperationException($"SLA Violation {violation.Id} not found");
            
        existing.Status = violation.Status;
        existing.Resolution = violation.Resolution;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<DateTime> CalculateDueDateAsync(Guid slaPolicyId, DateTime startTime, CancellationToken cancellationToken = default)
    {
        var policy = await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.Id == slaPolicyId)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (policy == null)
            return startTime.AddDays(1);
            
        return startTime.AddHours(policy.ResponseTimeHours);
    }

    public async Task<bool> IsViolationAsync(Guid taskId, Guid slaPolicyId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Set<BARQ.Core.Entities.Task>()
            .Where(t => t.TenantId == _tenantProvider.GetTenantId() && t.Id == taskId)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (task == null) return false;
        
        var dueDate = await CalculateDueDateAsync(slaPolicyId, task.CreatedAt, cancellationToken);
        return DateTime.UtcNow > dueDate && task.Status != "Completed";
    }

    public async System.Threading.Tasks.Task CheckAndCreateViolationsAsync(CancellationToken cancellationToken = default)
    {
        var activeTasks = await _context.Set<BARQ.Core.Entities.Task>()
            .Where(t => t.TenantId == _tenantProvider.GetTenantId() && t.Status != "Completed")
            .ToListAsync(cancellationToken);
            
        var defaultSlaPolicy = await _context.SlaPolicies
            .Where(p => p.TenantId == _tenantProvider.GetTenantId() && p.Name == "Default")
            .FirstOrDefaultAsync(cancellationToken);
            
        if (defaultSlaPolicy == null) return;
            
        foreach (var task in activeTasks)
        {
            var isViolation = await IsViolationAsync(task.Id, defaultSlaPolicy.Id, cancellationToken);
            if (isViolation)
            {
                var existingViolation = await _context.SlaViolations
                    .Where(v => v.TenantId == _tenantProvider.GetTenantId() && v.TaskId == task.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                    
                if (existingViolation == null)
                {
                    var violation = new SlaViolation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantProvider.GetTenantId(),
                        TaskId = task.Id,
                        SlaPolicyId = defaultSlaPolicy.Id,
                        ViolationTime = DateTime.UtcNow,
                        Status = "Open",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    await CreateSlaViolationAsync(violation, cancellationToken);
                }
            }
        }
    }
}
