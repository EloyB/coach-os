using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactEmailToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LessonSerieId_StudentEmail",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentEmail",
                table: "Enrollments");

            migrationBuilder.AlterColumn<string>(
                name: "StudentEmail",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            // Eerst nullable toevoegen en backfillen vanuit het bestaande StudentEmail;
            // pas daarna verplicht maken. Een defaultValue "" zou bestaande rijen een
            // leeg contactadres geven en de partiële unique index hieronder kan pas
            // kloppen als het adres echt gevuld is.
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Enrollments\" SET \"ContactEmail\" = lower(btrim(\"StudentEmail\"));");

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNameNormalized",
                table: "Enrollments",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(btrim(\"StudentName\"))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ContactEmail",
                table: "Enrollments",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Participant",
                table: "Enrollments",
                columns: new[] { "LessonSerieId", "ContactEmail", "StudentNameNormalized", "DateOfBirth" },
                unique: true,
                filter: "\"DateOfBirth\" IS NOT NULL AND \"Status\" IN (1, 2, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ContactEmail",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_Participant",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StudentNameNormalized",
                table: "Enrollments");

            // StudentEmail wordt weer verplicht: vul lege adressen (leden die de
            // communicatie via de leider lieten lopen) met het contactadres, anders
            // faalt de NOT NULL-constraint.
            migrationBuilder.Sql(
                "UPDATE \"Enrollments\" SET \"StudentEmail\" = \"ContactEmail\" WHERE \"StudentEmail\" IS NULL;");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Enrollments");

            migrationBuilder.AlterColumn<string>(
                name: "StudentEmail",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LessonSerieId_StudentEmail",
                table: "Enrollments",
                columns: new[] { "LessonSerieId", "StudentEmail" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentEmail",
                table: "Enrollments",
                column: "StudentEmail");
        }
    }
}
