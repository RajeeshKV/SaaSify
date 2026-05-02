using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface ITenantContextService
{
    Task SetTenantContextAsync(int tenantId);
    Task<int?> GetCurrentTenantIdAsync();
}

public class TenantContextService : ITenantContextService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public TenantContextService(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task SetTenantContextAsync(int tenantId)
    {
        _tenantContext.SetTenantId(tenantId);
        
        // Set PostgreSQL session variable for RLS
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.tenant_id', {0}, true)",
            tenantId);
    }

    public async Task<int?> GetCurrentTenantIdAsync()
    {
        return _tenantContext.TenantId;
    }
}
