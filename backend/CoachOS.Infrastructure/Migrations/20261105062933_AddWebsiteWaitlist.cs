using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebsiteWaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    UnsubscribeTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteWaitlistEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteWaitlistMails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteWaitlistMails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebsiteWaitlistMails_WebsiteWaitlistEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "WebsiteWaitlistEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteWaitlistEntries_Email",
                table: "WebsiteWaitlistEntries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteWaitlistEntries_UnsubscribeTokenHash",
                table: "WebsiteWaitlistEntries",
                column: "UnsubscribeTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteWaitlistMails_AvailableAt",
                table: "WebsiteWaitlistMails",
                column: "AvailableAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteWaitlistMails_EntryId",
                table: "WebsiteWaitlistMails",
                column: "EntryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebsiteWaitlistMails");

            migrationBuilder.DropTable(
                name: "WebsiteWaitlistEntries");
        }
    }
}
