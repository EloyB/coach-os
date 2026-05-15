using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminsActAsTrainers = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_OrganizationId",
                table: "OrganizationSettings",
                column: "OrganizationId",
                unique: true);

            // Backfill: bestaande organisaties krijgen een settings-rij met defaults
            // (AdminsActAsTrainers = true). Dit bewaart het historische gedrag waarbij
            // admins automatisch als trainer fungeerden (UserLookupService.IsActiveTrainerAsync
            // accepteerde Admin || Trainer).
            migrationBuilder.Sql(@"
                INSERT INTO ""OrganizationSettings"" (""Id"", ""OrganizationId"", ""AdminsActAsTrainers"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), o.""Id"", true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
                FROM ""Organizations"" o
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""OrganizationSettings"" s WHERE s.""OrganizationId"" = o.""Id""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationSettings");
        }
    }
}
