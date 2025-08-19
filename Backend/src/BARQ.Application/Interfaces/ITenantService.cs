using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ITenantService
    {
        Task<PagedResult<TenantDto>> GetTenantsAsync(ListRequest request);
        Task<TenantDto?> GetTenantByIdAsync(Guid id);
        Task<TenantDto?> GetTenantByNameAsync(string name);
        Task<TenantDto> CreateTenantAsync(CreateTenantRequest request);
        Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantRequest request);
        Task<bool> DeleteTenantAsync(Guid id);
        Task<bool> ActivateTenantAsync(Guid id);
        Task<bool> DeactivateTenantAsync(Guid id);
    }
}
