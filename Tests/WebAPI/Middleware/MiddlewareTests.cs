using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Security.Claims;

namespace Tests.WebAPI.Middleware;

public class CorrelationMiddlewareTests
{
    [Fact]
    public async Task Invoke_WithoutCorrelationId_GeneratesNew()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var middleware = new CorrelationMiddleware(mockNext.Object);
        var mockGenerator = new Mock<ICorrelationIdGenerator>();
        var testCorrelationId = "test-correlation-123";
        mockGenerator.Setup(g => g.GenerateCorrelationId()).Returns(testCorrelationId);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "";

        // Act
        await middleware.Invoke(httpContext, mockGenerator.Object);

        // Assert
        httpContext.Items.Should().ContainKey("CorrelationId");
        httpContext.Response.Headers.Should().ContainKey("X-Correlation-Id");
        mockGenerator.Verify(g => g.GenerateCorrelationId(), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithExistingCorrelationId_UsesProvided()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var middleware = new CorrelationMiddleware(mockNext.Object);
        var mockGenerator = new Mock<ICorrelationIdGenerator>();
        var providedCorrelationId = "provided-correlation-456";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = providedCorrelationId;

        // Act
        await middleware.Invoke(httpContext, mockGenerator.Object);

        // Assert
        httpContext.Items["CorrelationId"].Should().Be(providedCorrelationId);
        httpContext.Response.Headers["X-Correlation-Id"].Should().Contain(providedCorrelationId);
        mockGenerator.Verify(g => g.GenerateCorrelationId(), Times.Never);
    }

    [Fact]
    public async Task Invoke_CallsNext()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var middleware = new CorrelationMiddleware(mockNext.Object);
        var mockGenerator = new Mock<ICorrelationIdGenerator>();

        var httpContext = new DefaultHttpContext();

        // Act
        await middleware.Invoke(httpContext, mockGenerator.Object);

        // Assert
        mockNext.Verify(n => n(httpContext), Times.Once);
    }
}

public class TenantMiddlewareTests
{
    [Fact]
    public async Task Invoke_WithValidTenantHeader_SetsTenantId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(mockNext.Object, mockLogger.Object);
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTenantContextService = new Mock<ITenantContextService>();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "1";

        // Act
        await middleware.Invoke(httpContext, mockTenantContext.Object, mockTenantContextService.Object);

        // Assert
        mockTenantContext.Verify(tc => tc.SetTenantId(1), Times.Once);
        mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithoutTenantId_Returns400()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(mockNext.Object, mockLogger.Object);
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTenantContextService = new Mock<ITenantContextService>();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "";

        // Act
        await middleware.Invoke(httpContext, mockTenantContext.Object, mockTenantContextService.Object);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
        mockNext.Verify(n => n(httpContext), Times.Never);
    }

    [Fact]
    public async Task Invoke_WithInvalidTenantId_Returns400()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(mockNext.Object, mockLogger.Object);
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTenantContextService = new Mock<ITenantContextService>();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "invalid";

        // Act
        await middleware.Invoke(httpContext, mockTenantContext.Object, mockTenantContextService.Object);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_WithTenantClaim_SetsTenantId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(mockNext.Object, mockLogger.Object);
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTenantContextService = new Mock<ITenantContextService>();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("TenantId", "12")
            }))
        };

        // Act
        await middleware.Invoke(httpContext, mockTenantContext.Object, mockTenantContextService.Object);

        // Assert
        mockTenantContext.Verify(tc => tc.SetTenantId(12), Times.Once);
        mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithHeaderAndClaim_PrefersHeaderTenantId()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(mockNext.Object, mockLogger.Object);
        var mockTenantContext = new Mock<ITenantContext>();
        var mockTenantContextService = new Mock<ITenantContextService>();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("TenantId", "12")
            }))
        };
        httpContext.Request.Headers["X-Tenant-Id"] = "34";

        // Act
        await middleware.Invoke(httpContext, mockTenantContext.Object, mockTenantContextService.Object);

        // Assert
        mockTenantContext.Verify(tc => tc.SetTenantId(34), Times.Once);
    }
}

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task Invoke_WithNoException_CallsNext()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var middleware = new ExceptionMiddleware(mockNext.Object);

        var httpContext = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithArgumentNullException_Returns400()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Throws(new ArgumentNullException("testParam"));

        var middleware = new ExceptionMiddleware(mockNext.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Invoke_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        var middleware = new ExceptionMiddleware(mockNext.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Invoke_WithGenericException_Returns500()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(n => n(It.IsAny<HttpContext>()))
            .Throws(new InvalidOperationException("Something went wrong"));

        var middleware = new ExceptionMiddleware(mockNext.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(500);
    }
}
