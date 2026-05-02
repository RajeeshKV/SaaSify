using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Common.Interfaces;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICacheService, InMemoryCacheService>();
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddMemoryCache();
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>()
            .AddCheck<TenantHealthCheck>("tenant-context")
            .AddCheck<CacheHealthCheck>("cache");

        return services;
    }
}