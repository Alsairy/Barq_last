using BARQ.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BARQ.Infrastructure.Services;

public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _ctx;
    private Guid _tenantId;

    public TenantProvider(IHttpContextAccessor ctx) => _ctx = ctx;

    public Guid GetTenantId()
    {
        if (_tenantId != Guid.Empty) return _tenantId;
        var http = _ctx.HttpContext;

        if (http?.Request.Headers.TryGetValue("X-Tenant-Id", out var h) == true &&
            Guid.TryParse(h.FirstOrDefault(), out var id)) { _tenantId = id; return _tenantId; }

        var claim = http?.User?.FindFirst("TenantId")?.Value ?? http?.User?.FindFirst("tid")?.Value;
        if (Guid.TryParse(claim, out var cid)) { _tenantId = cid; return _tenantId; }

        return Guid.Empty; // safe default: no tenant context
    }

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;
    public string GetTenantName() => "Default Tenant"; // optional
    public void ClearTenantContext() => _tenantId = Guid.Empty;
    public Guid GetCurrentUserId()
        => Guid.TryParse(_ctx.HttpContext?.User?.FindFirst("sub")?.Value ?? "", out var uid) ? uid : Guid.Empty;
}
