using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxParticipantsToLessonSerie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "LessonSeries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LessonSerieId_StudentEmail",
                table: "Enrollments",
                columns: new[] { "LessonSerieId", "StudentEmail" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LessonSerieId_StudentEmail",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "LessonSeries");
        }
    }
}
