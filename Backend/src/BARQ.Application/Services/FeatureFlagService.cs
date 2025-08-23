using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;

namespace BARQ.Application.Services
{
    public sealed class FeatureFlagService : IFeatureFlagService
    {
        private readonly IConfiguration _cfg;
        private readonly BarqDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        
        public FeatureFlagService(IConfiguration cfg, BarqDbContext context, ITenantProvider tenantProvider)
        {
            _cfg = cfg;
            _context = context;
            _tenantProvider = tenantProvider;
        }
        
        public bool IsEnabled(string flagName) => _cfg.GetValue<bool>($"Features:{flagName}", false);

        public async System.Threading.Tasks.Task<PagedResult<FeatureFlagDto>> GetFeatureFlagsAsync(ListRequest request)
        {
            var query = _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && !f.IsDeleted);

            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(f => f.Name.Contains(request.SearchTerm) || f.DisplayName.Contains(request.SearchTerm));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(f => new FeatureFlagDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    IsEnabled = f.IsEnabled,
                    Environment = f.Environment,
                    Category = f.Category
                })
                .ToListAsync();

            return new PagedResult<FeatureFlagDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<FeatureFlagDto?> GetFeatureFlagByIdAsync(Guid id)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Id == id && !f.IsDeleted)
                .FirstOrDefaultAsync();

            if (featureFlag == null)
                return null;

            return new FeatureFlagDto
            {
                Id = featureFlag.Id,
                Name = featureFlag.Name,
                DisplayName = featureFlag.DisplayName,
                IsEnabled = featureFlag.IsEnabled,
                Environment = featureFlag.Environment,
                Category = featureFlag.Category
            };
        }

        public async System.Threading.Tasks.Task<FeatureFlagDto?> GetFeatureFlagByNameAsync(string name)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Name == name && !f.IsDeleted)
                .FirstOrDefaultAsync();

            if (featureFlag == null)
                return null;

            return new FeatureFlagDto
            {
                Id = featureFlag.Id,
                Name = featureFlag.Name,
                DisplayName = featureFlag.DisplayName,
                IsEnabled = featureFlag.IsEnabled,
                Environment = featureFlag.Environment,
                Category = featureFlag.Category
            };
        }

        public async System.Threading.Tasks.Task<FeatureFlagDto> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, string createdBy)
        {
            var featureFlag = new BARQ.Core.Entities.FeatureFlag
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.GetTenantId(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                IsEnabled = request.IsEnabled,
                Environment = request.Environment ?? "Production",
                Category = request.Category ?? "General",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.TryParse(createdBy, out var createdByGuid) ? createdByGuid : null
            };

            _context.FeatureFlags.Add(featureFlag);
            await _context.SaveChangesAsync();

            return new FeatureFlagDto
            {
                Id = featureFlag.Id,
                Name = featureFlag.Name,
                DisplayName = featureFlag.DisplayName,
                IsEnabled = featureFlag.IsEnabled,
                Environment = featureFlag.Environment,
                Category = featureFlag.Category
            };
        }

        public async System.Threading.Tasks.Task<FeatureFlagDto?> UpdateFeatureFlagAsync(Guid id, UpdateFeatureFlagRequest request, string updatedBy)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Id == id && !f.IsDeleted)
                .FirstOrDefaultAsync();

            if (featureFlag == null)
                return null;

            if (request.DisplayName != null) featureFlag.DisplayName = request.DisplayName;
            if (request.IsEnabled.HasValue) featureFlag.IsEnabled = request.IsEnabled.Value;
            if (request.Environment != null) featureFlag.Environment = request.Environment;
            if (request.Category != null) featureFlag.Category = request.Category;

            featureFlag.UpdatedAt = DateTime.UtcNow;
            featureFlag.UpdatedBy = Guid.TryParse(updatedBy, out var updatedByGuid) ? updatedByGuid : null;

            await _context.SaveChangesAsync();

            return new FeatureFlagDto
            {
                Id = featureFlag.Id,
                Name = featureFlag.Name,
                DisplayName = featureFlag.DisplayName,
                IsEnabled = featureFlag.IsEnabled,
                Environment = featureFlag.Environment,
                Category = featureFlag.Category
            };
        }

        public async System.Threading.Tasks.Task<bool> DeleteFeatureFlagAsync(Guid id, string deletedBy)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Id == id)
                .FirstOrDefaultAsync();
                
            if (featureFlag == null)
                return false;
                
            featureFlag.IsDeleted = true;
            featureFlag.UpdatedAt = DateTime.UtcNow;
            featureFlag.UpdatedBy = Guid.TryParse(deletedBy, out var deletedByGuid) ? deletedByGuid : null;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<bool> ToggleFeatureFlagAsync(Guid id, bool isEnabled, string changedBy, string? reason = null)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Id == id)
                .FirstOrDefaultAsync();
                
            if (featureFlag == null)
                return false;
                
            featureFlag.IsEnabled = isEnabled;
            featureFlag.UpdatedAt = DateTime.UtcNow;
            featureFlag.UpdatedBy = Guid.TryParse(changedBy, out var changedByGuid) ? changedByGuid : null;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<bool> IsFeatureEnabledAsync(string featureName, string? environment = null)
        {
            var featureFlag = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Name == featureName && !f.IsDeleted)
                .Where(f => environment == null || f.Environment == environment)
                .FirstOrDefaultAsync();

            return featureFlag?.IsEnabled ?? _cfg.GetValue<bool>($"Features:{featureName}", false);
        }

        public async System.Threading.Tasks.Task<Dictionary<string, bool>> GetFeatureFlagsForEnvironmentAsync(string environment)
        {
            var flags = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Environment == environment && !f.IsDeleted)
                .Select(f => new { f.Name, f.IsEnabled })
                .ToListAsync();

            return flags.ToDictionary(f => f.Name, f => f.IsEnabled);
        }

        public async System.Threading.Tasks.Task<List<FeatureFlagDto>> GetFeatureFlagsByCategoryAsync(string category)
        {
            var flags = await _context.FeatureFlags
                .Where(f => f.TenantId == _tenantProvider.GetTenantId() && f.Category == category && !f.IsDeleted)
                .Select(f => new FeatureFlagDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    IsEnabled = f.IsEnabled,
                    Environment = f.Environment,
                    Category = f.Category
                })
                .ToListAsync();

            return flags;
        }

        public async System.Threading.Tasks.Task RefreshFeatureFlagCacheAsync()
        {
            var tenantId = _tenantProvider.GetTenantId();
            var flags = await _context.FeatureFlags
                .Where(f => f.TenantId == tenantId && !f.IsDeleted)
                .ToListAsync();
            
            foreach (var flag in flags)
            {
                _cfg[$"Features:{flag.Name}"] = flag.IsEnabled.ToString();
            }
        }
    }
}
