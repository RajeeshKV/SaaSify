using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_settings",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    max_projects = table.Column<int>(type: "integer", nullable: false),
                    max_users = table.Column<int>(type: "integer", nullable: false),
                    enable_advanced_features = table.Column<bool>(type: "boolean", nullable: false),
                    enable_api_access = table.Column<bool>(type: "boolean", nullable: false),
                    enable_export = table.Column<bool>(type: "boolean", nullable: false),
                    enable_integrations = table.Column<bool>(type: "boolean", nullable: false),
                    max_storage_mb = table.Column<long>(type: "bigint", nullable: false),
                    max_api_calls_per_day = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings", x => x.tenant_id);
                    table.ForeignKey(
                        name: "fk_tenant_settings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index for better performance
            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_tenant_id",
                table: "tenant_settings",
                column: "tenant_id",
                unique: true);

            // Insert default settings for existing tenants based on their plan
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
            migrationBuilder.DropTable(
                name: "tenant_settings");
        }
    }
}
