using BARQ.Core.Entities;

namespace BARQ.Application.Interfaces;

public interface ISlaService
{
    Task<TimeSpan> CalculateRemainingTimeAsync(Guid taskId);
    Task<bool> CheckSlaViolationAsync(Guid taskId);
    Task<int> GetSlaHoursAsync(Guid taskId);
    Task<SlaViolation> CreateSlaViolationAsync(Guid taskId);
    Task<List<SlaViolation>> CheckViolationsAsync();
}
