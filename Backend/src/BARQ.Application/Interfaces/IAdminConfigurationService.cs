using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;

namespace BARQ.Application.Interfaces
{
    public interface IAdminConfigurationService
    {
        Task<PagedResult<AdminConfigurationDto>> GetConfigurationsAsync(Guid tenantId, AdminConfigurationListRequest request);
        Task<AdminConfigurationDto?> GetConfigurationByIdAsync(Guid id);
        Task<AdminConfigurationDto?> GetConfigurationByKeyAsync(Guid tenantId, string key);
        Task<AdminConfigurationDto> CreateConfigurationAsync(Guid tenantId, CreateAdminConfigurationRequest request);
        Task<AdminConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateAdminConfigurationRequest request);
        Task<bool> DeleteConfigurationAsync(Guid id);
        Task<bool> ValidateConfigurationAsync(Guid id, Guid validatedBy);
        Task<List<AdminConfigurationDto>> GetConfigurationsByCategoryAsync(Guid tenantId, string category);
        Task<List<AdminConfigurationDto>> GetConfigurationsByTypeAsync(Guid tenantId, AdminConfigurationType type);
        Task<bool> TestConfigurationAsync(Guid id);
        Task<string?> GetConfigurationValueAsync(Guid tenantId, string key);
        Task<T?> GetConfigurationValueAsync<T>(Guid tenantId, string key);
    }
}
