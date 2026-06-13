using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Customers",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Customers",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TaxIdentifier",
                table: "Customers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Customers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Nadia Rahman", 1, "TIN-FIBER-001", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Tariq Hasan", 1, "TIN-METRO-002", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Rumana Islam", 1, "TIN-NORTH-003", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Farhan Kabir", 1, "TIN-GREEN-004", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Mahira Chowdhury", 1, "TIN-BLUE-005", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"),
                columns: new[] { "ArchivedAtUtc", "ArchivedByUserId", "ContactPerson", "Status", "TaxIdentifier", "UpdatedAtUtc" },
                values: new object[] { null, null, "Sabbir Ahmed", 1, "TIN-EAST-006", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ContactEmail",
                table: "Customers",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Status",
                table: "Customers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_ContactEmail",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Status",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxIdentifier",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "Customers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
