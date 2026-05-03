using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantSettingsPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "user_roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "role_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "permissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.CreateIndex(
                name: "ix_permissions_tenant_id",
                table: "permissions",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_permissions_tenants_tenant_id",
                table: "permissions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_permissions_tenants_tenant_id",
                table: "permissions");

            migrationBuilder.DropTable(
                name: "tenant_settings");

            migrationBuilder.DropIndex(
                name: "ix_permissions_tenant_id",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "permissions");
        }
    }
}
