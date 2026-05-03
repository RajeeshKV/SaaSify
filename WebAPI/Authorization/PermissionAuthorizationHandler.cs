using Microsoft.AspNetCore.Authorization;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return;
            }

            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(tenantIdClaim, out var tenantId))
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if user has the required permission through their roles
            var hasPermission = await dbContext.UserRoles
                .IgnoreQueryFilters()
                .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == requirement.Permission && rp.TenantId == tenantId);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
