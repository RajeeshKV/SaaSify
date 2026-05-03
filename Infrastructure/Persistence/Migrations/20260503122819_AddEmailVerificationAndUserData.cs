using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndUserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

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

            // Complete RBAC setup for all active tenants
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
            ");

            // Create roles for all active tenants
            migrationBuilder.Sql(@"
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
                JOIN roles r ON r.name = 'Admin' AND r.tenant_id = t.id
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

            // Fix subscription expiry dates for Indian time zone
            migrationBuilder.Sql(@"
                UPDATE subscriptions 
                SET 
                    start_date = DATE_TRUNC('day', CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata'),
                    end_date = DATE_TRUNC('day', CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata') + INTERVAL '1 month'
                WHERE is_active = true
                AND (
                    start_date IS NULL 
                    OR end_date IS NULL 
                    OR end_date <= start_date
                    OR DATE_PART('day', start_date) != DATE_PART('day', end_date)
                )");

            // Deactivate expired subscriptions
            migrationBuilder.Sql(@"
                UPDATE subscriptions 
                SET is_active = false
                WHERE is_active = true 
                AND end_date < CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata'");

            // Create new active subscriptions for expired ones
            migrationBuilder.Sql(@"
                INSERT INTO subscriptions (tenant_id, plan, start_date, end_date, is_active, amount, currency, created_at)
                SELECT 
                    s.tenant_id,
                    s.plan,
                    DATE_TRUNC('day', CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata'),
                    DATE_TRUNC('day', CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata') + INTERVAL '1 month',
                    true,
                    COALESCE(s.amount, 0),
                    COALESCE(s.currency, 'USD'),
                    CURRENT_TIMESTAMP
                FROM subscriptions s
                WHERE s.is_active = false
                AND s.tenant_id NOT IN (
                    SELECT tenant_id 
                    FROM subscriptions 
                    WHERE is_active = true
                )
                AND s.tenant_id IN (
                    SELECT id 
                    FROM tenants 
                    WHERE is_active = true
                )");

            // Create tenant settings for all active tenants
            migrationBuilder.Sql(@"
                INSERT INTO tenant_settings (tenant_id, max_projects, max_users, enable_advanced_features, enable_api_access, enable_export, enable_integrations, max_storage_mb, max_api_calls_per_day, updated_at, created_at)
                SELECT 
                    t.id,
                    CASE 
                        WHEN t.plan = 'Free' THEN 5
                        WHEN t.plan = 'Professional' THEN 50
                        WHEN t.plan = 'Enterprise' THEN -1
                        ELSE 5
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN 3
                        WHEN t.plan = 'Professional' THEN 10
                        WHEN t.plan = 'Enterprise' THEN 50
                        ELSE 3
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN false
                        ELSE true
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN false
                        ELSE true
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN false
                        ELSE true
                    END,
                    CASE 
                        WHEN t.plan = 'Enterprise' THEN true
                        ELSE false
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN 100
                        WHEN t.plan = 'Professional' THEN 1000
                        WHEN t.plan = 'Enterprise' THEN 10000
                        ELSE 100
                    END,
                    CASE 
                        WHEN t.plan = 'Free' THEN 100
                        WHEN t.plan = 'Professional' THEN 1000
                        WHEN t.plan = 'Enterprise' THEN 5000
                        ELSE 100
                    END,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM tenants t
                WHERE t.is_active = true
                AND t.id NOT IN (SELECT tenant_id FROM tenant_settings)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_verification_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_token_expires",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "name",
                table: "users");
        }
    }
}
