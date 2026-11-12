using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOutboxRecipientEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "EmailOutboxMessages",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_RecipientEmail",
                table: "EmailOutboxMessages",
                column: "RecipientEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailOutboxMessages_RecipientEmail",
                table: "EmailOutboxMessages");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "EmailOutboxMessages");
        }
    }
}
