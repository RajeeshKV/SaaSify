using Application.Common.Interfaces;
using System.Text.Json;

namespace WebAPI.Middleware
{
    public class TenantLifecycleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantLifecycleMiddleware> _logger;

        public TenantLifecycleMiddleware(RequestDelegate next, ILogger<TenantLifecycleMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContextService tenantContextService)
        {
            // Skip tenant validation for auth endpoints and health checks
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (path.StartsWith("/api/auth") || path.StartsWith("/health")))
            {
                await _next(context);
                return;
            }

            // Get tenant from JWT token
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
            
            if (int.TryParse(tenantIdClaim, out var tenantId) && tenantId > 0)
            {
                // Set tenant context
                await tenantContextService.SetTenantContextAsync(tenantId);

                // Get current tenant info to validate
                var currentTenantId = await tenantContextService.GetCurrentTenantIdAsync();
                
                // Additional tenant validation could be added here
                // For now, we rely on the database RLS policies
                _logger.LogDebug("Tenant {TenantId} context set for request {RequestPath}", tenantId, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("No valid tenant ID found in JWT token for request {RequestPath}", context.Request.Path);
            }

            await _next(context);
        }
    }
}
