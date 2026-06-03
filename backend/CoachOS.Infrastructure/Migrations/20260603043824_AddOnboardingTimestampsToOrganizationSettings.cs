using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingTimestampsToOrganizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingDismissedAt",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingStartedAt",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainerModeChosenAt",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingDismissedAt",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OnboardingStartedAt",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "TrainerModeChosenAt",
                table: "OrganizationSettings");
        }
    }
}
