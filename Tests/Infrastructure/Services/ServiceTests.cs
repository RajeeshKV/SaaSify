using Microsoft.Extensions.Caching.Memory;
using Xunit;
using FluentAssertions;
using Moq;

namespace Tests.Infrastructure.Services;

public class InMemoryCacheServiceTests
{
    [Fact]
    public async Task SetAsync_WithValidData_StoresValue()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);
        var key = "test-key";
        var value = "test-value";

        // Act
        await cacheService.SetAsync(key, value);

        // Assert
        var cached = await cacheService.GetAsync<string>(key);
        cached.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);

        // Act
        var result = await cacheService.GetAsync<string>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);
        var key = "expiring-key";
        var value = "expiring-value";

        // Act
        await cacheService.SetAsync(key, value, TimeSpan.FromMilliseconds(100));
        await Task.Delay(150); // Wait for expiration

        // Assert
        var cached = await cacheService.GetAsync<string>(key);
        cached.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_RemovesValue()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);
        var key = "remove-key";
        await cacheService.SetAsync(key, "value");

        // Act
        await cacheService.RemoveAsync(key);

        // Assert
        var cached = await cacheService.GetAsync<string>(key);
        cached.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_DoesNotRemoveExistingValues()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);
        await cacheService.SetAsync("projects:1", "value");

        // Act
        await cacheService.RemoveByPatternAsync("projects:*");

        // Assert
        var cached = await cacheService.GetAsync<string>("projects:1");
        cached.Should().Be("value");
    }

    [Fact]
    public async Task GetAsync_WithComplexType_ReturnsCachedObject()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(1);
        var cacheService = new InMemoryCacheService(memoryCache, mockTenantContext.Object);
        var key = "object-key";
        var value = new { Id = 1, Name = "Test" };

        // Act
        await cacheService.SetAsync(key, value);

        // Assert
        var cached = await cacheService.GetAsync<object>(key);
        cached.Should().NotBeNull();
    }
}

public class CorrelationIdGeneratorTests
{
    [Fact]
    public void GenerateCorrelationId_ReturnsGuid()
    {
        // Arrange
        var generator = new CorrelationIdGenerator();

        // Act
        var correlationId = generator.GenerateCorrelationId();

        // Assert
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateCorrelationId_ReturnsDifferentGuids()
    {
        // Arrange
        var generator = new CorrelationIdGenerator();

        // Act
        var id1 = generator.GenerateCorrelationId();
        var id2 = generator.GenerateCorrelationId();

        // Assert
        id1.Should().NotBe(id2);
    }
}
