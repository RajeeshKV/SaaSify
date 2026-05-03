using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Xunit;

namespace Tests.Infrastructure;

public class ApplicationDbContextTests
{
    [Fact]
    public async Task Projects_AppliesTenantQueryFilter()
    {
        var tenantContext = new MockTenantContext();
        tenantContext.SetTenantId(1);
        await using var context = CreateContext(tenantContext);
        await SeedMultiTenantData(context);

        var projects = await context.Projects.ToListAsync();

        projects.Should().ContainSingle();
        projects.Single().TenantId.Should().Be(1);
        projects.Single().Name.Should().Be("Tenant 1 Project");
    }

    [Fact]
    public async Task Users_AppliesTenantQueryFilter()
    {
        var tenantContext = new MockTenantContext();
        tenantContext.SetTenantId(2);
        await using var context = CreateContext(tenantContext);
        await SeedMultiTenantData(context);

        var users = await context.Users.ToListAsync();

        users.Should().ContainSingle();
        users.Single().TenantId.Should().Be(2);
        users.Single().Email.Should().Be("tenant2@example.com");
    }

    [Fact]
    public async Task ChangingTenantContext_ChangesFilteredResults()
    {
        var tenantContext = new MockTenantContext();
        tenantContext.SetTenantId(1);
        await using var context = CreateContext(tenantContext);
        await SeedMultiTenantData(context);

        var tenantOneProjects = await context.Projects.Select(p => p.Name).ToListAsync();
        tenantContext.SetTenantId(2);
        var tenantTwoProjects = await context.Projects.Select(p => p.Name).ToListAsync();

        tenantOneProjects.Should().ContainSingle().Which.Should().Be("Tenant 1 Project");
        tenantTwoProjects.Should().ContainSingle().Which.Should().Be("Tenant 2 Project");
    }

    private static ApplicationDbContext CreateContext(MockTenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantContext);
    }

    private static async Task SeedMultiTenantData(ApplicationDbContext context)
    {
        var tenant1 = new Tenant { Id = 1, Name = "Tenant 1" };
        var tenant2 = new Tenant { Id = 2, Name = "Tenant 2" };

        context.Tenants.AddRange(tenant1, tenant2);
        context.Projects.AddRange(
            new Project { Id = 1, TenantId = 1, Name = "Tenant 1 Project", Tenant = tenant1 },
            new Project { Id = 2, TenantId = 2, Name = "Tenant 2 Project", Tenant = tenant2 });
        context.Users.AddRange(
            new User { Id = 1, TenantId = 1, Email = "tenant1@example.com", PasswordHash = "hash", Role = "Admin", Tenant = tenant1 },
            new User { Id = 2, TenantId = 2, Email = "tenant2@example.com", PasswordHash = "hash", Role = "Member", Tenant = tenant2 });
        await context.SaveChangesAsync();
    }
}
