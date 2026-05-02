using Microsoft.Extensions.Caching.Memory;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ITenantContext _tenantContext;

    public InMemoryCacheService(IMemoryCache cache, ITenantContext tenantContext)
    {
        _cache = cache;
        _tenantContext = tenantContext;
    }

    private string GetTenantKey(string key)
    {
        return $"tenant:{_tenantContext.TenantId}:{key}";
    }

    public Task<T> GetAsync<T>(string key)
    {
        var tenantKey = GetTenantKey(key);
        _cache.TryGetValue(tenantKey, out T value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var tenantKey = GetTenantKey(key);
        var cacheOptions = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        }

        _cache.Set(tenantKey, value, cacheOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        var tenantKey = GetTenantKey(key);
        _cache.Remove(tenantKey);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        // InMemoryCache doesn't support pattern removal
        // This would require a custom implementation or use of StackExchange.Redis
        return Task.CompletedTask;
    }
}
