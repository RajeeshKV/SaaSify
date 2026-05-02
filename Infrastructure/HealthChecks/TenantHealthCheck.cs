using Microsoft.Extensions.Diagnostics.HealthChecks;

public class TenantHealthCheck : IHealthCheck
{
    private readonly ITenantContext _tenantContext;

    public TenantHealthCheck(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_tenantContext.TenantId > 0)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy($"Tenant context available: {_tenantContext.TenantId}"));
            }
            
            return Task.FromResult(
                HealthCheckResult.Degraded("Tenant context not set or invalid"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Tenant context check failed", ex));
        }
    }
}
