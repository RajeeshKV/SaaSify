using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ITenantContext tenantContext, ITenantContextService tenantContextService)
    {
        if (ShouldSkipTenantResolution(context.Request.Path) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        int tenantId = 0;

        // Try to get tenant from header first (for API testing)
        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantHeader) && int.TryParse(tenantHeader, out var headerId))
        {
            tenantId = headerId;
        }

        // Fall back to JWT token for authenticated users
        if (tenantId == 0 && context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("TenantId");
            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var claimId))
            {
                tenantId = claimId;
            }
        }

        // Skip tenant validation for Stripe webhook callbacks
        var isStripeCallback = context.Request.Path.StartsWithSegments("/api/stripe/success") || 
                              context.Request.Path.StartsWithSegments("/api/stripe/cancel") ||
                              context.Request.Path.StartsWithSegments("/api/stripe/webhook");
        
        if (tenantId == 0 && !isStripeCallback)
        {
            _logger.LogWarning("TenantId is required for {Path}", context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { 
                message = "TenantId is required",
                code = "TENANT_REQUIRED"
            });
            return;
        }

        // For Stripe callbacks, we don't need tenant context
        if (isStripeCallback)
        {
            _logger.LogDebug("Skipping tenant validation for Stripe callback: {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        // Validate tenant exists and is active
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found for {Path}", tenantId, context.Request.Path);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { 
                message = "Tenant not found",
                code = "TENANT_NOT_FOUND"
            });
            return;
        }

        if (!tenant.IsActive)
        {
            _logger.LogWarning("Tenant {TenantId} is inactive for {Path}", tenantId, context.Request.Path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { 
                message = "Tenant account is inactive",
                code = "TENANT_INACTIVE"
            });
            return;
        }

        // Set tenant context for both the context service and the existing tenant context
        await tenantContextService.SetTenantContextAsync(tenantId);
        tenantContext.SetTenantId(tenantId);

        _logger.LogDebug("Tenant {TenantId} ({TenantName}) set for {Path}", tenantId, tenant.Name, context.Request.Path);

        await _next(context);
    }

    private static bool ShouldSkipTenantResolution(PathString path)
    {
        return path == PathString.Empty
            || path == "/"
            || path.StartsWithSegments("/api/auth")
            || path.StartsWithSegments("/health")
            || path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/api/health");
    }
}
