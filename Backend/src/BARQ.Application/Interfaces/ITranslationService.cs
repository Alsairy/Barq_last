using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface ITranslationService
    {
        Task<PagedResult<TranslationDto>> GetTranslationsAsync(ListRequest request);
        Task<TranslationDto?> GetTranslationByIdAsync(Guid id);
        Task<TranslationDto?> GetTranslationByKeyAsync(string languageCode, string key);
        Task<Dictionary<string, string>> GetTranslationsForLanguageAsync(string languageCode, string? category = null);
        Task<TranslationDto> CreateTranslationAsync(CreateTranslationRequest request, string createdBy);
        Task<TranslationDto?> UpdateTranslationAsync(Guid id, UpdateTranslationRequest request, string updatedBy);
        Task<bool> DeleteTranslationAsync(Guid id, string deletedBy);
        Task<List<TranslationDto>> BulkCreateTranslationsAsync(BulkTranslationRequest request, string createdBy);
        Task<bool> ApproveTranslationAsync(Guid id, string approvedBy);
        Task<bool> RejectTranslationAsync(Guid id, string rejectedBy, string reason);
        Task<List<TranslationDto>> GetTranslationsByCategoryAsync(string languageCode, string category);
        Task<List<TranslationDto>> GetPendingTranslationsAsync(string? languageCode = null);
        Task<TranslationStatsDto> GetTranslationStatsAsync(string languageCode);
        Task<List<TranslationStatsDto>> GetAllLanguageStatsAsync();
        Task<Dictionary<string, object>> ExportTranslationsAsync(string languageCode, string format = "json");
        Task<bool> ImportTranslationsAsync(string languageCode, Dictionary<string, object> translations, string importedBy);
        Task<List<string>> GetMissingTranslationKeysAsync(string languageCode, string? category = null);
        Task<bool> SyncTranslationKeysAsync(string sourceLanguageCode, string targetLanguageCode, string syncedBy);
    }
}
