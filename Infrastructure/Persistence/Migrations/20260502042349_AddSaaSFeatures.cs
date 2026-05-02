using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaaSFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "plan",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: true),
                    entity = table.Column<string>(type: "text", nullable: true),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    resource = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id",
                table: "audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                table: "roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_tenant_id",
                table: "subscriptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            // Enable Row-Level Security for tenant-specific tables
            migrationBuilder.Sql("ALTER TABLE users ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE projects ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE roles ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE subscriptions ENABLE ROW LEVEL SECURITY;");

            // Create RLS policies for tenant isolation
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_users ON users
                USING (tenant_id = current_setting('app.tenant_id')::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_projects ON projects
                USING (tenant_id = current_setting('app.tenant_id')::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_refresh_tokens ON refresh_tokens
                USING (user_id IN (
                    SELECT id FROM users 
                    WHERE tenant_id = current_setting('app.tenant_id')::int
                ));");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_roles ON roles
                USING (tenant_id = current_setting('app.tenant_id')::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_user_roles ON user_roles
                USING (user_id IN (
                    SELECT id FROM users 
                    WHERE tenant_id = current_setting('app.tenant_id')::int
                ));");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_audit_logs ON audit_logs
                USING (tenant_id = current_setting('app.tenant_id')::int);");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_subscriptions ON subscriptions
                USING (tenant_id = current_setting('app.tenant_id')::int);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop RLS policies
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_users ON users;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_projects ON projects;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_refresh_tokens ON refresh_tokens;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_roles ON roles;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_user_roles ON user_roles;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_audit_logs ON audit_logs;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_subscriptions ON subscriptions;");

            // Disable Row-Level Security
            migrationBuilder.Sql("ALTER TABLE users DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE projects DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE roles DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE user_roles DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE audit_logs DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE subscriptions DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "plan",
                table: "tenants");
        }
    }
}
