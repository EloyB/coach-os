using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTennisClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TennisClubId",
                table: "LessonSeries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TennisClubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TennisClubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TennisClubs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Dev data cleanup: existing rows have TennisClubId = Guid.Empty which violates the FK.
            // Remove in FK-safe order: Payments → Enrollments → Lessons → LessonSeries.
            migrationBuilder.Sql("DELETE FROM \"Payments\";");
            migrationBuilder.Sql("DELETE FROM \"Enrollments\";");
            migrationBuilder.Sql("DELETE FROM \"Lessons\";");
            migrationBuilder.Sql("DELETE FROM \"LessonSeries\";");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeries_TennisClubId",
                table: "LessonSeries",
                column: "TennisClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TennisClubs_OrganizationId",
                table: "TennisClubs",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonSeries_TennisClubs_TennisClubId",
                table: "LessonSeries",
                column: "TennisClubId",
                principalTable: "TennisClubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonSeries_TennisClubs_TennisClubId",
                table: "LessonSeries");

            migrationBuilder.DropTable(
                name: "TennisClubs");

            migrationBuilder.DropIndex(
                name: "IX_LessonSeries_TennisClubId",
                table: "LessonSeries");

            migrationBuilder.DropColumn(
                name: "TennisClubId",
                table: "LessonSeries");
        }
    }
}
