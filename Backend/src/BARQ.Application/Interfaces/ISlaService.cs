using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;

namespace BARQ.Application.Interfaces;

public interface ISlaService
{
    System.Threading.Tasks.Task<PagedResult<SlaPolicy>> GetSlaPoliciesAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaPolicy?> GetSlaPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaPolicy> CreateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaPolicy> UpdateSlaPolicyAsync(SlaPolicy slaPolicy, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task DeleteSlaPolicyAsync(Guid id, CancellationToken cancellationToken = default);
    
    System.Threading.Tasks.Task<PagedResult<SlaViolation>> GetSlaViolationsAsync(int page = 1, int pageSize = 10, string? status = null, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaViolation?> GetSlaViolationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaViolation> CreateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<SlaViolation> UpdateSlaViolationAsync(SlaViolation violation, CancellationToken cancellationToken = default);
    
    System.Threading.Tasks.Task<DateTime> CalculateDueDateAsync(Guid slaPolicyId, DateTime startTime, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<bool> IsViolationAsync(Guid taskId, Guid slaPolicyId, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task CheckAndCreateViolationsAsync(CancellationToken cancellationToken = default);
}
