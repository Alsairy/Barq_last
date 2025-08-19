using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SystemTask = System.Threading.Tasks.Task;

namespace BARQ.Application.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<LanguageService> _logger;

        public LanguageService(BarqDbContext context, ILogger<LanguageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<LanguageDto>> GetLanguagesAsync(ListRequest request)
        {
            try
            {
                var query = _context.Languages.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(l => l.Code.Contains(request.SearchTerm) ||
                                           l.Name.Contains(request.SearchTerm) ||
                                           l.NativeName.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(l => EF.Property<object>(l, request.SortBy))
                        : query.OrderBy(l => EF.Property<object>(l, request.SortBy));
                }
                else
                {
                    query = query.OrderBy(l => l.SortOrder).ThenBy(l => l.Name);
                }

                var totalCount = await query.CountAsync();
                var languages = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var languageDtos = languages.Select(MapToDto).ToList();

                return new PagedResult<LanguageDto>
                {
                    Items = languageDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting languages");
                throw;
            }
        }

        public async Task<LanguageDto?> GetLanguageByIdAsync(Guid id)
        {
            try
            {
                var language = await _context.Languages.FindAsync(id);
                return language != null ? MapToDto(language) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language by ID: {Id}", id);
                throw;
            }
        }

        public async Task<LanguageDto?> GetLanguageByCodeAsync(string code)
        {
            try
            {
                var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == code);
                return language != null ? MapToDto(language) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language by code: {Code}", code);
                throw;
            }
        }

        public async Task<List<LanguageDto>> GetEnabledLanguagesAsync()
        {
            try
            {
                var languages = await _context.Languages
                    .Where(l => l.IsEnabled)
                    .OrderBy(l => l.SortOrder)
                    .ThenBy(l => l.Name)
                    .ToListAsync();

                return languages.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled languages");
                throw;
            }
        }

        public async Task<LanguageDto?> GetDefaultLanguageAsync()
        {
            try
            {
                var language = await _context.Languages.FirstOrDefaultAsync(l => l.IsDefault && l.IsEnabled);
                return language != null ? MapToDto(language) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default language");
                throw;
            }
        }

        public async Task<LanguageDto> CreateLanguageAsync(CreateLanguageRequest request, string createdBy)
        {
            try
            {
                var existingLanguage = await _context.Languages.FirstOrDefaultAsync(l => l.Code == request.Code);
                if (existingLanguage != null)
                {
                    throw new InvalidOperationException($"Language with code '{request.Code}' already exists");
                }

                if (request.IsDefault)
                {
                    await ClearDefaultLanguageAsync();
                }

                var language = new Language
                {
                    Id = Guid.NewGuid(),
                    Code = request.Code,
                    Name = request.Name,
                    NativeName = request.NativeName,
                    Direction = request.Direction,
                    IsEnabled = request.IsEnabled,
                    IsDefault = request.IsDefault,
                    SortOrder = request.SortOrder,
                    Region = request.Region,
                    CultureCode = request.CultureCode,
                    DateFormat = request.DateFormat,
                    TimeFormat = request.TimeFormat,
                    NumberFormat = request.NumberFormat,
                    CurrencySymbol = request.CurrencySymbol,
                    CurrencyPosition = request.CurrencyPosition,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.Languages.Add(language);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Language created: {Code} ({Name}) by {CreatedBy}", 
                    request.Code, request.Name, createdBy);

                return MapToDto(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating language");
                throw;
            }
        }

        public async Task<LanguageDto?> UpdateLanguageAsync(Guid id, UpdateLanguageRequest request, string updatedBy)
        {
            try
            {
                var language = await _context.Languages.FindAsync(id);
                if (language == null)
                {
                    return null;
                }

                if (request.IsDefault.HasValue && request.IsDefault.Value)
                {
                    await ClearDefaultLanguageAsync();
                }

                if (request.Name != null) language.Name = request.Name;
                if (request.NativeName != null) language.NativeName = request.NativeName;
                if (request.Direction != null) language.Direction = request.Direction;
                if (request.IsEnabled.HasValue) language.IsEnabled = request.IsEnabled.Value;
                if (request.IsDefault.HasValue) language.IsDefault = request.IsDefault.Value;
                if (request.SortOrder.HasValue) language.SortOrder = request.SortOrder.Value;
                if (request.Region != null) language.Region = request.Region;
                if (request.CultureCode != null) language.CultureCode = request.CultureCode;
                if (request.DateFormat != null) language.DateFormat = request.DateFormat;
                if (request.TimeFormat != null) language.TimeFormat = request.TimeFormat;
                if (request.NumberFormat != null) language.NumberFormat = request.NumberFormat;
                if (request.CurrencySymbol != null) language.CurrencySymbol = request.CurrencySymbol;
                if (request.CurrencyPosition != null) language.CurrencyPosition = request.CurrencyPosition;
                if (request.Notes != null) language.Notes = request.Notes;

                language.UpdatedAt = DateTime.UtcNow;
                language.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Language updated: {Id} by {UpdatedBy}", id, updatedBy);

                return MapToDto(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating language: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteLanguageAsync(Guid id, string deletedBy)
        {
            try
            {
                var language = await _context.Languages.FindAsync(id);
                if (language == null)
                {
                    return false;
                }

                if (language.IsDefault)
                {
                    throw new InvalidOperationException("Cannot delete the default language");
                }

                var hasTranslations = await _context.Translations.AnyAsync(t => t.LanguageCode == language.Code);
                if (hasTranslations)
                {
                    throw new InvalidOperationException("Cannot delete language that has translations");
                }

                language.IsDeleted = true;
                language.UpdatedAt = DateTime.UtcNow;
                language.UpdatedBy = deletedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Language deleted: {Id} by {DeletedBy}", id, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting language: {Id}", id);
                throw;
            }
        }

        public async Task<bool> SetDefaultLanguageAsync(Guid id, string updatedBy)
        {
            try
            {
                var language = await _context.Languages.FindAsync(id);
                if (language == null || !language.IsEnabled)
                {
                    return false;
                }

                await ClearDefaultLanguageAsync();

                language.IsDefault = true;
                language.UpdatedAt = DateTime.UtcNow;
                language.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Default language set: {Id} by {UpdatedBy}", id, updatedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default language: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleLanguageAsync(Guid id, bool isEnabled, string updatedBy)
        {
            try
            {
                var language = await _context.Languages.FindAsync(id);
                if (language == null)
                {
                    return false;
                }

                if (!isEnabled && language.IsDefault)
                {
                    throw new InvalidOperationException("Cannot disable the default language");
                }

                language.IsEnabled = isEnabled;
                language.UpdatedAt = DateTime.UtcNow;
                language.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Language toggled: {Id} to {IsEnabled} by {UpdatedBy}", id, isEnabled, updatedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling language: {Id}", id);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetLanguageStatsAsync()
        {
            try
            {
                var totalLanguages = await _context.Languages.CountAsync();
                var enabledLanguages = await _context.Languages.CountAsync(l => l.IsEnabled);
                var rtlLanguages = await _context.Languages.CountAsync(l => l.Direction == "rtl" && l.IsEnabled);
                
                var languageUsage = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .GroupBy(ulp => ulp.LanguageCode)
                    .Select(g => new { LanguageCode = g.Key, UserCount = g.Count() })
                    .OrderByDescending(x => x.UserCount)
                    .Take(10)
                    .ToListAsync();

                var avgCompletionPercentage = await _context.Languages
                    .Where(l => l.IsEnabled)
                    .AverageAsync(l => l.CompletionPercentage);

                return new Dictionary<string, object>
                {
                    ["TotalLanguages"] = totalLanguages,
                    ["EnabledLanguages"] = enabledLanguages,
                    ["RTLLanguages"] = rtlLanguages,
                    ["LanguageUsage"] = languageUsage,
                    ["AverageCompletionPercentage"] = Math.Round(avgCompletionPercentage, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language stats");
                throw;
            }
        }

        public async SystemTask RefreshLanguageCompletionAsync(string languageCode)
        {
            try
            {
                var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == languageCode);
                if (language == null)
                {
                    return;
                }

                var totalKeys = await _context.Translations.CountAsync(t => t.LanguageCode == "en" && t.IsActive);
                var translatedKeys = await _context.Translations.CountAsync(t => t.LanguageCode == languageCode && t.IsActive);

                language.CompletionPercentage = totalKeys > 0 ? Math.Round((double)translatedKeys / totalKeys * 100, 2) : 0;
                language.UpdatedAt = DateTime.UtcNow;
                language.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Language completion refreshed: {LanguageCode} - {Percentage}%", 
                    languageCode, language.CompletionPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing language completion: {LanguageCode}", languageCode);
                throw;
            }
        }

        public async Task<List<LanguageDto>> GetLanguagesByDirectionAsync(string direction)
        {
            try
            {
                var languages = await _context.Languages
                    .Where(l => l.Direction == direction && l.IsEnabled)
                    .OrderBy(l => l.SortOrder)
                    .ThenBy(l => l.Name)
                    .ToListAsync();

                return languages.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting languages by direction: {Direction}", direction);
                throw;
            }
        }

        public async Task<bool> ValidateLanguageCodeAsync(string code)
        {
            try
            {
                var exists = await _context.Languages.AnyAsync(l => l.Code == code);
                return !exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating language code: {Code}", code);
                return false;
            }
        }

        private async SystemTask ClearDefaultLanguageAsync()
        {
            var currentDefault = await _context.Languages.FirstOrDefaultAsync(l => l.IsDefault);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                currentDefault.UpdatedAt = DateTime.UtcNow;
                currentDefault.UpdatedBy = "System";
            }
        }

        private static LanguageDto MapToDto(Language language)
        {
            return new LanguageDto
            {
                Id = language.Id.ToString(),
                Code = language.Code,
                Name = language.Name,
                NativeName = language.NativeName,
                Direction = language.Direction,
                IsEnabled = language.IsEnabled,
                IsDefault = language.IsDefault,
                SortOrder = language.SortOrder,
                Region = language.Region,
                CultureCode = language.CultureCode,
                DateFormat = language.DateFormat,
                TimeFormat = language.TimeFormat,
                NumberFormat = language.NumberFormat,
                CurrencySymbol = language.CurrencySymbol,
                CurrencyPosition = language.CurrencyPosition,
                CompletionPercentage = language.CompletionPercentage,
                Notes = language.Notes,
                CreatedAt = language.CreatedAt,
                UpdatedAt = language.UpdatedAt
            };
        }
    }
}
