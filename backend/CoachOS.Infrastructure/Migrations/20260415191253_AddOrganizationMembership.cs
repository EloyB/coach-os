using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId",
                table: "OrganizationMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId_OrganizationId",
                table: "OrganizationMemberships",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            // Backfill: maak voor elke bestaande user met OrganizationId een membership aan.
            // Rol komt uit ApplicationUser.Role, IsActive volgt de user. ApplicationUser.OrganizationId
            // blijft voorlopig bestaan en wordt in een latere migratie verwijderd.
            migrationBuilder.Sql("""
                INSERT INTO "OrganizationMemberships" (
                    "Id", "UserId", "OrganizationId", "Role", "IsActive",
                    "JoinedAt", "CreatedAt", "UpdatedAt"
                )
                SELECT
                    gen_random_uuid(),
                    u."Id",
                    u."OrganizationId",
                    u."Role",
                    u."IsActive",
                    COALESCE(u."CreatedAt", NOW()),
                    NOW(),
                    NOW()
                FROM "AspNetUsers" u
                WHERE u."OrganizationId" IS NOT NULL
                ON CONFLICT ("UserId", "OrganizationId") DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationMemberships");
        }
    }
}
