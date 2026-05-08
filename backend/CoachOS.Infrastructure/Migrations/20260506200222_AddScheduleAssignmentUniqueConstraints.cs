using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAssignmentUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId_En~1",
                table: "ScheduleAssignments",
                columns: new[] { "LessonSerieId", "WeeklyTemplateEntryId", "EnrollmentId" },
                unique: true,
                filter: "\"EnrollmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId_Enr~",
                table: "ScheduleAssignments",
                columns: new[] { "LessonSerieId", "WeeklyTemplateEntryId", "EnrollmentGroupId" },
                unique: true,
                filter: "\"EnrollmentGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId_En~1",
                table: "ScheduleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId_Enr~",
                table: "ScheduleAssignments");
        }
    }
}
