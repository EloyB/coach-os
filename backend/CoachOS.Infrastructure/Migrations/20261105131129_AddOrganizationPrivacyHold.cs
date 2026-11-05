using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPrivacyHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "OrganizationOffboardingNotices" (
                    "OrganizationId" uuid NOT NULL, "EndDate" date NOT NULL, "Kind" integer NOT NULL CHECK ("Kind" IN (0,1,2)),
                    "UserId" uuid NOT NULL, "Status" text NOT NULL DEFAULT 'pending', "Attempts" integer NOT NULL DEFAULT 0,
                    "AvailableAt" timestamptz NOT NULL DEFAULT now(), "CompletedAt" timestamptz NULL,
                    "LastErrorCode" text NULL,
                    PRIMARY KEY ("OrganizationId","EndDate","Kind","UserId")
                );
                CREATE INDEX "IX_OffboardingNotices_Delivery" ON "OrganizationOffboardingNotices" ("Status","AvailableAt");
                """);
            // Operational recovery receipts survive the erased organization; no tenant FK.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "PrivacyErasureReceipts" (
                    "OrganizationId" uuid NOT NULL,
                    "TargetKind" text NOT NULL CHECK ("TargetKind" IN ('enrollment','camp_enrollment','organization')),
                    "TargetId" uuid NOT NULL,
                    "CompletedAt" timestamp with time zone NOT NULL,
                    PRIMARY KEY ("OrganizationId", "TargetKind", "TargetId")
                )
                """);
            migrationBuilder.AddColumn<bool>(
                name: "PrivacyHold",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE \"OrganizationOffboardingNotices\"");
            migrationBuilder.DropColumn(
                name: "PrivacyHold",
                table: "Organizations");
        }
    }
}
