using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowAssignmentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountsReviewedByUserId",
                table: "BillingRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "BillingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedQueue",
                table: "BillingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWorkflowActionAtUtc",
                table: "BillingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerReviewedByUserId",
                table: "BillingRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "BillingRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000001"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 6, 9, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000002"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 8, 9, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000003"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 9, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 1, 9, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000004"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000005"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 11, 9, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 1, 11, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000006"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 12, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 1, 12, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000007"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000008"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000009"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000010"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000011"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000012"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000013"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000014"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000015"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 22, 9, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000016"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, 0, null, new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000017"),
                columns: new[] { "AccountsReviewedByUserId", "AssignedAtUtc", "AssignedQueue", "AssignedToUserId", "LastWorkflowActionAtUtc", "ManagerReviewedByUserId", "SubmittedByUserId" },
                values: new object[] { null, null, 0, null, new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_AssignedQueue",
                table: "BillingRequests",
                column: "AssignedQueue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingRequests_AssignedQueue",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "AccountsReviewedByUserId",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "AssignedQueue",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "LastWorkflowActionAtUtc",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "ManagerReviewedByUserId",
                table: "BillingRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "BillingRequests");

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000010"),
                column: "AssignedToUserId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000011"),
                column: "AssignedToUserId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000012"),
                column: "AssignedToUserId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000013"),
                column: "AssignedToUserId",
                value: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000014"),
                column: "AssignedToUserId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000015"),
                column: "AssignedToUserId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000016"),
                column: "AssignedToUserId",
                value: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.UpdateData(
                table: "BillingRequests",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-000000000017"),
                column: "AssignedToUserId",
                value: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
