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
        if (ShouldSkipTenantResolution(context.Request.Path) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        int tenantId = 0;

        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(tenantHeader) && int.TryParse(tenantHeader, out var headerId))
        {
            tenantId = headerId;
        }

        if (tenantId == 0 && context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("TenantId");

            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var claimId))
            {
                tenantId = claimId;
            }
        }

        if (tenantId == 0)
        {
            _logger.LogWarning("TenantId is required for {Path}", context.Request.Path);

            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = "TenantId is required" });
            return;
        }

        tenantContext.SetTenantId(tenantId);

        _logger.LogInformation("Tenant {TenantId} set for {Path}", tenantId, context.Request.Path);

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
