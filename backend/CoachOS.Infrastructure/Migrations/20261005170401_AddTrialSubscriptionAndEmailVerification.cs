using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialSubscriptionAndEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Plan",
                table: "Subscriptions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "LastTrialReminderDay",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TrialEndsAt",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationAttempts",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeExpiry",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCodeHash",
                table: "AspNetUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationSendCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationSentAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TermsAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermsAcceptances_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptances_OrganizationId",
                table: "TermsAcceptances",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TermsAcceptances_UserId",
                table: "TermsAcceptances",
                column: "UserId");

            // Bestaande klanten mogen niets merken van het abonnementsmodel:
            // 1) elke organisatie zonder abonnement krijgt er een met status Legacy (= 1):
            //    volledige toegang, geen proef, geen herinneringsmails;
            // 2) abonnementen die al bestonden krijgen via de kolom-default eveneens Legacy;
            // 3) alle bestaande gebruikers gelden als e-mail-bevestigd, zodat de nieuwe
            //    verificatie-eis voor het mailen van trainers/leerlingen hen nooit blokkeert.
            migrationBuilder.Sql("""
                INSERT INTO "Subscriptions"
                    ("Id", "OrganizationId", "Plan", "Status", "StartDate", "IsActive", "MonthlyPrice", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), o."Id", NULL, 1, CAST(o."CreatedAt" AS date), TRUE, 0, now(), now()
                FROM "Organizations" AS o
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Subscriptions" AS s WHERE s."OrganizationId" = o."Id"
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers" SET "EmailConfirmed" = TRUE WHERE "EmailConfirmed" = FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Legacy- en proefabonnementen hebben geen plan; die rijen moeten weg voor "Plan"
            // hieronder terug NOT NULL kan worden. Vóór deze migratie bestonden ze niet.
            migrationBuilder.Sql("""
                DELETE FROM "Subscriptions" WHERE "Plan" IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "TermsAcceptances");

            migrationBuilder.DropColumn(
                name: "LastTrialReminderDay",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EmailVerificationAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationSendCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationSentAt",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "Plan",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
