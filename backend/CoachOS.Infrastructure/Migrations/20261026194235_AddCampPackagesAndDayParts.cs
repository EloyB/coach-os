using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampPackagesAndDayParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "CampPrices",
                newName: "Discount");

            // Een tarief was een vaste prijs; het wordt een kortingsbedrag op de hoofdprijs van het kamp.
            migrationBuilder.Sql(@"
                UPDATE ""CampPrices"" AS p
                SET ""Discount"" = GREATEST(0, c.""Price"" - p.""Discount"")
                FROM ""Camps"" AS c
                WHERE c.""Id"" = p.""CampId"";");

            migrationBuilder.AddColumn<Guid>(
                name: "CampPackageId",
                table: "CampEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampDayParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampDayParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampDayParts_CampDays_CampDayId",
                        column: x => x.CampDayId,
                        principalTable: "CampDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampPackages_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampPackageSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PartOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampPackageSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampPackageSlots_CampPackages_CampPackageId",
                        column: x => x.CampPackageId,
                        principalTable: "CampPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_CampPackageId",
                table: "CampEnrollments",
                column: "CampPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDayParts_CampDayId",
                table: "CampDayParts",
                column: "CampDayId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDayParts_OrganizationId",
                table: "CampDayParts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampPackages_CampId_SortOrder",
                table: "CampPackages",
                columns: new[] { "CampId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CampPackages_OrganizationId",
                table: "CampPackages",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampPackageSlots_CampPackageId_Date_PartOrder",
                table: "CampPackageSlots",
                columns: new[] { "CampPackageId", "Date", "PartOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampPackageSlots_OrganizationId",
                table: "CampPackageSlots",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampEnrollments_CampPackages_CampPackageId",
                table: "CampEnrollments",
                column: "CampPackageId",
                principalTable: "CampPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampEnrollments_CampPackages_CampPackageId",
                table: "CampEnrollments");

            migrationBuilder.DropTable(
                name: "CampDayParts");

            migrationBuilder.DropTable(
                name: "CampPackageSlots");

            migrationBuilder.DropTable(
                name: "CampPackages");

            migrationBuilder.DropIndex(
                name: "IX_CampEnrollments_CampPackageId",
                table: "CampEnrollments");

            migrationBuilder.DropColumn(
                name: "CampPackageId",
                table: "CampEnrollments");

            migrationBuilder.Sql(@"
                UPDATE ""CampPrices"" AS p
                SET ""Discount"" = GREATEST(0, c.""Price"" - p.""Discount"")
                FROM ""Camps"" AS c
                WHERE c.""Id"" = p.""CampId"";");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "CampPrices",
                newName: "Amount");
        }
    }
}
