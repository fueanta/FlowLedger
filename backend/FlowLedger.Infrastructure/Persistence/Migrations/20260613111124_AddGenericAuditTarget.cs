using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenericAuditTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BillingRequestId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "AuditLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "System");

            migrationBuilder.AddColumn<string>(
                name: "AfterStatus",
                table: "AuditLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeStatus",
                table: "AuditLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EntityNumber",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "BillingRequest");

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000001"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Sarah Sales", null, null, new Guid("99999999-9999-9999-9999-000000000001"), "BR-2026-0001", "BillingRequest" });

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000002"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Sarah Sales", null, null, new Guid("99999999-9999-9999-9999-000000000004"), "BR-2026-0004", "BillingRequest" });

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000003"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Sarah Sales", null, null, new Guid("99999999-9999-9999-9999-000000000006"), "BR-2026-0006", "BillingRequest" });

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000004"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Amir Accounts", null, null, new Guid("99999999-9999-9999-9999-000000000008"), "BR-2026-0008", "BillingRequest" });

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000005"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Amir Accounts", null, null, new Guid("99999999-9999-9999-9999-000000000010"), "BR-2026-0010", "BillingRequest" });

            migrationBuilder.UpdateData(
                table: "AuditLogs",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-000000000006"),
                columns: new[] { "ActorDisplayName", "AfterStatus", "BeforeStatus", "EntityId", "EntityNumber", "EntityType" },
                values: new object[] { "Amir Accounts", null, null, new Guid("99999999-9999-9999-9999-000000000014"), "BR-2026-0014", "BillingRequest" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AfterStatus",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BeforeStatus",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityNumber",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "BillingRequestId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
