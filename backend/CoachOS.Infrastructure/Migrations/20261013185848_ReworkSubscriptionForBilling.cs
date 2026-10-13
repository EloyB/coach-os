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

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastCheckoutUrl",
                table: "Subscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaidAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPaymentId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPaymentStatus",
                table: "Subscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastCheckoutUrl",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaidAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaymentId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaymentStatus",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Subscriptions",
                newName: "MonthlyPrice");
        }
    }
}
