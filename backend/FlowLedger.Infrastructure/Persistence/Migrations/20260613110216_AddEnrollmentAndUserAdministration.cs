using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentAndUserAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeactivatedByUserId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnrollmentRequestId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "EnrollmentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    RequestedRole = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "DeactivatedAtUtc", "DeactivatedByUserId", "EnrollmentRequestId", "LastLoginAtUtc", "Status", "UpdatedAtUtc" },
                values: new object[] { null, null, null, null, 1, new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "DeactivatedAtUtc", "DeactivatedByUserId", "EnrollmentRequestId", "LastLoginAtUtc", "Status", "UpdatedAtUtc" },
                values: new object[] { null, null, null, null, 1, new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "DeactivatedAtUtc", "DeactivatedByUserId", "EnrollmentRequestId", "LastLoginAtUtc", "Status", "UpdatedAtUtc" },
                values: new object[] { null, null, null, null, 1, new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "DeactivatedAtUtc", "DeactivatedByUserId", "EnrollmentRequestId", "LastLoginAtUtc", "Status", "UpdatedAtUtc" },
                values: new object[] { null, null, null, null, 1, new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentRequests_CreatedAtUtc",
                table: "EnrollmentRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentRequests_Email",
                table: "EnrollmentRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentRequests_ReviewedByUserId",
                table: "EnrollmentRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentRequests_Status",
                table: "EnrollmentRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeactivatedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EnrollmentRequestId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
