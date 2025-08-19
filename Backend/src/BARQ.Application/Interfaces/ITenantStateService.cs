using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ITenantStateService
    {
        Task<PagedResult<TenantStateDto>> GetTenantStatesAsync(ListRequest request);
        Task<TenantStateDto?> GetTenantStateByIdAsync(Guid id);
        Task<TenantStateDto?> GetTenantStateByTenantIdAsync(Guid tenantId);
        Task<TenantStateDto?> UpdateTenantStateAsync(Guid tenantId, UpdateTenantStateRequest request, string updatedBy);
        Task RefreshTenantStateAsync(Guid tenantId);
        Task RefreshAllTenantStatesAsync();
        Task<List<TenantStateDto>> GetTenantsRequiringAttentionAsync();
        Task<List<TenantStateDto>> GetUnhealthyTenantsAsync();
        Task<Dictionary<string, object>> GetTenantStatsSummaryAsync();
        Task UpdateTenantUsageStatsAsync(Guid tenantId);
        Task MarkTenantForAttentionAsync(Guid tenantId, string reason, string markedBy);
        Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy);
    }
}
