using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPrivacyHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Shared operational recovery ledger, deliberately without a tenant FK:
            // its minimal deletion receipt must survive removal of its subject/organization.
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
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrivacyHold",
                table: "CampEnrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivacyHold",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PrivacyHold",
                table: "CampEnrollments");
        }
    }
}
