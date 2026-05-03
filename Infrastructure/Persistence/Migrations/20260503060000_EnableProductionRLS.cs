using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableProductionRLS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable Row-Level Security for all tenant-specific tables
            migrationBuilder.Sql("ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE users ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE projects ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE subscriptions ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE roles ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE permissions ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE role_permissions ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens ENABLE ROW LEVEL SECURITY;");

            // Create tenant isolation policies for all tables
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_tenants ON tenants
                USING (id = current_setting('app.tenant_id', true)::int OR current_setting('app.tenant_id', true) IS NULL);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_users ON users
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_projects ON projects
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_subscriptions ON subscriptions
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_audit_logs ON audit_logs
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_roles ON roles
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_permissions ON permissions
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_user_roles ON user_roles
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_role_permissions ON role_permissions
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_refresh_tokens ON refresh_tokens
                USING (tenant_id = current_setting('app.tenant_id', true)::int);");

            // Set default policies to allow all operations for valid tenants
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_tenants ON tenants FOR ALL USING (id = current_setting('app.tenant_id', true)::int OR current_setting('app.tenant_id', true) IS NULL);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_users ON users FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_projects ON projects FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_subscriptions ON subscriptions FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_audit_logs ON audit_logs FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_roles ON roles FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_permissions ON permissions FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_user_roles ON user_roles FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_role_permissions ON role_permissions FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
            migrationBuilder.Sql("ALTER POLICY tenant_isolation_refresh_tokens ON refresh_tokens FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::int);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all tenant isolation policies
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_tenants ON tenants;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_users ON users;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_projects ON projects;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_subscriptions ON subscriptions;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_audit_logs ON audit_logs;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_roles ON roles;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_permissions ON permissions;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_user_roles ON user_roles;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_role_permissions ON role_permissions;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_refresh_tokens ON refresh_tokens;");

            // Disable Row-Level Security for all tables
            migrationBuilder.Sql("ALTER TABLE tenants DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE users DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE projects DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE subscriptions DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_logs DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE roles DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE permissions DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE user_roles DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens DISABLE ROW LEVEL SECURITY;");
        }
    }
}
