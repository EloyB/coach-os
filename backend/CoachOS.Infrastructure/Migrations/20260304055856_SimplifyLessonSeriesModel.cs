using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLessonSeriesModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Courts_CourtId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonSeries_Courts_CourtId",
                table: "LessonSeries");

            migrationBuilder.DropIndex(
                name: "IX_LessonSeries_CourtId",
                table: "LessonSeries");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CourtId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "Lessons");

            migrationBuilder.AddColumn<string>(
                name: "CourtName",
                table: "Lessons",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourtName",
                table: "Lessons");

            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "LessonSeries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "LessonSeries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "LessonSeries",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "Lessons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeries_CourtId",
                table: "LessonSeries",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourtId",
                table: "Lessons",
                column: "CourtId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Courts_CourtId",
                table: "Lessons",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonSeries_Courts_CourtId",
                table: "LessonSeries",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
