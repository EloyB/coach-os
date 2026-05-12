using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTrainerToMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTrainer",
                table: "OrganizationMemberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: bestaande Trainer-memberships (UserRole.Trainer = 2) zijn per definitie trainers.
            // Admin- en Student-memberships blijven default false.
            migrationBuilder.Sql(@"UPDATE ""OrganizationMemberships"" SET ""IsTrainer"" = true WHERE ""Role"" = 2;");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_IsTrainer",
                table: "OrganizationMemberships",
                columns: new[] { "OrganizationId", "IsTrainer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_OrganizationId_IsTrainer",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "IsTrainer",
                table: "OrganizationMemberships");
        }
    }
}
