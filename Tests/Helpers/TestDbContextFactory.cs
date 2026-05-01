using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Tests.Helpers;

public class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new MockTenantContext();
        var context = new ApplicationDbContext(options, mockTenantContext);

        return context;
    }

    public static ApplicationDbContext CreateInMemoryDbContextWithData(int tenantId = 1)
    {
        var context = CreateInMemoryDbContext();
        
        // Seed test data
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Id = 1,
            TenantId = tenantId,
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            Role = "Admin",
            Tenant = tenant
        };
        context.Users.Add(user);

        var project = new Project
        {
            Id = 1,
            TenantId = tenantId,
            Name = "Test Project",
            Tenant = tenant
        };
        context.Projects.Add(project);

        context.SaveChanges();
        return context;
    }
}

public class MockTenantContext : ITenantContext
{
    public int TenantId { get; private set; } = 1;

    public void SetTenantId(int tenantId)
    {
        TenantId = tenantId;
    }
}
