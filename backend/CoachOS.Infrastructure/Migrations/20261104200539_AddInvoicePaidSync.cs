using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaidSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InvoiceMarkedPaid",
                table: "SubscriptionPayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "InvoicePaidSyncAttempts",
                table: "SubscriptionPayments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Bestaande betaalde facturen zijn al als betaald aangemaakt.
            migrationBuilder.Sql("""
                UPDATE "SubscriptionPayments" SET "InvoiceMarkedPaid" = TRUE
                WHERE "Status" = 2 AND "InvoiceStatus" = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceMarkedPaid",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "InvoicePaidSyncAttempts",
                table: "SubscriptionPayments");
        }
    }
}
