using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddInfrastructure(configuration);

        services.Should().Contain(s => s.ServiceType == typeof(ApplicationDbContext));
        services.Should().Contain(s => s.ServiceType == typeof(ITenantContext)
            && s.ImplementationType == typeof(TenantContext)
            && s.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(s => s.ServiceType == typeof(IUnitOfWork)
            && s.ImplementationType == typeof(UnitOfWork)
            && s.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(s => s.ServiceType == typeof(IRepository<>)
            && s.ImplementationType == typeof(Repository<>)
            && s.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(s => s.ServiceType == typeof(ICacheService)
            && s.ImplementationType == typeof(InMemoryCacheService)
            && s.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(s => s.ServiceType == typeof(ICorrelationIdGenerator)
            && s.ImplementationType == typeof(CorrelationIdGenerator)
            && s.Lifetime == ServiceLifetime.Singleton);
    }
}
