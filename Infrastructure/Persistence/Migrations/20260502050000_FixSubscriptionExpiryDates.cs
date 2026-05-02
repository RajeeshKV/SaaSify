using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSubscriptionExpiryDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing subscriptions to use Indian time zone and consistent monthly expiry
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

            // For expired subscriptions, set them to inactive and create new ones
            migrationBuilder.Sql(@"
                UPDATE subscriptions 
                SET is_active = false
                WHERE is_active = true 
                AND end_date < CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Asia/Kolkata'");

            // Create new active subscriptions for tenants with expired ones
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to original UTC-based dates
            migrationBuilder.Sql(@"
                UPDATE subscriptions 
                SET 
                    start_date = created_at,
                    end_date = created_at + INTERVAL '1 month'
                WHERE is_active = true");
        }
    }
}
