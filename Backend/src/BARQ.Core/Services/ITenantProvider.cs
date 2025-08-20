namespace BARQ.Core.Services
{
    public interface ITenantProvider
    {
        Guid GetTenantId();
        void SetTenantId(Guid tenantId);
        string? GetTenantName();
    }
}
