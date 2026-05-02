using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitializeFreeSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing tenants to have Free plan if not already set
            migrationBuilder.Sql(@"
                UPDATE tenants 
                SET plan = 'Free', is_active = true 
                WHERE plan IS NULL OR plan = ''");

            // Create free plan subscriptions for existing tenants without subscriptions
            migrationBuilder.Sql(@"
                INSERT INTO subscriptions (tenant_id, plan, start_date, end_date, is_active, amount, currency, created_at)
                SELECT 
                    t.id,
                    'Free',
                    COALESCE(t.created_at, CURRENT_TIMESTAMP),
                    CURRENT_TIMESTAMP + INTERVAL '1 month',
                    true,
                    0,
                    'USD',
                    CURRENT_TIMESTAMP
                FROM tenants t
                LEFT JOIN subscriptions s ON t.id = s.tenant_id AND s.is_active = true
                WHERE t.is_active = true 
                AND s.id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
