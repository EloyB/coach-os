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

            // 2. Bestaande inschrijvingen omzetten zodat de planning zich gedraagt zoals de keuze van toen:
            //    - niet open voor groepering: het maximum is de eigen groepsgrootte (solo = 1);
            //    - wel open voor groepering: het maximum van de reeks (grootste weekslot, hoogstens 4), maar nooit
            //      lager dan de eigen groep.
            //    Voor een groep telt de keuze van de leider voor iedereen (alleen de leider had de vlag). Actief =
            //    Pending (1), Confirmed (2) of PendingPayment (5).
            migrationBuilder.Sql(@"
UPDATE ""Enrollments"" AS e
SET ""MaxGroupSize"" = LEAST(4, GREATEST(sz.own_size, CASE WHEN sz.is_open THEN sz.series_max ELSE 1 END))
FROM (
    SELECT
        e2.""Id"" AS id,
        CASE WHEN e2.""EnrollmentGroupId"" IS NULL THEN 1
             ELSE GREATEST(1, (SELECT COUNT(*) FROM ""Enrollments"" m
                               WHERE m.""EnrollmentGroupId"" = e2.""EnrollmentGroupId"" AND m.""Status"" IN (1, 2, 5)))
        END AS own_size,
        CASE WHEN e2.""EnrollmentGroupId"" IS NULL THEN e2.""IsOpenToGrouping""
             ELSE COALESCE((SELECT l.""IsOpenToGrouping"" FROM ""EnrollmentGroups"" g
                            JOIN ""Enrollments"" l ON l.""Id"" = g.""LeaderEnrollmentId""
                            WHERE g.""Id"" = e2.""EnrollmentGroupId""), e2.""IsOpenToGrouping"")
        END AS is_open,
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
            // solo of een groepsleider de vlag; leden blijven false.
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
