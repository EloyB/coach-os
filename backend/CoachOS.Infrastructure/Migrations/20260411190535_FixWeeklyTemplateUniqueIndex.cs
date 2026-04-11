using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWeeklyTemplateUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime",
                table: "WeeklyTemplateEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries",
                columns: new[] { "LessonSerieId", "DayOfWeek", "StartTime", "CourtName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime",
                table: "WeeklyTemplateEntries",
                columns: new[] { "LessonSerieId", "DayOfWeek", "StartTime" },
                unique: true);
        }
    }
}
