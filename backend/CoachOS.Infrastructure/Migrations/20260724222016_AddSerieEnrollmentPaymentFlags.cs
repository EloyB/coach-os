using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSerieEnrollmentPaymentFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptManualPayment",
                table: "LessonSeries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptOnlinePayment",
                table: "LessonSeries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowGroupEnrollment",
                table: "LessonSeries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowSoloEnrollment",
                table: "LessonSeries",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptManualPayment",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "AcceptOnlinePayment",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "AllowGroupEnrollment",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "AllowSoloEnrollment",
                table: "LessonSeries");
        }
    }
}
