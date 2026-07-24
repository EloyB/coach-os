using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeRangeToLessonSerie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "LessonSeries",
                type: "integer",
                nullable: false,
                defaultValue: 99);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "LessonSeries",
                type: "integer",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "LessonSeries");
        }
    }
}
