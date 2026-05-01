using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Tests.Domain;

public class EntityTests
{
    [Fact]
    public void Tenant_CanStoreBasicProperties()
    {
        var tenant = new Tenant { Id = 7, Name = "Acme" };

        tenant.Id.Should().Be(7);
        tenant.Name.Should().Be("Acme");
    }

    [Fact]
    public void User_CanStoreTenantRelationshipAndCredentials()
    {
        var tenant = new Tenant { Id = 2, Name = "Tenant 2" };
        var user = new User
        {
            Id = 3,
            TenantId = tenant.Id,
            Email = "admin@example.com",
            PasswordHash = "hashed",
            Role = "Admin",
            Tenant = tenant
        };

        user.Id.Should().Be(3);
        user.TenantId.Should().Be(2);
        user.Email.Should().Be("admin@example.com");
        user.PasswordHash.Should().Be("hashed");
        user.Role.Should().Be("Admin");
        user.Tenant.Should().BeSameAs(tenant);
    }

    [Fact]
    public void Project_CanStoreTenantRelationship()
    {
        var tenant = new Tenant { Id = 4, Name = "Tenant 4" };
        var project = new Project
        {
            Id = 12,
            TenantId = tenant.Id,
            Name = "Migration",
            Tenant = tenant
        };

        project.Id.Should().Be(12);
        project.TenantId.Should().Be(4);
        project.Name.Should().Be("Migration");
        project.Tenant.Should().BeSameAs(tenant);
    }
}
