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
        
        // For now, skip PostgreSQL session variable setting to avoid compatibility issues
        // The tenant filtering is handled by Entity Framework global query filters
        // PostgreSQL RLS can be enabled later when database is properly configured
        
        // TODO: Enable PostgreSQL session variables when database supports set_config function
        // await _context.Database.ExecuteSqlRawAsync(
        //     "SET app.tenant_id = {0}",
        //     tenantId);
    }

    public async Task<int?> GetCurrentTenantIdAsync()
    {
        return _tenantContext.TenantId;
    }
}
