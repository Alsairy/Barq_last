using BARQ.Core.Services;

namespace BARQ.IntegrationTests.Mocks;

public class MockTenantProvider : ITenantProvider
{
    private Guid _tenantId;
    private string _tenantName;

    public MockTenantProvider(Guid? tenantId = null, string? tenantName = null)
    {
        _tenantId = tenantId ?? Guid.NewGuid();
        _tenantName = tenantName ?? "Test Tenant";
    }

    public Guid GetTenantId()
    {
        return _tenantId;
    }

    public string GetTenantName()
    {
        return _tenantName;
    }

    public void SetTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public void ClearTenantContext()
    {
        _tenantId = Guid.Empty;
    }

    public Guid GetCurrentUserId()
    {
        return Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
    }
}
