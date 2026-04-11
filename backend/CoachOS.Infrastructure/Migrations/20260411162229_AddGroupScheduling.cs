using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanningStatus",
                table: "LessonSeries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EnrollmentGroupId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToGrouping",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EnrollmentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonSerieId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LeaderEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentGroups_Enrollments_LeaderEnrollmentId",
                        column: x => x.LeaderEnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentGroups_LessonSeries_LessonSerieId",
                        column: x => x.LessonSerieId,
                        principalTable: "LessonSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentGroups_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlotPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeeklyTemplateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Preference = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlotPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeSlotPreferences_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotPreferences_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotPreferences_WeeklyTemplateEntries_WeeklyTemplateEnt~",
                        column: x => x.WeeklyTemplateEntryId,
                        principalTable: "WeeklyTemplateEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonSerieId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeeklyTemplateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleAssignments_EnrollmentGroups_EnrollmentGroupId",
                        column: x => x.EnrollmentGroupId,
                        principalTable: "EnrollmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleAssignments_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleAssignments_LessonSeries_LessonSerieId",
                        column: x => x.LessonSerieId,
                        principalTable: "LessonSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleAssignments_WeeklyTemplateEntries_WeeklyTemplateEnt~",
                        column: x => x.WeeklyTemplateEntryId,
                        principalTable: "WeeklyTemplateEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime",
                table: "WeeklyTemplateEntries",
                columns: new[] { "LessonSerieId", "DayOfWeek", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrollmentGroupId",
                table: "Enrollments",
                column: "EnrollmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentGroups_LeaderEnrollmentId",
                table: "EnrollmentGroups",
                column: "LeaderEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentGroups_LessonSerieId",
                table: "EnrollmentGroups",
                column: "LessonSerieId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentGroups_OrganizationId",
                table: "EnrollmentGroups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_EnrollmentGroupId",
                table: "ScheduleAssignments",
                column: "EnrollmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_EnrollmentId",
                table: "ScheduleAssignments",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId",
                table: "ScheduleAssignments",
                columns: new[] { "LessonSerieId", "WeeklyTemplateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_OrganizationId",
                table: "ScheduleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleAssignments_WeeklyTemplateEntryId",
                table: "ScheduleAssignments",
                column: "WeeklyTemplateEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotPreferences_EnrollmentId_WeeklyTemplateEntryId",
                table: "TimeSlotPreferences",
                columns: new[] { "EnrollmentId", "WeeklyTemplateEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotPreferences_OrganizationId",
                table: "TimeSlotPreferences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotPreferences_WeeklyTemplateEntryId",
                table: "TimeSlotPreferences",
                column: "WeeklyTemplateEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_EnrollmentGroups_EnrollmentGroupId",
                table: "Enrollments",
                column: "EnrollmentGroupId",
                principalTable: "EnrollmentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_EnrollmentGroups_EnrollmentGroupId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "ScheduleAssignments");

            migrationBuilder.DropTable(
                name: "TimeSlotPreferences");

            migrationBuilder.DropTable(
                name: "EnrollmentGroups");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime",
                table: "WeeklyTemplateEntries");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_EnrollmentGroupId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PlanningStatus",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "EnrollmentGroupId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsOpenToGrouping",
                table: "Enrollments");
        }
    }
}
