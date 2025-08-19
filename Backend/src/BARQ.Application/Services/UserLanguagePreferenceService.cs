using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public class UserLanguagePreferenceService : IUserLanguagePreferenceService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<UserLanguagePreferenceService> _logger;

        public UserLanguagePreferenceService(BarqDbContext context, ILogger<UserLanguagePreferenceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<UserLanguagePreferenceDto>> GetUserLanguagePreferencesAsync(Guid userId, ListRequest request)
        {
            try
            {
                var query = _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .Where(ulp => ulp.UserId == userId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(ulp => ulp.Language.Name.Contains(request.SearchTerm) ||
                                             ulp.Language.NativeName.Contains(request.SearchTerm) ||
                                             ulp.LanguageCode.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(ulp => EF.Property<object>(ulp, request.SortBy))
                        : query.OrderBy(ulp => EF.Property<object>(ulp, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(ulp => ulp.IsDefault).ThenBy(ulp => ulp.Language.Name);
                }

                var totalCount = await query.CountAsync();
                var preferences = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var preferenceDtos = preferences.Select(MapToDto).ToList();

                return new PagedResult<UserLanguagePreferenceDto>
                {
                    Items = preferenceDtos,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preferences for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserLanguagePreferenceDto?> GetUserLanguagePreferenceByIdAsync(Guid id)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .FirstOrDefaultAsync(ulp => ulp.Id == id);

                return preference != null ? MapToDto(preference) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preference by ID: {Id}", id);
                throw;
            }
        }

        public async Task<UserLanguagePreferenceDto?> GetUserDefaultLanguagePreferenceAsync(Guid userId)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.IsDefault);

                return preference != null ? MapToDto(preference) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user default language preference for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserLanguagePreferenceDto?> GetUserLanguagePreferenceByCodeAsync(Guid userId, string languageCode)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.LanguageCode == languageCode);

                return preference != null ? MapToDto(preference) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preference by code: {UserId}/{LanguageCode}", userId, languageCode);
                throw;
            }
        }

        public async Task<UserLanguagePreferenceDto> CreateUserLanguagePreferenceAsync(Guid userId, string languageId, string createdBy)
        {
            try
            {
                var language = await _context.Languages.FindAsync(Guid.Parse(languageId));
                if (language == null || !language.IsEnabled)
                {
                    throw new InvalidOperationException("Language not found or not enabled");
                }

                var existingPreference = await _context.UserLanguagePreferences
                    .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.LanguageCode == language.Code);

                if (existingPreference != null)
                {
                    throw new InvalidOperationException($"User already has preference for language '{language.Code}'");
                }

                var isFirstPreference = !await _context.UserLanguagePreferences.AnyAsync(ulp => ulp.UserId == userId);

                var preference = new UserLanguagePreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LanguageId = Guid.Parse(languageId),
                    LanguageCode = language.Code,
                    IsDefault = isFirstPreference,
                    DateFormat = language.DateFormat,
                    TimeFormat = language.TimeFormat,
                    NumberFormat = language.NumberFormat,
                    CurrencyCode = language.CurrencySymbol,
                    UseRTL = language.Direction == "rtl",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.UserLanguagePreferences.Add(preference);
                await _context.SaveChangesAsync();

                preference.Language = language;

                _logger.LogInformation("User language preference created: {UserId}/{LanguageCode} by {CreatedBy}", 
                    userId, language.Code, createdBy);

                return MapToDto(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user language preference");
                throw;
            }
        }

        public async Task<UserLanguagePreferenceDto?> UpdateUserLanguagePreferenceAsync(Guid id, UpdateUserLanguagePreferenceRequest request, string updatedBy)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .FirstOrDefaultAsync(ulp => ulp.Id == id);

                if (preference == null)
                {
                    return null;
                }

                if (request.LanguageId != null)
                {
                    var language = await _context.Languages.FindAsync(Guid.Parse(request.LanguageId));
                    if (language == null || !language.IsEnabled)
                    {
                        throw new InvalidOperationException("Language not found or not enabled");
                    }

                    preference.LanguageId = Guid.Parse(request.LanguageId);
                    preference.LanguageCode = language.Code;
                    preference.Language = language;
                }

                if (request.IsDefault.HasValue && request.IsDefault.Value)
                {
                    await ClearDefaultLanguagePreferenceAsync(preference.UserId);
                    preference.IsDefault = true;
                }
                else if (request.IsDefault.HasValue)
                {
                    preference.IsDefault = request.IsDefault.Value;
                }

                if (request.DateFormat != null) preference.DateFormat = request.DateFormat;
                if (request.TimeFormat != null) preference.TimeFormat = request.TimeFormat;
                if (request.NumberFormat != null) preference.NumberFormat = request.NumberFormat;
                if (request.Timezone != null) preference.Timezone = request.Timezone;
                if (request.CurrencyCode != null) preference.CurrencyCode = request.CurrencyCode;
                if (request.UseRTL.HasValue) preference.UseRTL = request.UseRTL.Value;
                if (request.HighContrast.HasValue) preference.HighContrast = request.HighContrast.Value;
                if (request.LargeText.HasValue) preference.LargeText = request.LargeText.Value;
                if (request.ReducedMotion.HasValue) preference.ReducedMotion = request.ReducedMotion.Value;
                if (request.ScreenReaderOptimized.HasValue) preference.ScreenReaderOptimized = request.ScreenReaderOptimized.Value;
                if (request.KeyboardNavigation != null) preference.KeyboardNavigation = request.KeyboardNavigation;

                preference.UpdatedAt = DateTime.UtcNow;
                preference.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User language preference updated: {Id} by {UpdatedBy}", id, updatedBy);

                return MapToDto(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user language preference: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserLanguagePreferenceAsync(Guid id, string deletedBy)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences.FindAsync(id);
                if (preference == null)
                {
                    return false;
                }

                if (preference.IsDefault)
                {
                    var otherPreference = await _context.UserLanguagePreferences
                        .FirstOrDefaultAsync(ulp => ulp.UserId == preference.UserId && ulp.Id != id);

                    if (otherPreference != null)
                    {
                        otherPreference.IsDefault = true;
                        otherPreference.UpdatedAt = DateTime.UtcNow;
                        otherPreference.UpdatedBy = deletedBy;
                    }
                }

                preference.IsDeleted = true;
                preference.UpdatedAt = DateTime.UtcNow;
                preference.UpdatedBy = deletedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User language preference deleted: {Id} by {DeletedBy}", id, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user language preference: {Id}", id);
                throw;
            }
        }

        public async Task<bool> SetDefaultLanguagePreferenceAsync(Guid userId, Guid preferenceId, string updatedBy)
        {
            try
            {
                var preference = await _context.UserLanguagePreferences
                    .FirstOrDefaultAsync(ulp => ulp.Id == preferenceId && ulp.UserId == userId);

                if (preference == null)
                {
                    return false;
                }

                await ClearDefaultLanguagePreferenceAsync(userId);

                preference.IsDefault = true;
                preference.UpdatedAt = DateTime.UtcNow;
                preference.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Default language preference set: {UserId}/{PreferenceId} by {UpdatedBy}", 
                    userId, preferenceId, updatedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default language preference: {UserId}/{PreferenceId}", userId, preferenceId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetUserAccessibilitySettingsAsync(Guid userId)
        {
            try
            {
                var preferences = await _context.UserLanguagePreferences
                    .Where(ulp => ulp.UserId == userId)
                    .ToListAsync();

                var defaultPreference = preferences.FirstOrDefault(p => p.IsDefault);

                return new Dictionary<string, object>
                {
                    ["UserId"] = userId,
                    ["HighContrast"] = defaultPreference?.HighContrast ?? false,
                    ["LargeText"] = defaultPreference?.LargeText ?? false,
                    ["ReducedMotion"] = defaultPreference?.ReducedMotion ?? false,
                    ["ScreenReaderOptimized"] = defaultPreference?.ScreenReaderOptimized ?? false,
                    ["KeyboardNavigation"] = defaultPreference?.KeyboardNavigation ?? "Standard",
                    ["UseRTL"] = defaultPreference?.UseRTL ?? false,
                    ["PreferenceCount"] = preferences.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user accessibility settings: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserAccessibilitySettingsAsync(Guid userId, UpdateUserLanguagePreferenceRequest request, string updatedBy)
        {
            try
            {
                var defaultPreference = await _context.UserLanguagePreferences
                    .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.IsDefault);

                if (defaultPreference == null)
                {
                    return false;
                }

                if (request.HighContrast.HasValue) defaultPreference.HighContrast = request.HighContrast.Value;
                if (request.LargeText.HasValue) defaultPreference.LargeText = request.LargeText.Value;
                if (request.ReducedMotion.HasValue) defaultPreference.ReducedMotion = request.ReducedMotion.Value;
                if (request.ScreenReaderOptimized.HasValue) defaultPreference.ScreenReaderOptimized = request.ScreenReaderOptimized.Value;
                if (request.KeyboardNavigation != null) defaultPreference.KeyboardNavigation = request.KeyboardNavigation;

                defaultPreference.UpdatedAt = DateTime.UtcNow;
                defaultPreference.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User accessibility settings updated: {UserId} by {UpdatedBy}", userId, updatedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user accessibility settings: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<UserLanguagePreferenceDto>> GetUsersWithAccessibilityNeedsAsync()
        {
            try
            {
                var preferences = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .Where(ulp => ulp.HighContrast || ulp.LargeText || ulp.ReducedMotion || 
                                ulp.ScreenReaderOptimized || ulp.KeyboardNavigation != "Standard")
                    .ToListAsync();

                return preferences.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with accessibility needs");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetLanguageUsageStatsAsync()
        {
            try
            {
                var languageUsage = await _context.UserLanguagePreferences
                    .Include(ulp => ulp.Language)
                    .GroupBy(ulp => ulp.LanguageCode)
                    .Select(g => new
                    {
                        LanguageCode = g.Key,
                        LanguageName = g.First().Language.Name,
                        UserCount = g.Count(),
                        DefaultUsers = g.Count(ulp => ulp.IsDefault),
                        RTLUsers = g.Count(ulp => ulp.UseRTL),
                        AccessibilityUsers = g.Count(ulp => ulp.HighContrast || ulp.LargeText || 
                                                          ulp.ReducedMotion || ulp.ScreenReaderOptimized)
                    })
                    .OrderByDescending(x => x.UserCount)
                    .ToListAsync();

                var totalUsers = await _context.UserLanguagePreferences
                    .Select(ulp => ulp.UserId)
                    .Distinct()
                    .CountAsync();

                var accessibilityStats = await _context.UserLanguagePreferences
                    .GroupBy(ulp => 1)
                    .Select(g => new
                    {
                        HighContrastUsers = g.Count(ulp => ulp.HighContrast),
                        LargeTextUsers = g.Count(ulp => ulp.LargeText),
                        ReducedMotionUsers = g.Count(ulp => ulp.ReducedMotion),
                        ScreenReaderUsers = g.Count(ulp => ulp.ScreenReaderOptimized),
                        RTLUsers = g.Count(ulp => ulp.UseRTL)
                    })
                    .FirstOrDefaultAsync();

                var accessibilityStatsDict = accessibilityStats != null 
                    ? new Dictionary<string, object>
                    {
                        ["HighContrastUsers"] = accessibilityStats.HighContrastUsers,
                        ["LargeTextUsers"] = accessibilityStats.LargeTextUsers,
                        ["ReducedMotionUsers"] = accessibilityStats.ReducedMotionUsers,
                        ["ScreenReaderUsers"] = accessibilityStats.ScreenReaderUsers,
                        ["RTLUsers"] = accessibilityStats.RTLUsers
                    }
                    : new Dictionary<string, object>();

                return new Dictionary<string, object>
                {
                    ["TotalUsers"] = totalUsers,
                    ["LanguageUsage"] = languageUsage,
                    ["AccessibilityStats"] = accessibilityStatsDict,
                    ["GeneratedAt"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language usage stats");
                throw;
            }
        }

        private async Task ClearDefaultLanguagePreferenceAsync(Guid userId)
        {
            var currentDefault = await _context.UserLanguagePreferences
                .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.IsDefault);

            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                currentDefault.UpdatedAt = DateTime.UtcNow;
                currentDefault.UpdatedBy = "System";
            }
        }

        private static UserLanguagePreferenceDto MapToDto(UserLanguagePreference preference)
        {
            return new UserLanguagePreferenceDto
            {
                Id = preference.Id.ToString(),
                UserId = preference.UserId.ToString(),
                LanguageId = preference.LanguageId.ToString(),
                LanguageCode = preference.LanguageCode,
                LanguageName = preference.Language?.Name ?? "",
                LanguageNativeName = preference.Language?.NativeName ?? "",
                IsDefault = preference.IsDefault,
                DateFormat = preference.DateFormat,
                TimeFormat = preference.TimeFormat,
                NumberFormat = preference.NumberFormat,
                Timezone = preference.Timezone,
                CurrencyCode = preference.CurrencyCode,
                UseRTL = preference.UseRTL,
                HighContrast = preference.HighContrast,
                LargeText = preference.LargeText,
                ReducedMotion = preference.ReducedMotion,
                ScreenReaderOptimized = preference.ScreenReaderOptimized,
                KeyboardNavigation = preference.KeyboardNavigation,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt
            };
        }
    }
}
