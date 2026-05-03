using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TenantId column to user_roles table
            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "user_roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Update existing user_roles to have the correct tenant_id based on the user
            migrationBuilder.Sql(@"
                UPDATE user_roles 
                SET tenant_id = u.tenant_id 
                FROM users u 
                WHERE user_roles.user_id = u.id");

            // Create index for better performance
            migrationBuilder.CreateIndex(
                name: "ix_user_roles_tenant_id",
                table: "user_roles",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the index
            migrationBuilder.DropIndex(
                name: "ix_user_roles_tenant_id",
                table: "user_roles");

            // Drop the TenantId column
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "user_roles");
        }
    }
}
