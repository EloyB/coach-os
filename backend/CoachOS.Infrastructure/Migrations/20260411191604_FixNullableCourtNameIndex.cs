using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNullableCourtNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries",
                columns: new[] { "LessonSerieId", "DayOfWeek", "StartTime", "CourtName" },
                unique: true,
                filter: "\"CourtName\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_Cou~",
                table: "WeeklyTemplateEntries",
                columns: new[] { "LessonSerieId", "DayOfWeek", "StartTime", "CourtName" },
                unique: true);
        }
    }
}
