using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToRolePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TenantId column to role_permissions table
            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "role_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Update existing role_permissions to have the correct tenant_id based on the role
            migrationBuilder.Sql(@"
                UPDATE role_permissions 
                SET tenant_id = r.tenant_id 
                FROM roles r 
                WHERE role_permissions.role_id = r.id");

            // Create index for better performance
            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_tenant_id",
                table: "role_permissions",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the index
            migrationBuilder.DropIndex(
                name: "ix_role_permissions_tenant_id",
                table: "role_permissions");

            // Drop the TenantId column
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "role_permissions");
        }
    }
}
