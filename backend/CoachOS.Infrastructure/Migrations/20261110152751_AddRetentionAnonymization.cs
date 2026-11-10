using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionAnonymization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "PrivacyErasureReceipts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Deleted");

            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAt",
                table: "CampEnrollments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Method",
                table: "PrivacyErasureReceipts");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "CampEnrollments");
        }
    }
}
