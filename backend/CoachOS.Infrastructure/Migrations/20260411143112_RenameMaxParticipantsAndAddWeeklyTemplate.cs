using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaxParticipantsAndAddWeeklyTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxParticipants",
                table: "LessonSeries",
                newName: "MaxRegistrations");

            migrationBuilder.CreateTable(
                name: "WeeklyTemplateEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonSerieId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourtName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTemplateEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyTemplateEntries_LessonSeries_LessonSerieId",
                        column: x => x.LessonSerieId,
                        principalTable: "LessonSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTemplateEntries_LessonSerieId",
                table: "WeeklyTemplateEntries",
                column: "LessonSerieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyTemplateEntries");

            migrationBuilder.RenameColumn(
                name: "MaxRegistrations",
                table: "LessonSeries",
                newName: "MaxParticipants");
        }
    }
}
