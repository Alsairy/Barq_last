using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services.Workflow
{
    public class SlaService : ISlaService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<SlaService> _logger;

        public SlaService(BarqDbContext context, ILogger<SlaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CheckSlaViolationAsync(Guid taskId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null) return false;

                var slaPolicy = await _context.SlaPolicy
                    .FirstOrDefaultAsync(sp => sp.TaskType == task.TaskType);

                if (slaPolicy == null) return false;

                var timeElapsed = DateTime.UtcNow - task.CreatedAt;
                return timeElapsed.TotalHours > slaPolicy.MaxHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SLA violation for task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<TimeSpan> GetRemainingTimeAsync(Guid taskId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null) return TimeSpan.Zero;

                var slaPolicy = await _context.SlaPolicy
                    .FirstOrDefaultAsync(sp => sp.TaskType == task.TaskType);

                if (slaPolicy == null) return TimeSpan.Zero;

                var timeElapsed = DateTime.UtcNow - task.CreatedAt;
                var remainingTime = TimeSpan.FromHours(slaPolicy.MaxHours) - timeElapsed;

                return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting remaining time for task {TaskId}", taskId);
                return TimeSpan.Zero;
            }
        }

        public async System.Threading.Tasks.Task CreateSlaViolationAsync(Guid taskId, string reason)
        {
            try
            {
                var violation = new SlaViolation
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    ViolationTime = DateTime.UtcNow,
                    Reason = reason,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow
                };

                _context.SlaViolations.Add(violation);
                await _context.SaveChangesAsync();

                _logger.LogWarning("SLA violation created for task {TaskId}: {Reason}", taskId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SLA violation for task {TaskId}", taskId);
                throw;
            }
        }
    }
}
