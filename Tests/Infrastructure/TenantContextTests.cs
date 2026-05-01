using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure;

public class TenantContextTests
{
    [Fact]
    public void TenantId_DefaultsToZero()
    {
        var tenantContext = new TenantContext();

        tenantContext.TenantId.Should().Be(0);
    }

    [Fact]
    public void SetTenantId_UpdatesCurrentTenant()
    {
        var tenantContext = new TenantContext();

        tenantContext.SetTenantId(42);

        tenantContext.TenantId.Should().Be(42);
    }
}
