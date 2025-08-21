using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BARQ.Application.Interfaces;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;

namespace BARQ.Application.Services.Workflow;

public class SlaService : ISlaService
{
    private readonly BarqDbContext _context;
    private readonly ILogger<SlaService> _logger;

    public SlaService(BarqDbContext context, ILogger<SlaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TimeSpan> CalculateRemainingTimeAsync(Guid taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return TimeSpan.Zero;

        var slaPolicy = await _context.SlaPolicies
            .FirstOrDefaultAsync(p => p.Priority == task.Priority && p.IsActive);

        if (slaPolicy == null)
            return TimeSpan.Zero;

        var elapsed = DateTime.UtcNow - task.CreatedAt;
        var slaTime = TimeSpan.FromHours(slaPolicy.ResponseTimeHours);
        
        return slaTime - elapsed;
    }

    public async Task<bool> CheckSlaViolationAsync(Guid taskId)
    {
        var remainingTime = await CalculateRemainingTimeAsync(taskId);
        return remainingTime < TimeSpan.Zero;
    }

    public async Task<int> GetSlaHoursAsync(Guid taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return 0;

        var slaPolicy = await _context.SlaPolicies
            .FirstOrDefaultAsync(p => p.Priority == task.Priority && p.IsActive);

        return slaPolicy?.ResponseTimeHours ?? 0;
    }

    public async Task<SlaViolation> CreateSlaViolationAsync(Guid taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            throw new ArgumentException("Task not found", nameof(taskId));

        var violation = new SlaViolation
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ViolationType = "ResponseTime",
            ViolationTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SlaViolations.Add(violation);
        await _context.SaveChangesAsync();

        return violation;
    }

    public async Task<List<SlaViolation>> CheckViolationsAsync()
    {
        var violations = new List<SlaViolation>();
        
        var openTasks = await _context.Tasks
            .Where(t => t.Status == "InProgress" || t.Status == "Open")
            .ToListAsync();

        foreach (var task in openTasks)
        {
            var isViolation = await CheckSlaViolationAsync(task.Id);
            if (isViolation)
            {
                var existingViolation = await _context.SlaViolations
                    .FirstOrDefaultAsync(v => v.TaskId == task.Id);

                if (existingViolation == null)
                {
                    var violation = await CreateSlaViolationAsync(task.Id);
                    violations.Add(violation);
                }
            }
        }

        return violations;
    }
}
