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
