using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Application.Common.Interfaces;
using Application.Common.Configuration;
using Infrastructure.Interceptors;
using Infrastructure.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                {
                    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                        ?? configuration.GetConnectionString("DefaultConnection");
                    
                    options.UseNpgsql(
                        connectionString,
                        b => b.MigrationsAssembly("Infrastructure"))
                        .UseSnakeCaseNamingConvention();
                    
                    // Add tenant interceptor for production RLS
                    options.AddInterceptors(serviceProvider.GetRequiredService<TenantDbInterceptor>());
                });

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddTransient<TenantDbInterceptor>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ITenantSettingsService, TenantSettingsService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<RBACMigrationService>();
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

        // Configure RBAC migration settings
        var rbacConfig = new RBACMigrationConfiguration();
        var rbacSection = configuration.GetSection("RBACMigration");
        
        configuration.Bind("RBACMigration", rbacConfig);
        services.AddSingleton(rbacConfig);
        
        // Add Order Event Publisher
        services.AddSingleton<IOrderEventPublisher, OrderEventPublisher>();

        return services;
    }
}