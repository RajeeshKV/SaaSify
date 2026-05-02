using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class CacheHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;
    private readonly ICacheService _cacheService;

    public CacheHealthCheck(IMemoryCache cache, ICacheService cacheService)
    {
        _cache = cache;
        _cacheService = cacheService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache functionality
            var testKey = "health-check-test";
            var testValue = DateTime.UtcNow;
            
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrievedValue = await _cacheService.GetAsync<DateTime>(testKey);
            
            if (retrievedValue == testValue)
            {
                await _cacheService.RemoveAsync(testKey);
                return HealthCheckResult.Healthy("Cache service is working correctly");
            }
            
            return HealthCheckResult.Degraded("Cache service not responding correctly");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache service check failed", ex);
        }
    }
}
