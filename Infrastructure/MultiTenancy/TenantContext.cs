public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }

    public void SetTenantId(int tenantId)
    {
        TenantId = tenantId;
    }
}