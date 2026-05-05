using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonRescheduleLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RescheduledToLessonId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_RescheduledToLessonId",
                table: "Lessons",
                column: "RescheduledToLessonId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Lessons_RescheduledToLessonId",
                table: "Lessons",
                column: "RescheduledToLessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Lessons_RescheduledToLessonId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_RescheduledToLessonId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "RescheduledToLessonId",
                table: "Lessons");
        }
    }
}
