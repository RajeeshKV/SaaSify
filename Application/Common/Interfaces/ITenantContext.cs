public interface ITenantContext
{
    int TenantId { get; }
    void SetTenantId(int tenantId);
}