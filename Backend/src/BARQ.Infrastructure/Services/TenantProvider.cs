using BARQ.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BARQ.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = user.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }
        }
        
        return Guid.Empty;
    }

    public void SetTenantId(Guid tenantId)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items["TenantId"] = tenantId;
        }
    }

    public string GetTenantName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantNameClaim = user.FindFirst("TenantName")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantNameClaim))
            {
                return tenantNameClaim;
            }
        }
        
        return "Unknown";
    }
}
