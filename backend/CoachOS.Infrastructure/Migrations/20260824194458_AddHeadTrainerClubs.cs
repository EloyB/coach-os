using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeadTrainerClubs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeadTrainerClubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    TennisClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadTrainerClubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadTrainerClubs_OrganizationMemberships_OrganizationMember~",
                        column: x => x.OrganizationMembershipId,
                        principalTable: "OrganizationMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeadTrainerClubs_TennisClubs_TennisClubId",
                        column: x => x.TennisClubId,
                        principalTable: "TennisClubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeadTrainerClubs_OrganizationMembershipId_TennisClubId",
                table: "HeadTrainerClubs",
                columns: new[] { "OrganizationMembershipId", "TennisClubId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeadTrainerClubs_TennisClubId",
                table: "HeadTrainerClubs",
                column: "TennisClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeadTrainerClubs");
        }
    }
}
