using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsOpenToGroupingWithMaxGroupSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Nieuwe kolom, tijdelijk met 1 als standaard; de echte waarden volgen uit de oude vlag.
            migrationBuilder.AddColumn<int>(
                name: "MaxGroupSize",
                table: "Enrollments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // 2. Bestaande inschrijvingen krijgen het maximum van de reeks: het grootste weekslot, hoogstens 4 (zonder
            //    weekslots 4), maar nooit lager dan de eigen groep. De oude vlag "open voor groepering" speelt dus geen
            //    rol meer. Voor een groep krijgen alle leden dezelfde waarde. Actief = Pending (1), Confirmed (2) of
            //    PendingPayment (5); geannuleerde leden tellen niet mee voor de groepsgrootte.
            migrationBuilder.Sql(@"
UPDATE ""Enrollments"" AS e
SET ""MaxGroupSize"" = LEAST(4, GREATEST(sz.own_size, sz.series_max))
FROM (
    SELECT
        e2.""Id"" AS id,
        CASE WHEN e2.""EnrollmentGroupId"" IS NULL THEN 1
             ELSE GREATEST(1, (SELECT COUNT(*) FROM ""Enrollments"" m
                               WHERE m.""EnrollmentGroupId"" = e2.""EnrollmentGroupId"" AND m.""Status"" IN (1, 2, 5)))
        END AS own_size,
        COALESCE((SELECT CASE WHEN MAX(w.""MaxStudents"") < 1 THEN 4 ELSE LEAST(4, MAX(w.""MaxStudents"")) END
                  FROM ""WeeklyTemplateEntries"" w WHERE w.""LessonSerieId"" = e2.""LessonSerieId""), 4) AS series_max
    FROM ""Enrollments"" e2
) AS sz
WHERE sz.id = e.""Id"";");

            // 3. De oude vlag mag weg.
            migrationBuilder.DropColumn(
                name: "IsOpenToGrouping",
                table: "Enrollments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToGrouping",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Terug naar de vlag: open als het maximum groter is dan de eigen groep. Zoals vroeger krijgt enkel een
            // solo of een groepsleider de vlag; leden blijven false. De oorspronkelijke vlaggen zijn niet meer
            // te herstellen: na de omzetting stonden alle inschrijvingen op het reeksmaximum.
            migrationBuilder.Sql(@"
UPDATE ""Enrollments"" AS e
SET ""IsOpenToGrouping"" = e.""MaxGroupSize"" > CASE WHEN e.""EnrollmentGroupId"" IS NULL THEN 1
    ELSE GREATEST(1, (SELECT COUNT(*) FROM ""Enrollments"" m
                      WHERE m.""EnrollmentGroupId"" = e.""EnrollmentGroupId"" AND m.""Status"" IN (1, 2, 5))) END
WHERE e.""EnrollmentGroupId"" IS NULL
   OR EXISTS (SELECT 1 FROM ""EnrollmentGroups"" g
              WHERE g.""Id"" = e.""EnrollmentGroupId"" AND g.""LeaderEnrollmentId"" = e.""Id"");");

            migrationBuilder.DropColumn(
                name: "MaxGroupSize",
                table: "Enrollments");
        }
    }
}
