using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateExistingTenantRBAC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create default permissions for the system
            migrationBuilder.Sql(@"
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
            ");

            // Create default roles for each tenant
            migrationBuilder.Sql(@"
                INSERT INTO roles (tenant_id, name, description, created_at, updated_at)
                SELECT 
                    t.id,
                    'Admin',
                    'Full administrative access',
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
                    'Manager level access',
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
                    'Basic user access',
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM roles r 
                    WHERE r.tenant_id = t.id AND r.name = 'User'
                );
            ");

            // Assign permissions to roles
            migrationBuilder.Sql(@"
                -- Admin role gets all permissions
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.tenant_id = t.id AND r.name = 'Admin'
                JOIN permissions p ON p.tenant_id = t.id
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
                );

                -- Manager role gets project permissions and user management
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.tenant_id = t.id AND r.name = 'Manager'
                JOIN permissions p ON p.tenant_id = t.id 
                    AND p.name IN ('project.read', 'project.write', 'project.delete', 'user.manage')
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
                );

                -- User role gets basic project permissions
                INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
                SELECT 
                    t.id,
                    r.id,
                    p.id,
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN roles r ON r.tenant_id = t.id AND r.name = 'User'
                JOIN permissions p ON p.tenant_id = t.id AND p.name = 'project.read'
                WHERE t.is_active = true
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp 
                    WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
                );
            ");

            // Assign existing users to appropriate roles based on their current role field
            migrationBuilder.Sql(@"
                -- Assign Admin role to users with 'Admin' role field
                INSERT INTO user_roles (tenant_id, user_id, role_id, assigned_at)
                SELECT 
                    u.tenant_id,
                    u.id,
                    r.id,
                    CURRENT_TIMESTAMP
                FROM users u
                JOIN roles r ON r.tenant_id = u.tenant_id AND r.name = 'Admin'
                WHERE u.role = 'Admin'
                AND NOT EXISTS (
                    SELECT 1 FROM user_roles ur 
                    WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
                );

                -- Assign Manager role to users with 'Manager' role field
                INSERT INTO user_roles (tenant_id, user_id, role_id, assigned_at)
                SELECT 
                    u.tenant_id,
                    u.id,
                    r.id,
                    CURRENT_TIMESTAMP
                FROM users u
                JOIN roles r ON r.tenant_id = u.tenant_id AND r.name = 'Manager'
                WHERE u.role = 'Manager'
                AND NOT EXISTS (
                    SELECT 1 FROM user_roles ur 
                    WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
                );

                -- Assign User role to users with 'User' role field or any other role
                INSERT INTO user_roles (tenant_id, user_id, role_id, assigned_at)
                SELECT 
                    u.tenant_id,
                    u.id,
                    r.id,
                    CURRENT_TIMESTAMP
                FROM users u
                JOIN roles r ON r.tenant_id = u.tenant_id AND r.name = 'User'
                WHERE u.role NOT IN ('Admin', 'Manager')
                AND NOT EXISTS (
                    SELECT 1 FROM user_roles ur 
                    WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
                );

                -- For any users without role assignments, assign them to User role as fallback
                INSERT INTO user_roles (tenant_id, user_id, role_id, assigned_at)
                SELECT 
                    u.tenant_id,
                    u.id,
                    r.id,
                    CURRENT_TIMESTAMP
                FROM users u
                JOIN roles r ON r.tenant_id = u.tenant_id AND r.name = 'User'
                WHERE NOT EXISTS (
                    SELECT 1 FROM user_roles ur 
                    WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
                );
            ");

            // Create audit log entries for the migration
            migrationBuilder.Sql(@"
                INSERT INTO audit_logs (tenant_id, user_id, action, data, timestamp)
                SELECT 
                    t.id,
                    u.id,
                    'RBAC_MIGRATION',
                    'User assigned to role during RBAC migration',
                    CURRENT_TIMESTAMP
                FROM tenants t
                JOIN users u ON u.tenant_id = t.id
                JOIN user_roles ur ON ur.tenant_id = t.id AND ur.user_id = u.id
                WHERE t.is_active = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the created data - this is a one-time migration
            // In production, you might want to keep this data
            migrationBuilder.Sql("DELETE FROM audit_logs WHERE action = 'RBAC_MIGRATION'");
            migrationBuilder.Sql("DELETE FROM user_roles WHERE assigned_at >= CURRENT_DATE - INTERVAL '1 day'");
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE granted_at >= CURRENT_DATE - INTERVAL '1 day'");
            migrationBuilder.Sql("DELETE FROM roles WHERE created_at >= CURRENT_DATE - INTERVAL '1 day'");
            migrationBuilder.Sql("DELETE FROM permissions WHERE created_at >= CURRENT_DATE - INTERVAL '1 day'");
        }
    }
}
