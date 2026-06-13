using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableBillingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueDays",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "VatPercentage",
                table: "Invoices",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 15m);

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Description", "Value" },
                values: new object[,]
                {
                    { "Billing.InvoiceDueDays", "Number of days after issue date used for new invoice due dates.", "30" },
                    { "Billing.ManagerApprovalThreshold", "Total amount above which Accounts approval routes to Management.", "100000" },
                    { "Billing.VatPercentage", "VAT percentage used for new billing request totals.", "15" }
                });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000001"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000002"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000003"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000004"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000005"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000006"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });

            migrationBuilder.UpdateData(
                table: "Invoices",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-000000000007"),
                columns: new[] { "DueDays", "VatPercentage" },
                values: new object[] { 30, 15m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Billing.InvoiceDueDays");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Billing.ManagerApprovalThreshold");

            migrationBuilder.DeleteData(
                table: "AppSettings",
                keyColumn: "Key",
                keyValue: "Billing.VatPercentage");

            migrationBuilder.DropColumn(
                name: "DueDays",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VatPercentage",
                table: "Invoices");
        }
    }
}
