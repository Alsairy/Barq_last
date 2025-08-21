using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;

namespace BARQ.Application.Interfaces;

public interface IEscalationService
{
    System.Threading.Tasks.Task<PagedResult<EscalationRule>> GetEscalationRulesAsync(Guid? slaPolicyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<EscalationRule?> GetEscalationRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<EscalationRule> CreateEscalationRuleAsync(EscalationRule rule, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<EscalationRule> UpdateEscalationRuleAsync(EscalationRule rule, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task DeleteEscalationRuleAsync(Guid id, CancellationToken cancellationToken = default);
    
    System.Threading.Tasks.Task<PagedResult<EscalationAction>> GetEscalationActionsAsync(Guid? violationId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<EscalationAction> CreateEscalationActionAsync(EscalationAction action, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<EscalationAction> UpdateEscalationActionAsync(EscalationAction action, CancellationToken cancellationToken = default);
    
    System.Threading.Tasks.Task ProcessEscalationsAsync(CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task ExecuteEscalationActionAsync(Guid actionId, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<bool> ShouldEscalateAsync(Guid violationId, CancellationToken cancellationToken = default);
}
