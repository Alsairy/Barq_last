using BARQ.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BARQ.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid _currentTenantId = Guid.Empty;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        if (_currentTenantId != Guid.Empty)
        {
            return _currentTenantId;
        }

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
        _currentTenantId = tenantId;
    }

    public string? GetTenantName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("TenantName")?.Value;
        }
        
        return null;
    }
}
