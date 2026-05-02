using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Application.Common.Interfaces;
using Application.Common.Configuration;

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

        // Configure subscription settings
        var subscriptionConfig = new SubscriptionConfiguration();
        var plansSection = configuration.GetSection("Subscription:Plans");
        
        foreach (var planSection in plansSection.GetChildren())
        {
            var plan = new PlanConfiguration
            {
                Name = planSection["Name"] ?? "",
                Description = planSection["Description"] ?? "",
                MonthlyPrice = decimal.Parse(planSection["MonthlyPrice"] ?? "0"),
                RateLimitPerMinute = int.Parse(planSection["RateLimitPerMinute"] ?? "100"),
                MaxUsers = int.Parse(planSection["MaxUsers"] ?? "3"),
                Features = planSection.GetSection("Features").GetChildren().Select(f => f.Value ?? "").ToList()
            };
            subscriptionConfig.Plans.Add(plan);
        }
        
        services.AddSingleton(subscriptionConfig);

        return services;
    }
}