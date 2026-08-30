using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonWeeklyTemplateLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WeeklyTemplateEntryId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_WeeklyTemplateEntryId",
                table: "Lessons",
                column: "WeeklyTemplateEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_WeeklyTemplateEntries_WeeklyTemplateEntryId",
                table: "Lessons",
                column: "WeeklyTemplateEntryId",
                principalTable: "WeeklyTemplateEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Backfill bestaande lessen: koppel aan hun weekslot op reeks + dag + starttijd + baan.
            // Nieuwe lessen krijgen de koppeling al bij aanmaak. Best-effort: een les die vóór deze
            // migratie los bewerkt is (afwijkende tijd) matcht niet en blijft null — dat is prima.
            // Postgres EXTRACT(DOW): 0=zondag; onze DayOfWeek: 0=maandag → (dow + 6) % 7.
            migrationBuilder.Sql("""
                UPDATE "Lessons" AS l
                SET "WeeklyTemplateEntryId" = w."Id"
                FROM "WeeklyTemplateEntries" AS w
                WHERE l."LessonSerieId" = w."LessonSerieId"
                  AND ((EXTRACT(DOW FROM l."Date")::int + 6) % 7) = w."DayOfWeek"
                  AND l."StartTime" = w."StartTime"
                  AND COALESCE(l."CourtName", '') = COALESCE(w."CourtName", '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_WeeklyTemplateEntries_WeeklyTemplateEntryId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_WeeklyTemplateEntryId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "WeeklyTemplateEntryId",
                table: "Lessons");
        }
    }
}
