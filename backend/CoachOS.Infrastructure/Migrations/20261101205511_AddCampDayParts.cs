using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampDayParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HalfDayPrice",
                table: "Camps",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AllowHalfDays",
                table: "Camps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DayPrice",
                table: "Camps",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "CampEnrollments",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonEnd",
                table: "CampDays",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AfternoonStart",
                table: "CampDays",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningEnd",
                table: "CampDays",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningStart",
                table: "CampDays",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampEnrollmentDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Morning = table.Column<bool>(type: "boolean", nullable: false),
                    Afternoon = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampEnrollmentDays", x => x.Id);
                    table.CheckConstraint("CK_CampEnrollmentDays_AtLeastOnePart", "\"Morning\" OR \"Afternoon\"");
                    table.ForeignKey(
                        name: "FK_CampEnrollmentDays_CampDays_CampDayId",
                        column: x => x.CampDayId,
                        principalTable: "CampDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampEnrollmentDays_CampEnrollments_CampEnrollmentId",
                        column: x => x.CampEnrollmentId,
                        principalTable: "CampEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentDays_CampDayId",
                table: "CampEnrollmentDays",
                column: "CampDayId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentDays_CampEnrollmentId_CampDayId",
                table: "CampEnrollmentDays",
                columns: new[] { "CampEnrollmentId", "CampDayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentDays_OrganizationId",
                table: "CampEnrollmentDays",
                column: "OrganizationId");

            // Bestaande data behouden: elk bestaand kamp wordt "halve dagen niet toegestaan" met de oude
            // hoofdprijs verdeeld over de dagen (enkel voor weergave; de halve-dagprijs gelijk aan de dagprijs
            // zodat een dag met één dagdeel voor de volle dagprijs blijft verkocht worden). De oude uren worden
            // het enige dagdeel (voormiddag), elke bestaande inschrijving krijgt dat dagdeel voor elke kampdag
            // en behoudt exact het bedrag van het oude tarief (of de oude hoofdprijs).
            migrationBuilder.Sql("""
                UPDATE "Camps" c SET
                    "AllowHalfDays" = false,
                    "DayPrice" = ROUND(c."Price" / GREATEST((SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = c."Id"), 1), 2),
                    "HalfDayPrice" = ROUND(c."Price" / GREATEST((SELECT COUNT(*) FROM "CampDays" d WHERE d."CampId" = c."Id"), 1), 2);
                """);

            migrationBuilder.Sql("""
                UPDATE "CampDays" SET "MorningStart" = "StartTime", "MorningEnd" = "EndTime";
                """);

            migrationBuilder.Sql("""
                UPDATE "CampEnrollments" e SET "TotalAmount" = COALESCE(
                    (SELECT p."Amount" FROM "CampPrices" p WHERE p."Id" = e."CampPriceId"),
                    (SELECT c."Price" FROM "Camps" c WHERE c."Id" = e."CampId"),
                    0);
                """);

            migrationBuilder.Sql("""
                INSERT INTO "CampEnrollmentDays" ("Id", "OrganizationId", "CampEnrollmentId", "CampDayId", "Morning", "Afternoon", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), e."OrganizationId", e."Id", d."Id", true, false, now(), now()
                FROM "CampEnrollments" e
                JOIN "CampDays" d ON d."CampId" = e."CampId";
                """);

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

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Camps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampEnrollmentDays");

            migrationBuilder.DropColumn(
                name: "AllowHalfDays",
                table: "Camps");

            migrationBuilder.DropColumn(
                name: "DayPrice",
                table: "Camps");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "CampEnrollments");

            migrationBuilder.DropColumn(
                name: "AfternoonEnd",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "AfternoonStart",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "MorningEnd",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "MorningStart",
                table: "CampDays");

            migrationBuilder.DropColumn(
                name: "HalfDayPrice",
                table: "Camps");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Camps",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CampPriceId",
                table: "CampEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "CampDays",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "CampDays",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "CampPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
    }
}
