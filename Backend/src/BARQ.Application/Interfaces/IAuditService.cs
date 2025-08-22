namespace BARQ.Application.Interfaces
{
    public interface IAuditService
    {
        System.Threading.Tasks.Task LogAsync(string entityType, string action, object data);
        System.Threading.Tasks.Task LogAsync(string entityType, string action, object data, Guid? userId = null);
        System.Threading.Tasks.Task<IEnumerable<object>> GetAuditLogsAsync(string? entityType = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
