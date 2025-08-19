using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace BARQ.Application.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<TranslationService> _logger;
        private readonly IMemoryCache _cache;
        private const string TRANSLATION_CACHE_KEY = "translations_{0}_{1}"; // languageCode_category

        public TranslationService(BarqDbContext context, ILogger<TranslationService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<PagedResult<TranslationDto>> GetTranslationsAsync(ListRequest request)
        {
            try
            {
                var query = _context.Translations.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(t => t.Key.Contains(request.SearchTerm) ||
                                           t.Value.Contains(request.SearchTerm) ||
                                           (t.Category != null && t.Category.Contains(request.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(t => EF.Property<object>(t, request.SortBy))
                        : query.OrderBy(t => EF.Property<object>(t, request.SortBy));
                }
                else
                {
                    query = query.OrderBy(t => t.LanguageCode).ThenBy(t => t.Category).ThenBy(t => t.Key);
                }

                var totalCount = await query.CountAsync();
                var translations = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var translationDtos = translations.Select(MapToDto).ToList();

                return new PagedResult<TranslationDto>
                {
                    Items = translationDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations");
                throw;
            }
        }

        public async Task<TranslationDto?> GetTranslationByIdAsync(Guid id)
        {
            try
            {
                var translation = await _context.Translations.FindAsync(id);
                return translation != null ? MapToDto(translation) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation by ID: {Id}", id);
                throw;
            }
        }

        public async Task<TranslationDto?> GetTranslationByKeyAsync(string languageCode, string key)
        {
            try
            {
                var translation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == languageCode && t.Key == key && t.IsActive);

                return translation != null ? MapToDto(translation) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation by key: {LanguageCode}/{Key}", languageCode, key);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetTranslationsForLanguageAsync(string languageCode, string? category = null)
        {
            try
            {
                var cacheKey = string.Format(TRANSLATION_CACHE_KEY, languageCode, category ?? "all");
                
                if (_cache.TryGetValue(cacheKey, out Dictionary<string, string>? cachedTranslations))
                {
                    return cachedTranslations!;
                }

                var query = _context.Translations
                    .Where(t => t.LanguageCode == languageCode && t.IsActive && t.IsApproved);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(t => t.Category == category);
                }

                var translations = await query
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.Key)
                    .ToDictionaryAsync(t => t.Key, t => t.Value);

                _cache.Set(cacheKey, translations, TimeSpan.FromMinutes(30));

                return translations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations for language: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<TranslationDto> CreateTranslationAsync(CreateTranslationRequest request, string createdBy)
        {
            try
            {
                var existingTranslation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Key == request.Key);

                if (existingTranslation != null)
                {
                    throw new InvalidOperationException($"Translation already exists for key '{request.Key}' in language '{request.LanguageCode}'");
                }

                var translation = new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = request.LanguageCode,
                    Key = request.Key,
                    Value = request.Value,
                    Category = request.Category,
                    Context = request.Context,
                    IsPlural = request.IsPlural,
                    PluralValue = request.PluralValue,
                    Notes = request.Notes,
                    IsActive = request.IsActive,
                    Region = request.Region,
                    Priority = request.Priority,
                    TranslatedBy = createdBy,
                    TranslatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };

                _context.Translations.Add(translation);
                await _context.SaveChangesAsync();

                InvalidateTranslationCache(request.LanguageCode, request.Category);

                _logger.LogInformation("Translation created: {Key} for {LanguageCode} by {CreatedBy}", 
                    request.Key, request.LanguageCode, createdBy);

                return MapToDto(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating translation");
                throw;
            }
        }

        public async Task<TranslationDto?> UpdateTranslationAsync(Guid id, UpdateTranslationRequest request, string updatedBy)
        {
            try
            {
                var translation = await _context.Translations.FindAsync(id);
                if (translation == null)
                {
                    return null;
                }

                var oldCategory = translation.Category;

                if (request.Value != null) translation.Value = request.Value;
                if (request.Category != null) translation.Category = request.Category;
                if (request.Context != null) translation.Context = request.Context;
                if (request.IsPlural.HasValue) translation.IsPlural = request.IsPlural.Value;
                if (request.PluralValue != null) translation.PluralValue = request.PluralValue;
                if (request.Notes != null) translation.Notes = request.Notes;
                if (request.IsActive.HasValue) translation.IsActive = request.IsActive.Value;
                if (request.Region != null) translation.Region = request.Region;
                if (request.Priority.HasValue) translation.Priority = request.Priority.Value;

                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = null;

                await _context.SaveChangesAsync();

                InvalidateTranslationCache(translation.LanguageCode, oldCategory);
                InvalidateTranslationCache(translation.LanguageCode, translation.Category);

                _logger.LogInformation("Translation updated: {Id} by {UpdatedBy}", id, updatedBy);

                return MapToDto(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTranslationAsync(Guid id, string deletedBy)
        {
            try
            {
                var translation = await _context.Translations.FindAsync(id);
                if (translation == null)
                {
                    return false;
                }

                translation.IsDeleted = true;
                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = null;

                await _context.SaveChangesAsync();

                InvalidateTranslationCache(translation.LanguageCode, translation.Category);

                _logger.LogInformation("Translation deleted: {Id} by {DeletedBy}", id, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation: {Id}", id);
                throw;
            }
        }

        public async Task<List<TranslationDto>> BulkCreateTranslationsAsync(BulkTranslationRequest request, string createdBy)
        {
            try
            {
                var translations = new List<Translation>();
                var createdTranslations = new List<TranslationDto>();

                foreach (var translationData in request.Translations)
                {
                    var existingTranslation = await _context.Translations
                        .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Key == translationData.Key);

                    if (existingTranslation == null)
                    {
                        var translation = new Translation
                        {
                            Id = Guid.NewGuid(),
                            LanguageCode = request.LanguageCode,
                            Key = translationData.Key,
                            Value = translationData.Value,
                            Category = translationData.Category,
                            Context = translationData.Context,
                            IsPlural = translationData.IsPlural,
                            PluralValue = translationData.PluralValue,
                            TranslatedBy = createdBy,
                            TranslatedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = null
                        };

                        translations.Add(translation);
                        createdTranslations.Add(MapToDto(translation));
                    }
                }

                if (translations.Any())
                {
                    _context.Translations.AddRange(translations);
                    await _context.SaveChangesAsync();

                    InvalidateTranslationCache(request.LanguageCode, null);

                    _logger.LogInformation("Bulk created {Count} translations for {LanguageCode} by {CreatedBy}", 
                        translations.Count, request.LanguageCode, createdBy);
                }

                return createdTranslations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating translations");
                throw;
            }
        }

        public async Task<bool> ApproveTranslationAsync(Guid id, string approvedBy)
        {
            try
            {
                var translation = await _context.Translations.FindAsync(id);
                if (translation == null)
                {
                    return false;
                }

                translation.IsApproved = true;
                translation.ApprovedBy = approvedBy;
                translation.ApprovedAt = DateTime.UtcNow;
                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = null;

                await _context.SaveChangesAsync();

                InvalidateTranslationCache(translation.LanguageCode, translation.Category);

                _logger.LogInformation("Translation approved: {Id} by {ApprovedBy}", id, approvedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving translation: {Id}", id);
                throw;
            }
        }

        public async Task<bool> RejectTranslationAsync(Guid id, string rejectedBy, string reason)
        {
            try
            {
                var translation = await _context.Translations.FindAsync(id);
                if (translation == null)
                {
                    return false;
                }

                translation.IsApproved = false;
                translation.Notes = $"Rejected by {rejectedBy}: {reason}";
                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Translation rejected: {Id} by {RejectedBy}", id, rejectedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting translation: {Id}", id);
                throw;
            }
        }

        public async Task<List<TranslationDto>> GetTranslationsByCategoryAsync(string languageCode, string category)
        {
            try
            {
                var translations = await _context.Translations
                    .Where(t => t.LanguageCode == languageCode && t.Category == category && t.IsActive)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.Key)
                    .ToListAsync();

                return translations.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations by category: {LanguageCode}/{Category}", languageCode, category);
                throw;
            }
        }

        public async Task<List<TranslationDto>> GetPendingTranslationsAsync(string? languageCode = null)
        {
            try
            {
                var query = _context.Translations.Where(t => !t.IsApproved && t.IsActive);

                if (!string.IsNullOrEmpty(languageCode))
                {
                    query = query.Where(t => t.LanguageCode == languageCode);
                }

                var translations = await query
                    .OrderBy(t => t.TranslatedAt)
                    .ToListAsync();

                return translations.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending translations");
                throw;
            }
        }

        public async Task<TranslationStatsDto> GetTranslationStatsAsync(string languageCode)
        {
            try
            {
                var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == languageCode);
                if (language == null)
                {
                    throw new InvalidOperationException($"Language not found: {languageCode}");
                }

                var totalKeys = await _context.Translations
                    .Where(t => t.LanguageCode == "en") // Assuming English is the source language
                    .CountAsync();

                var translatedKeys = await _context.Translations
                    .Where(t => t.LanguageCode == languageCode && t.IsActive)
                    .CountAsync();

                var approvedKeys = await _context.Translations
                    .Where(t => t.LanguageCode == languageCode && t.IsActive && t.IsApproved)
                    .CountAsync();

                var categoryStats = await _context.Translations
                    .Where(t => t.LanguageCode == languageCode && t.IsActive)
                    .GroupBy(t => t.Category)
                    .Select(g => new CategoryStats
                    {
                        Category = g.Key ?? "Uncategorized",
                        TranslatedKeys = g.Count(),
                        ApprovedKeys = g.Count(t => t.IsApproved)
                    })
                    .ToListAsync();

                foreach (var catStat in categoryStats)
                {
                    var totalInCategory = await _context.Translations
                        .Where(t => t.LanguageCode == "en" && t.Category == catStat.Category)
                        .CountAsync();
                    
                    catStat.TotalKeys = totalInCategory;
                    catStat.CompletionPercentage = totalInCategory > 0 
                        ? Math.Round((double)catStat.TranslatedKeys / totalInCategory * 100, 2) 
                        : 0;
                }

                var lastUpdated = await _context.Translations
                    .Where(t => t.LanguageCode == languageCode)
                    .MaxAsync(t => (DateTime?)t.UpdatedAt);

                return new TranslationStatsDto
                {
                    LanguageCode = languageCode,
                    LanguageName = language.Name,
                    TotalKeys = totalKeys,
                    TranslatedKeys = translatedKeys,
                    ApprovedKeys = approvedKeys,
                    CompletionPercentage = totalKeys > 0 ? Math.Round((double)translatedKeys / totalKeys * 100, 2) : 0,
                    ApprovalPercentage = translatedKeys > 0 ? Math.Round((double)approvedKeys / translatedKeys * 100, 2) : 0,
                    LastUpdated = lastUpdated,
                    CategoryStats = categoryStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation stats for: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<List<TranslationStatsDto>> GetAllLanguageStatsAsync()
        {
            try
            {
                var languages = await _context.Languages.Where(l => l.IsEnabled).ToListAsync();
                var stats = new List<TranslationStatsDto>();

                foreach (var language in languages)
                {
                    var languageStats = await GetTranslationStatsAsync(language.Code);
                    stats.Add(languageStats);
                }

                return stats.OrderByDescending(s => s.CompletionPercentage).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all language stats");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> ExportTranslationsAsync(string languageCode, string format = "json")
        {
            try
            {
                var translations = await GetTranslationsForLanguageAsync(languageCode);
                
                return new Dictionary<string, object>
                {
                    ["language"] = languageCode,
                    ["format"] = format,
                    ["exportedAt"] = DateTime.UtcNow,
                    ["count"] = translations.Count,
                    ["translations"] = translations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting translations for: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<bool> ImportTranslationsAsync(string languageCode, Dictionary<string, object> translations, string importedBy)
        {
            try
            {
                var translationRequests = new List<TranslationKeyValue>();

                foreach (var kvp in translations)
                {
                    if (kvp.Value is string value)
                    {
                        translationRequests.Add(new TranslationKeyValue
                        {
                            Key = kvp.Key,
                            Value = value
                        });
                    }
                }

                var bulkRequest = new BulkTranslationRequest
                {
                    LanguageCode = languageCode,
                    Translations = translationRequests
                };

                await BulkCreateTranslationsAsync(bulkRequest, importedBy);

                _logger.LogInformation("Imported {Count} translations for {LanguageCode} by {ImportedBy}", 
                    translationRequests.Count, languageCode, importedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing translations for: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<List<string>> GetMissingTranslationKeysAsync(string languageCode, string? category = null)
        {
            try
            {
                var sourceQuery = _context.Translations.Where(t => t.LanguageCode == "en" && t.IsActive);
                var targetQuery = _context.Translations.Where(t => t.LanguageCode == languageCode && t.IsActive);

                if (!string.IsNullOrEmpty(category))
                {
                    sourceQuery = sourceQuery.Where(t => t.Category == category);
                    targetQuery = targetQuery.Where(t => t.Category == category);
                }

                var sourceKeys = await sourceQuery.Select(t => t.Key).ToListAsync();
                var targetKeys = await targetQuery.Select(t => t.Key).ToListAsync();

                return sourceKeys.Except(targetKeys).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missing translation keys for: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<bool> SyncTranslationKeysAsync(string sourceLanguageCode, string targetLanguageCode, string syncedBy)
        {
            try
            {
                var missingKeys = await GetMissingTranslationKeysAsync(targetLanguageCode);
                var sourceTranslations = await _context.Translations
                    .Where(t => t.LanguageCode == sourceLanguageCode && missingKeys.Contains(t.Key) && t.IsActive)
                    .ToListAsync();

                var newTranslations = sourceTranslations.Select(st => new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = targetLanguageCode,
                    Key = st.Key,
                    Value = $"[NEEDS TRANSLATION] {st.Value}",
                    Category = st.Category,
                    Context = st.Context,
                    IsPlural = st.IsPlural,
                    PluralValue = st.PluralValue != null ? $"[NEEDS TRANSLATION] {st.PluralValue}" : null,
                    Priority = st.Priority,
                    TranslatedBy = syncedBy,
                    TranslatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                }).ToList();

                if (newTranslations.Any())
                {
                    _context.Translations.AddRange(newTranslations);
                    await _context.SaveChangesAsync();

                    InvalidateTranslationCache(targetLanguageCode, null);

                    _logger.LogInformation("Synced {Count} translation keys from {Source} to {Target} by {SyncedBy}", 
                        newTranslations.Count, sourceLanguageCode, targetLanguageCode, syncedBy);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing translation keys from {Source} to {Target}", sourceLanguageCode, targetLanguageCode);
                throw;
            }
        }

        private void InvalidateTranslationCache(string languageCode, string? category)
        {
            var cacheKey = string.Format(TRANSLATION_CACHE_KEY, languageCode, category ?? "all");
            _cache.Remove(cacheKey);
            
            var allCacheKey = string.Format(TRANSLATION_CACHE_KEY, languageCode, "all");
            _cache.Remove(allCacheKey);
        }

        private static TranslationDto MapToDto(Translation translation)
        {
            return new TranslationDto
            {
                Id = translation.Id.ToString(),
                LanguageCode = translation.LanguageCode,
                Key = translation.Key,
                Value = translation.Value,
                Category = translation.Category,
                Context = translation.Context,
                IsPlural = translation.IsPlural,
                PluralValue = translation.PluralValue,
                IsApproved = translation.IsApproved,
                ApprovedBy = translation.ApprovedBy,
                ApprovedAt = translation.ApprovedAt,
                TranslatedBy = translation.TranslatedBy,
                TranslatedAt = translation.TranslatedAt,
                Notes = translation.Notes,
                IsActive = translation.IsActive,
                Region = translation.Region,
                Priority = translation.Priority,
                CreatedAt = translation.CreatedAt,
                UpdatedAt = translation.UpdatedAt
            };
        }
    }
}
