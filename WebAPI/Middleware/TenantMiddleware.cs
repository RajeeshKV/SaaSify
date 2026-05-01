using System.Security.Claims;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ITenantContext tenantContext)
    {
        if (HttpMethods.IsOptions(context.Request.Method) || ShouldSkipTenantResolution(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Get tenant from header first
        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        int tenantId = 0;

        if (!string.IsNullOrEmpty(tenantHeader) && int.TryParse(tenantHeader, out var headerId))
        {
            tenantId = headerId;
        }
        else
        {
            // Try to get from JWT claims
            var tenantClaim = context.User?.FindFirst("TenantId");
            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var claimId))
            {
                tenantId = claimId;
            }
        }

        if (tenantId == 0)
        {
            _logger.LogWarning("TenantId is required");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = "TenantId is required" });
            return;
        }

        tenantContext.SetTenantId(tenantId);
        _logger.LogInformation("Tenant {TenantId} set for request {Path}", tenantId, context.Request.Path);

        await _next(context);
    }

    private static bool ShouldSkipTenantResolution(PathString path)
    {
        return path == PathString.Empty
            || path == "/"
            || path.StartsWithSegments("/api/auth")
            || path.StartsWithSegments("/api/health")
            || path.StartsWithSegments("/swagger");
    }
}
