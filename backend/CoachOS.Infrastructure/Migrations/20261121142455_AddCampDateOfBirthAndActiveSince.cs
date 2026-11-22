using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampDateOfBirthAndActiveSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveSince",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveSince",
                table: "CampEnrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "CampEnrollments",
                type: "date",
                nullable: true);

            // Bestaande bevestigde of op betaling wachtende inschrijvingen: benader het moment met de laatste
            // wijziging (fallback: inschrijfdatum). Status 2 = Confirmed, 5 = PendingPayment.
            migrationBuilder.Sql("UPDATE \"Enrollments\" SET \"ActiveSince\" = COALESCE(\"UpdatedAt\", \"EnrolledAt\") WHERE \"Status\" IN (2, 5);");
            migrationBuilder.Sql("UPDATE \"CampEnrollments\" SET \"ActiveSince\" = COALESCE(\"UpdatedAt\", \"EnrolledAt\") WHERE \"Status\" IN (2, 5);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveSince",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ActiveSince",
                table: "CampEnrollments");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "CampEnrollments");
        }
    }
}
