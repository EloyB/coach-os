using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillTrialSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deploy safety net: SubscriptionStatusMachine only added columns, so
            // organizations created before this feature shipped have NO row in
            // Subscriptions. SubscriptionAccessMiddleware fails closed on a null
            // subscription, which would 403 every pre-existing org on every
            // request post-deploy. Backfill a Trialing subscription (60-day trial
            // from now) for every org that doesn't already have one.
            // NOTE: the 60-day grant is a business decision — confirm before prod.
            migrationBuilder.Sql(@"
                INSERT INTO ""Subscriptions"" (""Id"", ""OrganizationId"", ""Status"", ""TrialEndsAt"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), o.""Id"", 1, now() + interval '60 days', now(), now()
                FROM ""Organizations"" o
                WHERE NOT EXISTS (SELECT 1 FROM ""Subscriptions"" s WHERE s.""OrganizationId"" = o.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: backfilled rows are indistinguishable from legitimately
            // created Trialing subscriptions, so we can't safely delete them.
        }
    }
}
