using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampPriceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampPriceId",
                table: "CampEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampPrices_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampPrices_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_CampPriceId",
                table: "CampEnrollments",
                column: "CampPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_CampPrices_CampId_SortOrder",
                table: "CampPrices",
                columns: new[] { "CampId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CampPrices_OrganizationId",
                table: "CampPrices",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampEnrollments_CampPrices_CampPriceId",
                table: "CampEnrollments",
                column: "CampPriceId",
                principalTable: "CampPrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampEnrollments_CampPrices_CampPriceId",
                table: "CampEnrollments");

            migrationBuilder.DropTable(
                name: "CampPrices");

            migrationBuilder.DropIndex(
                name: "IX_CampEnrollments_CampPriceId",
                table: "CampEnrollments");

            migrationBuilder.DropColumn(
                name: "CampPriceId",
                table: "CampEnrollments");
        }
    }
}
