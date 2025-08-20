using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ISystemHealthService
    {
        Task<PagedResult<SystemHealthDto>> GetSystemHealthAsync(ListRequest request);
        Task<SystemHealthDto?> GetSystemHealthByIdAsync(Guid id);
        Task<SystemHealthDto?> GetSystemHealthByComponentAsync(string component);
        Task<List<SystemHealthDto>> GetSystemHealthByStatusAsync(string status);
        Task<SystemHealthDto> UpdateSystemHealthAsync(string component, string status, string? statusMessage, long responseTimeMs, Dictionary<string, object>? details = null);
        Task<OpsDashboardDto> GetOpsDashboardAsync();
        Task RefreshAllHealthChecksAsync();
        Task<bool> IsSystemHealthyAsync();
        Task<Dictionary<string, object>> GetSystemMetricsAsync();
        Task CleanupOldHealthRecordsAsync(int daysToKeep = 30);
    }
}
