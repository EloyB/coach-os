using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePricingModeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Mode_GroupSize_Category",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "GroupSize",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "LessonSeriePrices");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_SortOrder",
                table: "LessonSeriePrices",
                columns: new[] { "LessonSerieId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_SortOrder",
                table: "LessonSeriePrices");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupSize",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Mode_GroupSize_Category",
                table: "LessonSeriePrices",
                columns: new[] { "LessonSerieId", "Mode", "GroupSize", "Category" });
        }
    }
}
