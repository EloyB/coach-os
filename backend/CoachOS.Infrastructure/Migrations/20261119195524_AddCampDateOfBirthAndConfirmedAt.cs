using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampDateOfBirthAndConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "CampEnrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "CampEnrollments",
                type: "date",
                nullable: true);

            // Bestaande bevestigde inschrijvingen: benader het bevestigingsmoment met de laatste wijziging
            // (fallback: inschrijfdatum). Status 2 = Confirmed.
            migrationBuilder.Sql("UPDATE \"Enrollments\" SET \"ConfirmedAt\" = COALESCE(\"UpdatedAt\", \"EnrolledAt\") WHERE \"Status\" = 2;");
            migrationBuilder.Sql("UPDATE \"CampEnrollments\" SET \"ConfirmedAt\" = COALESCE(\"UpdatedAt\", \"EnrolledAt\") WHERE \"Status\" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "CampEnrollments");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "CampEnrollments");
        }
    }
}
