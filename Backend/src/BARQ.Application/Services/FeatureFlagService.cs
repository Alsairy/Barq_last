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
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<FeatureFlagService> _logger;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_PREFIX = "feature_flag_";
        private const int CACHE_DURATION_MINUTES = 5;

        public FeatureFlagService(BarqDbContext context, ILogger<FeatureFlagService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<PagedResult<FeatureFlagDto>> GetFeatureFlagsAsync(ListRequest request)
        {
            try
            {
                var query = _context.FeatureFlags.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(f => f.Name.Contains(request.SearchTerm) || 
                                           f.DisplayName.Contains(request.SearchTerm) ||
                                           (f.Description != null && f.Description.Contains(request.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc" 
                        ? query.OrderByDescending(f => EF.Property<object>(f, request.SortBy))
                        : query.OrderBy(f => EF.Property<object>(f, request.SortBy));
                }
                else
                {
                    query = query.OrderBy(f => f.Priority).ThenBy(f => f.Name);
                }

                var totalCount = await query.CountAsync();
                var flags = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var flagDtos = flags.Select(MapToDto).ToList();

                return new PagedResult<FeatureFlagDto>
                {
                    Items = flagDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags");
                throw;
            }
        }

        public async Task<FeatureFlagDto?> GetFeatureFlagByIdAsync(Guid id)
        {
            try
            {
                var flag = await _context.FeatureFlags.FindAsync(id);
                return flag != null ? MapToDto(flag) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flag by ID: {Id}", id);
                throw;
            }
        }

        public async Task<FeatureFlagDto?> GetFeatureFlagByNameAsync(string name)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{name}";
                if (_cache.TryGetValue(cacheKey, out FeatureFlagDto? cachedFlag))
                {
                    return cachedFlag;
                }

                var flag = await _context.FeatureFlags
                    .FirstOrDefaultAsync(f => f.Name == name);

                var flagDto = flag != null ? MapToDto(flag) : null;
                
                if (flagDto != null)
                {
                    _cache.Set(cacheKey, flagDto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }

                return flagDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flag by name: {Name}", name);
                throw;
            }
        }

        public async Task<FeatureFlagDto> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, string createdBy)
        {
            try
            {
                var existingFlag = await _context.FeatureFlags
                    .FirstOrDefaultAsync(f => f.Name == request.Name);

                if (existingFlag != null)
                {
                    throw new InvalidOperationException($"Feature flag with name '{request.Name}' already exists");
                }

                var flag = new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Description = request.Description,
                    IsEnabled = false,
                    Environment = request.Environment,
                    Category = request.Category,
                    ImpactDescription = request.ImpactDescription,
                    RequiresRestart = request.RequiresRestart,
                    Priority = request.Priority,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.TryParse(createdBy, out var createdByGuid) ? createdByGuid : null
                };

                _context.FeatureFlags.Add(flag);
                await _context.SaveChangesAsync();

                await LogFeatureFlagHistoryAsync(flag.Id, "Created", false, false, createdBy, "Feature flag created");

                _logger.LogInformation("Feature flag created: {Name} by {CreatedBy}", flag.Name, createdBy);
                
                await RefreshFeatureFlagCacheAsync();
                
                return MapToDto(flag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature flag: {Name}", request.Name);
                throw;
            }
        }

        public async Task<FeatureFlagDto?> UpdateFeatureFlagAsync(Guid id, UpdateFeatureFlagRequest request, string updatedBy)
        {
            try
            {
                var flag = await _context.FeatureFlags.FindAsync(id);
                if (flag == null)
                {
                    return null;
                }

                var previousEnabled = flag.IsEnabled;
                var hasChanges = false;

                if (!string.IsNullOrEmpty(request.DisplayName) && request.DisplayName != flag.DisplayName)
                {
                    flag.DisplayName = request.DisplayName;
                    hasChanges = true;
                }

                if (request.Description != flag.Description)
                {
                    flag.Description = request.Description;
                    hasChanges = true;
                }

                if (request.IsEnabled.HasValue && request.IsEnabled.Value != flag.IsEnabled)
                {
                    flag.IsEnabled = request.IsEnabled.Value;
                    flag.EnabledAt = request.IsEnabled.Value ? DateTime.UtcNow : null;
                    flag.DisabledAt = !request.IsEnabled.Value ? DateTime.UtcNow : null;
                    flag.EnabledBy = request.IsEnabled.Value ? updatedBy : flag.EnabledBy;
                    flag.DisabledBy = !request.IsEnabled.Value ? updatedBy : flag.DisabledBy;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.Environment) && request.Environment != flag.Environment)
                {
                    flag.Environment = request.Environment;
                    hasChanges = true;
                }

                if (request.Category != flag.Category)
                {
                    flag.Category = request.Category;
                    hasChanges = true;
                }

                if (request.ImpactDescription != flag.ImpactDescription)
                {
                    flag.ImpactDescription = request.ImpactDescription;
                    hasChanges = true;
                }

                if (request.RolloutPercentage.HasValue && request.RolloutPercentage.Value != flag.RolloutPercentage)
                {
                    flag.RolloutPercentage = request.RolloutPercentage.Value;
                    hasChanges = true;
                }

                if (request.RequiresRestart.HasValue && request.RequiresRestart.Value != flag.RequiresRestart)
                {
                    flag.RequiresRestart = request.RequiresRestart.Value;
                    hasChanges = true;
                }

                if (request.Priority.HasValue && request.Priority.Value != flag.Priority)
                {
                    flag.Priority = request.Priority.Value;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    flag.UpdatedAt = DateTime.UtcNow;
                    flag.UpdatedBy = Guid.TryParse(updatedBy, out var updatedByGuid) ? updatedByGuid : null;
                    await _context.SaveChangesAsync();

                    await LogFeatureFlagHistoryAsync(flag.Id, "Updated", previousEnabled, flag.IsEnabled, updatedBy, request.Reason);

                    _logger.LogInformation("Feature flag updated: {Name} by {UpdatedBy}", flag.Name, updatedBy);
                    
                    await RefreshFeatureFlagCacheAsync();
                }

                return MapToDto(flag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature flag: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteFeatureFlagAsync(Guid id, string deletedBy)
        {
            try
            {
                var flag = await _context.FeatureFlags.FindAsync(id);
                if (flag == null)
                {
                    return false;
                }

                if (flag.IsSystemFlag)
                {
                    throw new InvalidOperationException("System feature flags cannot be deleted");
                }

                await LogFeatureFlagHistoryAsync(flag.Id, "Deleted", flag.IsEnabled, false, deletedBy, "Feature flag deleted");

                _context.FeatureFlags.Remove(flag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Feature flag deleted: {Name} by {DeletedBy}", flag.Name, deletedBy);
                
                await RefreshFeatureFlagCacheAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feature flag: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleFeatureFlagAsync(Guid id, bool isEnabled, string changedBy, string? reason = null)
        {
            try
            {
                var flag = await _context.FeatureFlags.FindAsync(id);
                if (flag == null)
                {
                    return false;
                }

                var previousEnabled = flag.IsEnabled;
                flag.IsEnabled = isEnabled;
                flag.EnabledAt = isEnabled ? DateTime.UtcNow : flag.EnabledAt;
                flag.DisabledAt = !isEnabled ? DateTime.UtcNow : flag.DisabledAt;
                flag.EnabledBy = isEnabled ? changedBy : flag.EnabledBy;
                flag.DisabledBy = !isEnabled ? changedBy : flag.DisabledBy;
                flag.UpdatedAt = DateTime.UtcNow;
                flag.UpdatedBy = Guid.TryParse(changedBy, out var updatedByGuid) ? updatedByGuid : null;

                await _context.SaveChangesAsync();

                var action = isEnabled ? "Enabled" : "Disabled";
                await LogFeatureFlagHistoryAsync(flag.Id, action, previousEnabled, isEnabled, changedBy, reason);

                _logger.LogInformation("Feature flag {Action}: {Name} by {ChangedBy}", action.ToLower(), flag.Name, changedBy);
                
                await RefreshFeatureFlagCacheAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling feature flag: {Id}", id);
                throw;
            }
        }

        public async Task<bool> IsFeatureEnabledAsync(string featureName, string? environment = null)
        {
            try
            {
                var flag = await GetFeatureFlagByNameAsync(featureName);
                if (flag == null)
                {
                    return false;
                }

                if (!flag.IsEnabled)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(environment) && 
                    flag.Environment != "All" && 
                    flag.Environment != environment)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if feature is enabled: {FeatureName}", featureName);
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> GetFeatureFlagsForEnvironmentAsync(string environment)
        {
            try
            {
                var flags = await _context.FeatureFlags
                    .Where(f => f.Environment == environment || f.Environment == "All")
                    .ToListAsync();

                return flags.ToDictionary(f => f.Name, f => f.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags for environment: {Environment}", environment);
                throw;
            }
        }

        public async Task<List<FeatureFlagDto>> GetFeatureFlagsByCategoryAsync(string category)
        {
            try
            {
                var flags = await _context.FeatureFlags
                    .Where(f => f.Category == category)
                    .OrderBy(f => f.Priority)
                    .ThenBy(f => f.Name)
                    .ToListAsync();

                return flags.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags by category: {Category}", category);
                throw;
            }
        }

        public async System.Threading.Tasks.Task RefreshFeatureFlagCacheAsync()
        {
            try
            {
                var flags = await _context.FeatureFlags.ToListAsync();
                foreach (var flag in flags)
                {
                    var cacheKey = $"{CACHE_KEY_PREFIX}{flag.Name}";
                    _cache.Set(cacheKey, MapToDto(flag), TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }

                _logger.LogInformation("Feature flag cache refreshed for {Count} flags", flags.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing feature flag cache");
                throw;
            }
        }

        private async System.Threading.Tasks.Task LogFeatureFlagHistoryAsync(Guid featureFlagId, string action, bool previousValue, bool newValue, string changedBy, string? reason)
        {
            try
            {
                var history = new FeatureFlagHistory
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagId = featureFlagId,
                    Action = action,
                    PreviousValue = previousValue,
                    NewValue = newValue,
                    ChangedBy = changedBy,
                    Reason = reason,
                    ChangedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.TryParse(changedBy, out var createdByGuid2) ? createdByGuid2 : null
                };

                _context.FeatureFlagHistory.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging feature flag history for flag: {FeatureFlagId}", featureFlagId);
            }
        }

        private static FeatureFlagDto MapToDto(FeatureFlag flag)
        {
            return new FeatureFlagDto
            {
                Id = flag.Id.ToString(),
                Name = flag.Name,
                DisplayName = flag.DisplayName,
                Description = flag.Description,
                IsEnabled = flag.IsEnabled,
                Environment = flag.Environment,
                Category = flag.Category,
                EnabledAt = flag.EnabledAt,
                DisabledAt = flag.DisabledAt,
                EnabledBy = flag.EnabledBy,
                DisabledBy = flag.DisabledBy,
                ImpactDescription = flag.ImpactDescription,
                RolloutPercentage = flag.RolloutPercentage,
                RequiresRestart = flag.RequiresRestart,
                IsSystemFlag = flag.IsSystemFlag,
                Priority = flag.Priority,
                CreatedAt = flag.CreatedAt,
                UpdatedAt = flag.UpdatedAt
            };
        }
    }
}
