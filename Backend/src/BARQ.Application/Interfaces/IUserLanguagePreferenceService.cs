using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IUserLanguagePreferenceService
    {
        Task<PagedResult<UserLanguagePreferenceDto>> GetUserLanguagePreferencesAsync(Guid userId, ListRequest request);
        Task<UserLanguagePreferenceDto?> GetUserLanguagePreferenceByIdAsync(Guid id);
        Task<UserLanguagePreferenceDto?> GetUserDefaultLanguagePreferenceAsync(Guid userId);
        Task<UserLanguagePreferenceDto?> GetUserLanguagePreferenceByCodeAsync(Guid userId, string languageCode);
        Task<UserLanguagePreferenceDto> CreateUserLanguagePreferenceAsync(Guid userId, string languageId, string createdBy);
        Task<UserLanguagePreferenceDto?> UpdateUserLanguagePreferenceAsync(Guid id, UpdateUserLanguagePreferenceRequest request, string updatedBy);
        Task<bool> DeleteUserLanguagePreferenceAsync(Guid id, string deletedBy);
        Task<bool> SetDefaultLanguagePreferenceAsync(Guid userId, Guid preferenceId, string updatedBy);
        Task<Dictionary<string, object>> GetUserAccessibilitySettingsAsync(Guid userId);
        Task<bool> UpdateUserAccessibilitySettingsAsync(Guid userId, UpdateUserLanguagePreferenceRequest request, string updatedBy);
        Task<List<UserLanguagePreferenceDto>> GetUsersWithAccessibilityNeedsAsync();
        Task<Dictionary<string, object>> GetLanguageUsageStatsAsync();
    }
}
