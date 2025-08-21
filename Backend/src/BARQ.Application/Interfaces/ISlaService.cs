namespace BARQ.Application.Interfaces
{
    public interface ISlaService
    {
        Task<bool> CheckSlaViolationAsync(Guid taskId);
        Task<TimeSpan> GetRemainingTimeAsync(Guid taskId);
        Task CreateSlaViolationAsync(Guid taskId, string reason);
    }
}
