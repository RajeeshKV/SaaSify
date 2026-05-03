using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEmailVerificationAndUserData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to users table
        migrationBuilder.AddColumn<string>(
            name: "name",
            table: "users",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "is_email_verified",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: true); // Set existing users as verified

        migrationBuilder.AddColumn<string>(
            name: "email_verification_token",
            table: "users",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "email_verification_token_expires",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "email_verified_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        // Populate existing users with random names and mark as verified
        migrationBuilder.Sql(@"
            UPDATE users 
            SET 
                name = CASE 
                    WHEN id = 1 THEN 'John Smith'
                    WHEN id = 2 THEN 'Sarah Johnson'
                    WHEN id = 3 THEN 'Michael Davis'
                    WHEN id = 4 THEN 'Emily Wilson'
                    WHEN id = 5 THEN 'David Brown'
                    WHEN id = 6 THEN 'Jessica Martinez'
                    WHEN id = 7 THEN 'Robert Anderson'
                    WHEN id = 8 THEN 'Lisa Thompson'
                    WHEN id = 9 THEN 'James Garcia'
                    WHEN id = 10 THEN 'Mary Rodriguez'
                    ELSE 'User ' || id
                END,
                is_email_verified = true,
                email_verified_at = CURRENT_TIMESTAMP
            WHERE name IS NULL OR name = ''
        ");

        // Ensure all active tenants have proper RBAC setup
        migrationBuilder.Sql(@"
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
        ");

        // Create roles for all active tenants
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
            INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
            SELECT 
                t.id,
                r.id,
                p.id,
                CURRENT_TIMESTAMP
            FROM tenants t
            JOIN roles r ON r.tenant_id = t.id AND r.name = 'Admin'
            JOIN permissions p ON p.tenant_id = t.id 
                AND p.name IN ('project.read', 'project.write', 'project.delete', 'user.manage', 'subscription.manage', 'tenant.admin')
            WHERE t.is_active = true
            AND NOT EXISTS (
                SELECT 1 FROM role_permissions rp 
                WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
            );

            INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
            SELECT 
                t.id,
                r.id,
                p.id,
                CURRENT_TIMESTAMP
            FROM tenants t
            JOIN roles r ON r.name = 'Manager' AND r.tenant_id = t.id
            JOIN permissions p ON p.tenant_id = t.id 
                AND p.name IN ('project.read', 'project.write', 'project.delete', 'user.manage')
            WHERE t.is_active = true
            AND NOT EXISTS (
                SELECT 1 FROM role_permissions rp 
                WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
            );

            INSERT INTO role_permissions (tenant_id, role_id, permission_id, granted_at)
            SELECT 
                t.id,
                r.id,
                p.id,
                CURRENT_TIMESTAMP
            FROM tenants t
            JOIN roles r ON r.name = 'User' AND r.tenant_id = t.id
            JOIN permissions p ON p.tenant_id = t.id AND p.name = 'project.read'
            WHERE t.is_active = true
            AND NOT EXISTS (
                SELECT 1 FROM role_permissions rp 
                WHERE rp.tenant_id = t.id AND rp.role_id = r.id AND rp.permission_id = p.id
            );
        ");

        // Assign roles to users without roles
        migrationBuilder.Sql(@"
            INSERT INTO user_roles (tenant_id, user_id, role_id, assigned_at)
            SELECT 
                u.tenant_id,
                u.id,
                r.id,
                CURRENT_TIMESTAMP
            FROM users u
            JOIN roles r ON r.tenant_id = u.tenant_id AND r.name = COALESCE(u.role, 'User')
            WHERE NOT EXISTS (
                SELECT 1 FROM user_roles ur 
                WHERE ur.tenant_id = u.tenant_id AND ur.user_id = u.id
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove role assignments
        migrationBuilder.Sql("DELETE FROM user_roles WHERE tenant_id IN (SELECT id FROM tenants WHERE is_active = true)");

        // Remove role permissions
        migrationBuilder.Sql("DELETE FROM role_permissions WHERE tenant_id IN (SELECT id FROM tenants WHERE is_active = true)");

        // Remove roles
        migrationBuilder.Sql("DELETE FROM roles WHERE tenant_id IN (SELECT id FROM tenants WHERE is_active = true)");

        // Remove permissions created by this migration
        migrationBuilder.Sql(@"
            DELETE FROM permissions 
            WHERE tenant_id IN (SELECT id FROM tenants WHERE is_active = true)
            AND name IN ('tenant.access', 'project.read', 'project.write', 'project.delete', 'user.manage', 'subscription.manage', 'tenant.admin')
        ");

        // Drop new columns
        migrationBuilder.DropColumn(
            name: "email_verified_at",
            table: "users");

        migrationBuilder.DropColumn(
            name: "email_verification_token_expires",
            table: "users");

        migrationBuilder.DropColumn(
            name: "email_verification_token",
            table: "users");

        migrationBuilder.DropColumn(
            name: "is_email_verified",
            table: "users");

        migrationBuilder.DropColumn(
            name: "name",
            table: "users");
    }
}
