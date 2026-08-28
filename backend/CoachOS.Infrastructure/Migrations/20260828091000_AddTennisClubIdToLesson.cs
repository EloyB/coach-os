using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTennisClubIdToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TennisClubId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TennisClubId",
                table: "Lessons",
                column: "TennisClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_TennisClubs_TennisClubId",
                table: "Lessons",
                column: "TennisClubId",
                principalTable: "TennisClubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_TennisClubs_TennisClubId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_TennisClubId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "TennisClubId",
                table: "Lessons");
        }
    }
}
