using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAccessPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Create all required permissions for all active tenants
                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'tenant.access',
                    'Basic tenant access',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'tenant.access'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'project.read',
                    'Read and view projects',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'project.read'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'project.write',
                    'Create and edit projects',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'project.write'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'project.delete',
                    'Delete projects',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'project.delete'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'user.manage',
                    'Manage users in tenant',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'user.manage'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'subscription.manage',
                    'Manage subscription and billing',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'subscription.manage'
                );

                INSERT INTO permissions (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'tenant.admin',
                    'Full tenant administration',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM permissions p 
                    WHERE p.tenant_id = t.id AND p.name = 'tenant.admin'
                );

                -- Create all required roles for all active tenants
                INSERT INTO roles (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'Admin',
                    'Administrator with full access',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM roles r 
                    WHERE r.tenant_id = t.id AND r.name = 'Admin'
                );

                INSERT INTO roles (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'Manager',
                    'Manager with project and user management access',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM roles r 
                    WHERE r.tenant_id = t.id AND r.name = 'Manager'
                );

                INSERT INTO roles (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'User',
                    'Basic user with read access',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM roles r 
                    WHERE r.tenant_id = t.id AND r.name = 'User'
                );

                -- Assign all permissions to Admin role
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_at, updated_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.name = 'Admin' AND r.tenant_id = t.id
                JOIN permissions p ON p.tenant_id = t.id
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id 
                    AND rp.role_id = r.id 
                    AND rp.permission_id = p.id
                );

                -- Assign specific permissions to Manager role
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_at, updated_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.name = 'Manager' AND r.tenant_id = t.id
                JOIN permissions p ON p.tenant_id = t.id 
                AND p.name IN ('project.read', 'project.write', 'project.delete', 'user.manage')
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id 
                    AND rp.role_id = r.id 
                    AND rp.permission_id = p.id
                );

                -- Assign read permission to User role
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_at, updated_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.name = 'User' AND r.tenant_id = t.id
                JOIN permissions p ON p.tenant_id = t.id AND p.name = 'project.read'
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id 
                    AND rp.role_id = r.id 
                    AND rp.permission_id = p.id
                );

                -- Ensure all existing users have Admin role if they don't have any role
                INSERT INTO user_roles (tenant_id, user_id, role_id, created_at, updated_at)
                SELECT 
                    u.tenant_id,
                    u.id,
                    r.id,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM users u
                JOIN tenants t ON t.id = u.tenant_id AND t.is_active = true
                JOIN roles r ON r.name = 'Admin' AND r.tenant_id = u.tenant_id
                WHERE NOT EXISTS (
                    SELECT 1 FROM user_roles ur 
                    WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
