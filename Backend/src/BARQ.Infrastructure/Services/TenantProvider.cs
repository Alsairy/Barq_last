using BARQ.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BARQ.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private static readonly System.Threading.AsyncLocal<Guid?> _overrideTenant = new();

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var overrideId = _overrideTenant.Value;
        if (overrideId.HasValue && overrideId.Value != Guid.Empty)
        {
            return overrideId.Value;
        }

        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = user.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }
        }

        var hdr = http?.Request.Headers["X-Tenant-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(hdr) && Guid.TryParse(hdr, out var fromHeader))
        {
            return fromHeader;
        }

        return Guid.Empty;
    }

    public void SetTenantId(Guid tenantId) => _overrideTenant.Value = tenantId;

    public string GetTenantName()
    {
        return "System";
    }
}
