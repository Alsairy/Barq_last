using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public sealed class TenantStateService : ITenantStateService
    {
        private readonly BarqDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<TenantStateService> _logger;

        public TenantStateService(BarqDbContext context, ITenantProvider tenantProvider, ILogger<TenantStateService> logger)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task<PagedResult<TenantStateDto>> GetTenantStatesAsync(ListRequest request)
        {
            var query = _context.TenantStates.Include(ts => ts.Tenant)
                .Where(ts => ts.TenantId == _tenantProvider.GetTenantId() && 
                            (string.IsNullOrEmpty(request.SearchTerm) || ts.Tenant.Name.Contains(request.SearchTerm)));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ts => new TenantStateDto
                {
                    Id = ts.Id.ToString(),
                    TenantId = ts.TenantId.ToString(),
                    TenantName = ts.Tenant.Name,
                    Status = ts.Status,
                    LastHealthCheck = ts.LastHealthCheck,
                    RequiresAttention = ts.RequiresAttention
                })
                .ToListAsync();

            return new PagedResult<TenantStateDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByIdAsync(Guid id)
        {
            var tenantState = await _context.TenantStates
                .Include(ts => ts.Tenant)
                .Where(ts => ts.Id == id && ts.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync();

            if (tenantState == null)
                return null;

            return new TenantStateDto
            {
                Id = tenantState.Id.ToString(),
                TenantId = tenantState.TenantId.ToString(),
                TenantName = tenantState.Tenant?.Name ?? "",
                Status = tenantState.Status,
                LastHealthCheck = tenantState.LastHealthCheck,
                RequiresAttention = tenantState.RequiresAttention
            };
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByTenantIdAsync(Guid tenantId)
        {
            var tenantState = await _context.TenantStates
                .Include(ts => ts.Tenant)
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState == null)
                return null;

            return new TenantStateDto
            {
                Id = tenantState.Id.ToString(),
                TenantId = tenantState.TenantId.ToString(),
                TenantName = tenantState.Tenant?.Name ?? "",
                Status = tenantState.Status,
                LastHealthCheck = tenantState.LastHealthCheck,
                RequiresAttention = tenantState.RequiresAttention
            };
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> UpdateTenantStateAsync(Guid tenantId, UpdateTenantStateRequest request, string updatedBy)
        {
            var tenantState = await _context.TenantStates
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState == null)
                throw new ArgumentException($"Tenant state not found for tenant {tenantId}");

            tenantState.Status = request.Status;
            tenantState.LastHealthCheck = DateTime.UtcNow;
            tenantState.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TenantStateDto
            {
                Id = tenantState.Id.ToString(),
                TenantId = tenantState.TenantId.ToString(),
                TenantName = tenantState.Tenant?.Name ?? "",
                Status = tenantState.Status,
                LastHealthCheck = tenantState.LastHealthCheck,
                RequiresAttention = tenantState.RequiresAttention
            };
        }

        public async System.Threading.Tasks.Task RefreshTenantStateAsync(Guid tenantId)
        {
            var tenantState = await _context.TenantStates
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState != null)
            {
                tenantState.LastHealthCheck = DateTime.UtcNow;
                tenantState.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Refreshed tenant state for tenant {TenantId}", tenantId);
            }
        }

        public async System.Threading.Tasks.Task RefreshAllTenantStatesAsync()
        {
            var tenantStates = await _context.TenantStates.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var tenantState in tenantStates)
            {
                tenantState.LastHealthCheck = now;
                tenantState.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Refreshed all tenant states ({Count} tenants)", tenantStates.Count);
        }

        public async System.Threading.Tasks.Task<List<TenantStateDto>> GetTenantsRequiringAttentionAsync()
        {
            var tenantStates = await _context.TenantStates
                .Include(ts => ts.Tenant)
                .Where(ts => ts.RequiresAttention && ts.TenantId == _tenantProvider.GetTenantId())
                .Select(ts => new TenantStateDto
                {
                    Id = ts.Id.ToString(),
                    TenantId = ts.TenantId.ToString(),
                    TenantName = ts.Tenant.Name,
                    Status = ts.Status,
                    LastHealthCheck = ts.LastHealthCheck,
                    RequiresAttention = ts.RequiresAttention
                })
                .ToListAsync();

            return tenantStates;
        }

        public async System.Threading.Tasks.Task<List<TenantStateDto>> GetUnhealthyTenantsAsync()
        {
            var tenantStates = await _context.TenantStates
                .Include(ts => ts.Tenant)
                .Where(ts => (ts.Status == "Unhealthy" || ts.Status == "Critical") && ts.TenantId == _tenantProvider.GetTenantId())
                .Select(ts => new TenantStateDto
                {
                    Id = ts.Id.ToString(),
                    TenantId = ts.TenantId.ToString(),
                    TenantName = ts.Tenant.Name,
                    Status = ts.Status,
                    LastHealthCheck = ts.LastHealthCheck,
                    RequiresAttention = ts.RequiresAttention
                })
                .ToListAsync();

            return tenantStates;
        }

        public async System.Threading.Tasks.Task<Dictionary<string, object>> GetTenantStatsSummaryAsync()
        {
            var tenantId = _tenantProvider.GetTenantId();
            var totalTenants = await _context.TenantStates.CountAsync(ts => ts.TenantId == tenantId);
            var healthyTenants = await _context.TenantStates.CountAsync(ts => ts.Status == "Healthy" && ts.TenantId == tenantId);
            var unhealthyTenants = await _context.TenantStates.CountAsync(ts => ts.Status == "Unhealthy" && ts.TenantId == tenantId);
            var criticalTenants = await _context.TenantStates.CountAsync(ts => ts.Status == "Critical" && ts.TenantId == tenantId);
            var attentionRequired = await _context.TenantStates.CountAsync(ts => ts.RequiresAttention && ts.TenantId == tenantId);

            return new Dictionary<string, object>
            {
                ["totalTenants"] = totalTenants,
                ["healthyTenants"] = healthyTenants,
                ["unhealthyTenants"] = unhealthyTenants,
                ["criticalTenants"] = criticalTenants,
                ["attentionRequired"] = attentionRequired,
                ["healthPercentage"] = totalTenants > 0 ? (double)healthyTenants / totalTenants * 100 : 0
            };
        }

        public async System.Threading.Tasks.Task UpdateTenantUsageStatsAsync(Guid tenantId)
        {
            var tenantState = await _context.TenantStates
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState != null)
            {
                tenantState.LastHealthCheck = DateTime.UtcNow;
                tenantState.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated usage stats for tenant {TenantId}", tenantId);
            }
        }

        public async System.Threading.Tasks.Task MarkTenantForAttentionAsync(Guid tenantId, string reason, string markedBy)
        {
            var tenantState = await _context.TenantStates
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState != null)
            {
                tenantState.RequiresAttention = true;
                tenantState.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogWarning("Tenant {TenantId} marked for attention by {MarkedBy}: {Reason}", tenantId, markedBy, reason);
            }
        }

        public async System.Threading.Tasks.Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy)
        {
            var tenantState = await _context.TenantStates
                .Where(ts => ts.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantState != null)
            {
                tenantState.RequiresAttention = false;
                tenantState.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Tenant {TenantId} attention cleared by {ClearedBy}", tenantId, clearedBy);
            }
        }
    }
}
