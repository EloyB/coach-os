using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexiblePricingOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Category_GroupSize",
                table: "LessonSeriePrices");

            migrationBuilder.AlterColumn<int>(
                name: "GroupSize",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LessonSeriePrices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "LessonSeriePrices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "ReusableKey",
                table: "LessonSeriePrices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedPriceOptionId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "LessonSeriePrices"
                SET "Label" = CASE "Category"
                    WHEN 1 THEN 'Jeugd'
                    WHEN 2 THEN 'Volwassenen'
                    ELSE 'Prijsoptie'
                END,
                "Description" = CASE
                    WHEN "GroupSize" IS NOT NULL THEN 'Gemigreerd uit de vorige groepsgrootte-prijsmatrix.'
                    ELSE NULL
                END,
                "SortOrder" = COALESCE("GroupSize", 0)
                WHERE "Label" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Mode_GroupSize_Category",
                table: "LessonSeriePrices",
                columns: new[] { "LessonSerieId", "Mode", "GroupSize", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_OrganizationId_ReusableKey",
                table: "LessonSeriePrices",
                columns: new[] { "OrganizationId", "ReusableKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Mode_GroupSize_Category",
                table: "LessonSeriePrices");

            migrationBuilder.DropIndex(
                name: "IX_LessonSeriePrices_OrganizationId_ReusableKey",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "ReusableKey",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "LessonSeriePrices");

            migrationBuilder.DropColumn(
                name: "SelectedPriceOptionId",
                table: "Enrollments");

            migrationBuilder.AlterColumn<int>(
                name: "GroupSize",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "LessonSeriePrices",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Category_GroupSize",
                table: "LessonSeriePrices",
                columns: new[] { "LessonSerieId", "Category", "GroupSize" },
                unique: true);
        }
    }
}
