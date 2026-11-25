using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormFieldScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "FormFields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Bestaande vragen worden "algemeen" (1 = PerEnrollment): tot nu toe beantwoordde enkel de leider de extra
            // vragen, en zo blijft dat gedrag voor bestaande formulieren. Nieuwe vragen zijn standaard "per speler" (0).
            migrationBuilder.Sql("UPDATE \"FormFields\" SET \"Scope\" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "FormFields");
        }
    }
}
