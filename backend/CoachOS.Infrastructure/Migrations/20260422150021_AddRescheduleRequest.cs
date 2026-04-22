using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRescheduleRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RescheduleRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnrollmentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlternativeWeeklyTemplateEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolverNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescheduleRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescheduleRequests_EnrollmentGroups_EnrollmentGroupId",
                        column: x => x.EnrollmentGroupId,
                        principalTable: "EnrollmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescheduleRequests_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescheduleRequests_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescheduleRequests_ScheduleAssignments_ScheduleAssignmentId",
                        column: x => x.ScheduleAssignmentId,
                        principalTable: "ScheduleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RescheduleRequests_WeeklyTemplateEntries_AlternativeWeeklyT~",
                        column: x => x.AlternativeWeeklyTemplateEntryId,
                        principalTable: "WeeklyTemplateEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RescheduleRequests_AlternativeWeeklyTemplateEntryId",
                table: "RescheduleRequests",
                column: "AlternativeWeeklyTemplateEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RescheduleRequests_EnrollmentGroupId",
                table: "RescheduleRequests",
                column: "EnrollmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RescheduleRequests_EnrollmentId",
                table: "RescheduleRequests",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RescheduleRequests_OrganizationId",
                table: "RescheduleRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RescheduleRequests_ScheduleAssignmentId",
                table: "RescheduleRequests",
                column: "ScheduleAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RescheduleRequests");
        }
    }
}
