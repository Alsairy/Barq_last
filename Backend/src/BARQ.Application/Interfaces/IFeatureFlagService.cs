using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IFeatureFlagService
    {
        Task<PagedResult<FeatureFlagDto>> GetFeatureFlagsAsync(ListRequest request);
        Task<FeatureFlagDto?> GetFeatureFlagByIdAsync(Guid id);
        Task<FeatureFlagDto?> GetFeatureFlagByNameAsync(string name);
        Task<FeatureFlagDto> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, string createdBy);
        Task<FeatureFlagDto?> UpdateFeatureFlagAsync(Guid id, UpdateFeatureFlagRequest request, string updatedBy);
        Task<bool> DeleteFeatureFlagAsync(Guid id, string deletedBy);
        Task<bool> ToggleFeatureFlagAsync(Guid id, bool isEnabled, string changedBy, string? reason = null);
        Task<bool> IsFeatureEnabledAsync(string featureName, string? environment = null);
        Task<Dictionary<string, bool>> GetFeatureFlagsForEnvironmentAsync(string environment);
        Task<List<FeatureFlagDto>> GetFeatureFlagsByCategoryAsync(string category);
        Task RefreshFeatureFlagCacheAsync();
    }
}
