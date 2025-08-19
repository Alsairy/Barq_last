using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ILanguageService
    {
        Task<PagedResult<LanguageDto>> GetLanguagesAsync(ListRequest request);
        Task<LanguageDto?> GetLanguageByIdAsync(Guid id);
        Task<LanguageDto?> GetLanguageByCodeAsync(string code);
        Task<List<LanguageDto>> GetEnabledLanguagesAsync();
        Task<LanguageDto?> GetDefaultLanguageAsync();
        Task<LanguageDto> CreateLanguageAsync(CreateLanguageRequest request, string createdBy);
        Task<LanguageDto?> UpdateLanguageAsync(Guid id, UpdateLanguageRequest request, string updatedBy);
        Task<bool> DeleteLanguageAsync(Guid id, string deletedBy);
        Task<bool> SetDefaultLanguageAsync(Guid id, string updatedBy);
        Task<bool> ToggleLanguageAsync(Guid id, bool isEnabled, string updatedBy);
        Task<Dictionary<string, object>> GetLanguageStatsAsync();
        Task RefreshLanguageCompletionAsync(string languageCode);
        Task<List<LanguageDto>> GetLanguagesByDirectionAsync(string direction);
        Task<bool> ValidateLanguageCodeAsync(string code);
    }
}
