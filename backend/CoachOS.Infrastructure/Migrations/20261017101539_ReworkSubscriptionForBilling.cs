using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkSubscriptionForBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonthlyPrice",
                table: "Subscriptions",
                newName: "Price");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CancelAt",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CurrentPeriodEnd",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CurrentPeriodStart",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeal",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OverageMarginPercent",
                table: "Subscriptions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OveragePricePerStudent",
                table: "Subscriptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentLimit",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    InvoiceEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VatNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VatVerified = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountExclVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    AmountInclVat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MolliePaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceStatus = table.Column<int>(type: "integer", nullable: false),
                    InvoiceAttempts = table.Column<int>(type: "integer", nullable: false),
                    InvoiceNextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoicePdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InvoiceLastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_MollieSubscriptionId",
                table: "Subscriptions",
                column: "MollieSubscriptionId",
                unique: true,
                filter: "\"MollieSubscriptionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_OrganizationId",
                table: "BillingProfiles",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_InvoiceStatus",
                table: "SubscriptionPayments",
                column: "InvoiceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_MolliePaymentId",
                table: "SubscriptionPayments",
                column: "MolliePaymentId",
                unique: true,
                filter: "\"MolliePaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_OrganizationId",
                table: "SubscriptionPayments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_SubscriptionId_PeriodStart",
                table: "SubscriptionPayments",
                columns: new[] { "SubscriptionId", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingProfiles");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_MollieSubscriptionId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CancelAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEnd",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodStart",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeal",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OverageMarginPercent",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OveragePricePerStudent",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StudentLimit",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Subscriptions",
                newName: "MonthlyPrice");
        }
    }
}
