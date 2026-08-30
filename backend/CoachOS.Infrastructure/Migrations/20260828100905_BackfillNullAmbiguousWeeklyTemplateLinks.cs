using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillNullAmbiguousWeeklyTemplateLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repareert de backfill uit AddLessonWeeklyTemplateLink (20260826051456): die matchte
            // lessen aan een weekslot op reeks + dag + starttijd + baan, maar meerdere onbenoemde
            // weekslots (CourtName null) op hetzelfde dag+start zijn expliciet toegestaan (parallelle
            // velden, baan later toewijzen). Bij zo'n ambiguïteit koos de UPDATE...FROM willekeurig
            // ÉÉN van de weekslots, waardoor alle lessen van dat moment aan dat ene weekslot hingen
            // en het andere weekslot zonder lessen bleef — met foute "pas hele weekslot aan"-updates
            // tot gevolg. Los-koppelen (NULL) is hier veiliger dan gokken: geen match is prima (zie
            // ook de nieuwe aanmaak-logica in LessonSerieService.CreateAsync, die dezelfde regel volgt).
            migrationBuilder.Sql("""
                WITH ambiguous_groups AS (
                    SELECT "LessonSerieId", "DayOfWeek", "StartTime"
                    FROM "WeeklyTemplateEntries"
                    WHERE "CourtName" IS NULL
                    GROUP BY "LessonSerieId", "DayOfWeek", "StartTime"
                    HAVING COUNT(*) > 1
                ),
                ambiguous_entry_ids AS (
                    SELECT w."Id"
                    FROM "WeeklyTemplateEntries" AS w
                    INNER JOIN ambiguous_groups AS g
                        ON w."LessonSerieId" = g."LessonSerieId"
                       AND w."DayOfWeek" = g."DayOfWeek"
                       AND w."StartTime" = g."StartTime"
                    WHERE w."CourtName" IS NULL
                )
                UPDATE "Lessons"
                SET "WeeklyTemplateEntryId" = NULL
                WHERE "WeeklyTemplateEntryId" IN (SELECT "Id" FROM ambiguous_entry_ids);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Niet omkeerbaar: de oorspronkelijke (foutieve, willekeurige) koppeling die hier
            // losgemaakt wordt, is niet reconstrueerbaar — en zou ook niet terug moeten.
        }
    }
}
