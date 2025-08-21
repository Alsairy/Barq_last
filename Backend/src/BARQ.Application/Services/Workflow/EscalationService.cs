using BARQ.Application.Interfaces;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services.Workflow
{
    public class EscalationService : IEscalationService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<EscalationService> _logger;

        public EscalationService(BarqDbContext context, ILogger<EscalationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task ExecuteEscalationAsync(Guid taskId, string escalationType)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null) return;

                var escalationRules = await _context.EscalationRules
                    .Where(er => er.TaskType == task.TaskType && er.EscalationType == escalationType)
                    .OrderBy(er => er.Priority)
                    .ToListAsync();

                foreach (var rule in escalationRules)
                {
                    await ExecuteEscalationRuleAsync(task, rule);
                }

                _logger.LogInformation("Escalation executed for task {TaskId} with type {EscalationType}", taskId, escalationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing escalation for task {TaskId}", taskId);
                throw;
            }
        }

        private async System.Threading.Tasks.Task ExecuteEscalationRuleAsync(Core.Entities.Task task, EscalationRule rule)
        {
            try
            {
                switch (rule.ActionType)
                {
                    case "Notify":
                        await NotifyStakeholdersAsync(task, rule);
                        break;
                    case "Reassign":
                        await ReassignTaskAsync(task, rule);
                        break;
                    case "Escalate":
                        await EscalateToManagerAsync(task, rule);
                        break;
                    default:
                        _logger.LogWarning("Unknown escalation action type: {ActionType}", rule.ActionType);
                        break;
                }

                var action = new EscalationAction
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    RuleId = rule.Id,
                    ActionType = rule.ActionType,
                    ExecutedAt = DateTime.UtcNow,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.EscalationActions.Add(action);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing escalation rule {RuleId} for task {TaskId}", rule.Id, task.Id);
                throw;
            }
        }

        private async System.Threading.Tasks.Task NotifyStakeholdersAsync(Core.Entities.Task task, EscalationRule rule)
        {
            _logger.LogInformation("Notifying stakeholders for task {TaskId} escalation", task.Id);
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task ReassignTaskAsync(Core.Entities.Task task, EscalationRule rule)
        {
            _logger.LogInformation("Reassigning task {TaskId} due to escalation", task.Id);
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task EscalateToManagerAsync(Core.Entities.Task task, EscalationRule rule)
        {
            _logger.LogInformation("Escalating task {TaskId} to manager", task.Id);
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
