namespace BARQ.Application.Interfaces
{
    public interface IEscalationService
    {
        Task ExecuteEscalationAsync(Guid taskId, string escalationType);
    }
}
