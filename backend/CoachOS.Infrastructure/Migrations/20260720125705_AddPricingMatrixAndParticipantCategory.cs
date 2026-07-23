using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingMatrixAndParticipantCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YouthMaxAge",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 17);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Enrollments",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LessonSeriePrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonSerieId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    GroupSize = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSeriePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSeriePrices_LessonSeries_LessonSerieId",
                        column: x => x.LessonSerieId,
                        principalTable: "LessonSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonSeriePrices_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // LET OP — handmatig verwijderd: EF genereerde hier een CreateTable voor
            // "TrainerAvailabilities" plus drie bijhorende indexen. Die tabel bestaat
            // al sinds migratie 20260613045047_AddTrainerAvailability. De oorzaak is
            // dat 20260613160526_AddCampsModule.Designer.cs de entiteit niet bevat,
            // waardoor EF hem als "nieuw" ziet bij het diffen. De model-snapshot is
            // vanaf deze migratie weer correct, dus dit is eenmalig.

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_OrganizationId_Date_CourtName",
                table: "Lessons",
                columns: new[] { "OrganizationId", "Date", "CourtName" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_LessonSerieId_Category_GroupSize",
                table: "LessonSeriePrices",
                columns: new[] { "LessonSerieId", "Category", "GroupSize" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSeriePrices_OrganizationId",
                table: "LessonSeriePrices",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonSeriePrices");

            // Bewust GEEN DropTable voor "TrainerAvailabilities": deze migratie
            // maakt die tabel niet aan (zie toelichting in Up), dus terugdraaien
            // mag hem niet weggooien — dat zou bestaande beschikbaarheden wissen.

            migrationBuilder.DropIndex(
                name: "IX_Lessons_OrganizationId_Date_CourtName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "YouthMaxAge",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Enrollments");
        }
    }
}
