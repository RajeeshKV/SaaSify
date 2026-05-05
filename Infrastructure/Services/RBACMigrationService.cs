using Application.Common.Configuration;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class RBACMigrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly RBACMigrationConfiguration _config;
        private readonly ILogger<RBACMigrationService> _logger;

        public RBACMigrationService(
            ApplicationDbContext context,
            RBACMigrationConfiguration config,
            ILogger<RBACMigrationService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> MigrateExistingTenantsAsync()
        {
            try
            {
                _logger.LogInformation("Starting RBAC migration for existing tenants");

                var tenants = await _context.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.IsActive)
                    .ToListAsync();

                foreach (var tenant in tenants)
                {
                    await MigrateTenantAsync(tenant);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("RBAC migration completed for {Count} tenants", tenants.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RBAC migration failed");
                return false;
            }
        }

        private async Task MigrateTenantAsync(Tenant tenant)
        {
            _logger.LogDebug("Migrating RBAC for tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);

            // Create default permissions
            await CreatePermissionsAsync(tenant);

            // Create default roles
            await CreateRolesAsync(tenant);

            // Assign permissions to roles
            await AssignPermissionsToRolesAsync(tenant);

            // Assign users to roles
            await AssignUsersToRolesAsync(tenant);

            // Create audit log
            await CreateAuditLogAsync(tenant);
        }

        private async Task CreatePermissionsAsync(Tenant tenant)
        {
            foreach (var permissionConfig in _config.DefaultPermissions)
            {
                var existingPermission = await _context.Permissions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.Name == permissionConfig.Name);

                if (existingPermission == null)
                {
                    var permission = new Permission
                    {
                        TenantId = tenant.Id,
                        Name = permissionConfig.Name,
                        Description = permissionConfig.Description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Permissions.Add(permission);
                    _logger.LogDebug("Created permission {Permission} for tenant {TenantId}", permissionConfig.Name, tenant.Id);
                }
            }
        }

        private async Task CreateRolesAsync(Tenant tenant)
        {
            foreach (var roleConfig in _config.DefaultRoles)
            {
                var existingRole = await _context.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == roleConfig.Name);

                if (existingRole == null)
                {
                    var role = new Role
                    {
                        TenantId = tenant.Id,
                        Name = roleConfig.Name,
                        Description = roleConfig.Description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Roles.Add(role);
                    _logger.LogDebug("Created role {Role} for tenant {TenantId}", roleConfig.Name, tenant.Id);
                }
            }
        }

        private async Task AssignPermissionsToRolesAsync(Tenant tenant)
        {
            foreach (var mapping in _config.RolePermissionMappings)
            {
                var role = await _context.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == mapping.Role);

                if (role == null) continue;

                foreach (var permissionName in mapping.Permissions)
                {
                    var permission = await _context.Permissions
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.Name == permissionName);

                    if (permission == null) continue;

                    var existingAssignment = await _context.RolePermissions
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(rp => rp.TenantId == tenant.Id && rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                    if (existingAssignment == null)
                    {
                        var rolePermission = new RolePermission
                        {
                            TenantId = tenant.Id,
                            RoleId = role.Id,
                            PermissionId = permission.Id,
                            GrantedAt = DateTime.UtcNow
                        };

                        _context.RolePermissions.Add(rolePermission);
                        _logger.LogDebug("Assigned permission {Permission} to role {Role} for tenant {TenantId}", permissionName, mapping.Role, tenant.Id);
                    }
                }
            }
        }

        private async Task AssignUsersToRolesAsync(Tenant tenant)
        {
            var users = await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenant.Id)
                .ToListAsync();

            foreach (var user in users)
            {
                // Check if user already has a role assignment
                var existingAssignment = await _context.UserRoles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(ur => ur.TenantId == tenant.Id && ur.UserId == user.Id);

                if (existingAssignment != null) continue;

                // Find the best role mapping based on user's current role
                var bestMapping = _config.UserRoleMappings
                    .Where(m => m.UserRole == user.Role)
                    .OrderByDescending(m => m.Priority)
                    .FirstOrDefault();

                // If no specific mapping, use default role
                var targetRoleName = bestMapping?.AssignedRole ?? 
                    _config.DefaultRoles.FirstOrDefault(r => r.IsDefault)?.Name ?? "User";

                var targetRole = await _context.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == targetRoleName);

                if (targetRole != null)
                {
                    var userRole = new UserRole
                    {
                        TenantId = tenant.Id,
                        UserId = user.Id,
                        RoleId = targetRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    _logger.LogDebug("Assigned user {UserId} to role {Role} for tenant {TenantId}", user.Id, targetRoleName, tenant.Id);
                }
            }
        }

        private async Task CreateAuditLogAsync(Tenant tenant)
        {
            var auditLog = new AuditLog
            {
                TenantId = tenant.Id,
                UserId = 1, // System migration - using admin user ID
                Action = "RBAC_MIGRATION",
                Data = $"RBAC migration completed for tenant {tenant.Name}",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasBeenMigratedAsync(int tenantId)
        {
            var auditLog = await _context.AuditLogs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(al => al.TenantId == tenantId && al.Action == "RBAC_MIGRATION");

            return auditLog != null;
        }

        public async Task<List<string>> GetMigrationStatusAsync()
        {
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.IsActive)
                .ToListAsync();

            var status = new List<string>();

            foreach (var tenant in tenants)
            {
                var hasBeenMigrated = await HasBeenMigratedAsync(tenant.Id);
                status.Add($"Tenant {tenant.Name} (ID: {tenant.Id}): {(hasBeenMigrated ? "Migrated" : "Not Migrated")}");
            }

            return status;
        }
    }
}
