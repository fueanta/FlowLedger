using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlowLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingRequests_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingRequests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_BillingRequests_BillingRequestId",
                        column: x => x.BillingRequestId,
                        principalTable: "BillingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingRequestLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRequestLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingRequestLineItems_BillingRequests_BillingRequestId",
                        column: x => x.BillingRequestId,
                        principalTable: "BillingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_BillingRequests_BillingRequestId",
                        column: x => x.BillingRequestId,
                        principalTable: "BillingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_BillingRequests_BillingRequestId",
                        column: x => x.BillingRequestId,
                        principalTable: "BillingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "BillingAddress", "ContactEmail", "CreatedAtUtc", "Name", "Phone" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "House 11, Road 7, Dhaka", "billing@fiberretail.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Fiber Retail Ltd.", "+8801700000001" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Port Road, Chattogram", "finance@metrologistics.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Metro Logistics Bangladesh", "+8801700000002" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Airport Road, Dhaka", "accounts@northstar.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Northstar Enterprise", "+8801700000003" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "Industrial Area, Gazipur", "billing@greenline.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Greenline Distribution", "+8801700000004" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "Banani, Dhaka", "finance@bluepeak.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "BluePeak Systems", "+8801700000005" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), "Motijheel, Dhaka", "accounts@easterntrading.local", new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Eastern Trading Co.", "+8801700000006" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAtUtc", "Email", "FullName", "IsActive", "Role" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "sales@flowledger.local", "Sarah Sales", true, 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "accounts@flowledger.local", "Amir Accounts", true, 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "manager@flowledger.local", "Mona Manager", true, 3 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 1, 5, 9, 0, 0, 0, DateTimeKind.Utc), "admin@flowledger.local", "Adam Admin", true, 4 }
                });

            migrationBuilder.InsertData(
                table: "BillingRequests",
                columns: new[] { "Id", "ApprovedAtUtc", "AssignedToUserId", "CreatedAtUtc", "CreatedByUserId", "CustomerId", "Description", "RejectedAtUtc", "RequestNumber", "Status", "SubmittedAtUtc", "SubtotalAmount", "Title", "TotalAmount", "UpdatedAtUtc", "VatAmount" },
                values: new object[,]
                {
                    { new Guid("99999999-9999-9999-9999-000000000001"), null, null, new DateTime(2026, 1, 6, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Retail starter billing for seeded workflow testing.", null, "BR-2026-0001", 1, null, 10434.78m, "Retail starter billing", 12000m, new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Utc), 1565.22m },
                    { new Guid("99999999-9999-9999-9999-000000000002"), null, null, new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Monthly logistics support for seeded workflow testing.", null, "BR-2026-0002", 1, null, 67826.09m, "Monthly logistics support", 78000m, new DateTime(2026, 1, 8, 9, 0, 0, 0, DateTimeKind.Utc), 10173.91m },
                    { new Guid("99999999-9999-9999-9999-000000000003"), null, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 8, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "Small distribution order for seeded workflow testing.", null, "BR-2026-0003", 3, new DateTime(2026, 1, 9, 9, 0, 0, 0, DateTimeKind.Utc), 24347.83m, "Small distribution order", 28000m, new DateTime(2026, 1, 9, 9, 0, 0, 0, DateTimeKind.Utc), 3652.17m },
                    { new Guid("99999999-9999-9999-9999-000000000004"), null, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 9, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Fiber Retail service package for seeded workflow testing.", null, "BR-2026-0004", 3, new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), 39130.43m, "Fiber Retail service package", 45000m, new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), 5869.57m },
                    { new Guid("99999999-9999-9999-9999-000000000005"), null, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "Platform implementation advance for seeded workflow testing.", null, "BR-2026-0005", 4, new DateTime(2026, 1, 11, 9, 0, 0, 0, DateTimeKind.Utc), 108695.65m, "Platform implementation advance", 125000m, new DateTime(2026, 1, 11, 9, 0, 0, 0, DateTimeKind.Utc), 16304.35m },
                    { new Guid("99999999-9999-9999-9999-000000000006"), null, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 11, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Metro Logistics annual support for seeded workflow testing.", null, "BR-2026-0006", 3, new DateTime(2026, 1, 12, 9, 0, 0, 0, DateTimeKind.Utc), 156521.74m, "Metro Logistics annual support", 180000m, new DateTime(2026, 1, 12, 9, 0, 0, 0, DateTimeKind.Utc), 23478.26m },
                    { new Guid("99999999-9999-9999-9999-000000000007"), null, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 12, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), "Enterprise procurement billing for seeded workflow testing.", null, "BR-2026-0007", 4, new DateTime(2026, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), 217391.30m, "Enterprise procurement billing", 250000m, new DateTime(2026, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), 32608.70m },
                    { new Guid("99999999-9999-9999-9999-000000000008"), null, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 13, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Northstar revised billing for seeded workflow testing.", new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), "BR-2026-0008", 6, new DateTime(2026, 1, 14, 9, 0, 0, 0, DateTimeKind.Utc), 56521.74m, "Northstar revised billing", 65000m, new DateTime(2026, 1, 14, 9, 0, 0, 0, DateTimeKind.Utc), 8478.26m },
                    { new Guid("99999999-9999-9999-9999-000000000009"), null, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 14, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "Greenline returned billing for seeded workflow testing.", new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), "BR-2026-0009", 6, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc), 30434.78m, "Greenline returned billing", 35000m, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc), 4565.22m },
                    { new Guid("99999999-9999-9999-9999-000000000010"), new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "BluePeak subscription billing for seeded workflow testing.", null, "BR-2026-0010", 7, new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), 45217.39m, "BluePeak subscription billing", 52000m, new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), 6782.61m },
                    { new Guid("99999999-9999-9999-9999-000000000011"), new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Fiber Retail replenishment for seeded workflow testing.", null, "BR-2026-0011", 7, new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), 80000m, "Fiber Retail replenishment", 92000m, new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), 12000m },
                    { new Guid("99999999-9999-9999-9999-000000000012"), new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Metro Logistics freight billing for seeded workflow testing.", null, "BR-2026-0012", 7, new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), 73913.04m, "Metro Logistics freight billing", 85000m, new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), 11086.96m },
                    { new Guid("99999999-9999-9999-9999-000000000013"), new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), "Eastern Trading supply billing for seeded workflow testing.", null, "BR-2026-0013", 7, new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), 121739.13m, "Eastern Trading supply billing", 140000m, new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), 18260.87m },
                    { new Guid("99999999-9999-9999-9999-000000000014"), new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Northstar completion billing for seeded workflow testing.", null, "BR-2026-0014", 8, new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), 27826.09m, "Northstar completion billing", 32000m, new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), 4173.91m },
                    { new Guid("99999999-9999-9999-9999-000000000015"), new DateTime(2026, 1, 22, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "Greenline delivery billing for seeded workflow testing.", null, "BR-2026-0015", 8, new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), 76521.74m, "Greenline delivery billing", 88000m, new DateTime(2026, 1, 24, 9, 0, 0, 0, DateTimeKind.Utc), 11478.26m },
                    { new Guid("99999999-9999-9999-9999-000000000016"), new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "BluePeak enterprise billing for seeded workflow testing.", null, "BR-2026-0016", 8, new DateTime(2026, 1, 22, 9, 0, 0, 0, DateTimeKind.Utc), 304347.83m, "BluePeak enterprise billing", 350000m, new DateTime(2026, 1, 25, 9, 0, 0, 0, DateTimeKind.Utc), 45652.17m },
                    { new Guid("99999999-9999-9999-9999-000000000017"), null, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 22, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Cancelled duplicate request for seeded workflow testing.", null, "BR-2026-0017", 9, null, 34782.61m, "Cancelled duplicate request", 40000m, new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), 5217.39m }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAtUtc", "IsRead", "Message", "Title", "UserId" },
                values: new object[,]
                {
                    { new Guid("ffffffff-ffff-ffff-ffff-000000000001"), new DateTime(2026, 1, 6, 9, 0, 0, 0, DateTimeKind.Utc), false, "BR-2026-0004 is waiting for Accounts review.", "Request ready for review", new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("ffffffff-ffff-ffff-ffff-000000000002"), new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Utc), false, "BR-2026-0007 is waiting for Management approval.", "Manager approval needed", new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("ffffffff-ffff-ffff-ffff-000000000003"), new DateTime(2026, 1, 8, 9, 0, 0, 0, DateTimeKind.Utc), false, "BR-2026-0008 needs revision.", "Request rejected", new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "ActionType", "ActorUserId", "BillingRequestId", "CreatedAtUtc", "Message", "MetadataJson" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000001"), 1, new Guid("11111111-1111-1111-1111-111111111111"), new Guid("99999999-9999-9999-9999-000000000001"), new DateTime(2026, 1, 6, 11, 0, 0, 0, DateTimeKind.Utc), "Billing request created.", null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000002"), 3, new Guid("11111111-1111-1111-1111-111111111111"), new Guid("99999999-9999-9999-9999-000000000004"), new DateTime(2026, 1, 9, 11, 0, 0, 0, DateTimeKind.Utc), "Billing request submitted to Accounts.", null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000003"), 3, new Guid("11111111-1111-1111-1111-111111111111"), new Guid("99999999-9999-9999-9999-000000000006"), new DateTime(2026, 1, 11, 11, 0, 0, 0, DateTimeKind.Utc), "Billing request submitted to Accounts.", null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000004"), 5, new Guid("22222222-2222-2222-2222-222222222222"), new Guid("99999999-9999-9999-9999-000000000008"), new DateTime(2026, 1, 13, 11, 0, 0, 0, DateTimeKind.Utc), "Accounts rejected request for revision.", null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000005"), 7, new Guid("22222222-2222-2222-2222-222222222222"), new Guid("99999999-9999-9999-9999-000000000010"), new DateTime(2026, 1, 15, 11, 0, 0, 0, DateTimeKind.Utc), "Invoice generated after Accounts approval.", null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-000000000006"), 8, new Guid("22222222-2222-2222-2222-222222222222"), new Guid("99999999-9999-9999-9999-000000000014"), new DateTime(2026, 1, 19, 11, 0, 0, 0, DateTimeKind.Utc), "Invoice marked as paid.", null }
                });

            migrationBuilder.InsertData(
                table: "BillingRequestLineItems",
                columns: new[] { "Id", "BillingRequestId", "Description", "LineTotal", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000001"), new Guid("99999999-9999-9999-9999-000000000001"), "Retail starter billing line item", 10434.78m, 1, 10434.78m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000002"), new Guid("99999999-9999-9999-9999-000000000002"), "Monthly logistics support line item", 67826.09m, 1, 67826.09m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000003"), new Guid("99999999-9999-9999-9999-000000000003"), "Small distribution order line item", 24347.83m, 1, 24347.83m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000004"), new Guid("99999999-9999-9999-9999-000000000004"), "Fiber Retail service package line item", 39130.43m, 1, 39130.43m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000005"), new Guid("99999999-9999-9999-9999-000000000005"), "Platform implementation advance line item", 108695.65m, 1, 108695.65m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000006"), new Guid("99999999-9999-9999-9999-000000000006"), "Metro Logistics annual support line item", 156521.74m, 1, 156521.74m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000007"), new Guid("99999999-9999-9999-9999-000000000007"), "Enterprise procurement billing line item", 217391.30m, 1, 217391.30m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000008"), new Guid("99999999-9999-9999-9999-000000000008"), "Northstar revised billing line item", 56521.74m, 1, 56521.74m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000009"), new Guid("99999999-9999-9999-9999-000000000009"), "Greenline returned billing line item", 30434.78m, 1, 30434.78m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000010"), new Guid("99999999-9999-9999-9999-000000000010"), "BluePeak subscription billing line item", 45217.39m, 1, 45217.39m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000011"), new Guid("99999999-9999-9999-9999-000000000011"), "Fiber Retail replenishment line item", 80000m, 1, 80000m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000012"), new Guid("99999999-9999-9999-9999-000000000012"), "Metro Logistics freight billing line item", 73913.04m, 1, 73913.04m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000013"), new Guid("99999999-9999-9999-9999-000000000013"), "Eastern Trading supply billing line item", 121739.13m, 1, 121739.13m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000014"), new Guid("99999999-9999-9999-9999-000000000014"), "Northstar completion billing line item", 27826.09m, 1, 27826.09m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000015"), new Guid("99999999-9999-9999-9999-000000000015"), "Greenline delivery billing line item", 76521.74m, 1, 76521.74m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000016"), new Guid("99999999-9999-9999-9999-000000000016"), "BluePeak enterprise billing line item", 304347.83m, 1, 304347.83m },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000017"), new Guid("99999999-9999-9999-9999-000000000017"), "Cancelled duplicate request line item", 34782.61m, 1, 34782.61m }
                });

            migrationBuilder.InsertData(
                table: "Comments",
                columns: new[] { "Id", "AuthorUserId", "BillingRequestId", "Body", "CreatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("dddddddd-dddd-dddd-dddd-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("99999999-9999-9999-9999-000000000008"), "Rejected pending corrected purchase order.", new DateTime(2026, 1, 13, 13, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("dddddddd-dddd-dddd-dddd-000000000002"), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("99999999-9999-9999-9999-000000000009"), "Rejected because billed amount needs clarification.", new DateTime(2026, 1, 14, 13, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("dddddddd-dddd-dddd-dddd-000000000003"), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("99999999-9999-9999-9999-000000000006"), "High-value request needs management approval after accounts review.", new DateTime(2026, 1, 11, 13, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Invoices",
                columns: new[] { "Id", "BillingRequestId", "CustomerId", "DueAtUtc", "InvoiceNumber", "IssuedAtUtc", "PaidAtUtc", "Status", "SubtotalAmount", "TotalAmount", "VatAmount" },
                values: new object[,]
                {
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000001"), new Guid("99999999-9999-9999-9999-000000000010"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), new DateTime(2026, 2, 16, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0001", new DateTime(2026, 1, 17, 9, 0, 0, 0, DateTimeKind.Utc), null, 2, 45217.39m, 52000m, 6782.61m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000002"), new Guid("99999999-9999-9999-9999-000000000011"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), new DateTime(2026, 2, 17, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0002", new DateTime(2026, 1, 18, 9, 0, 0, 0, DateTimeKind.Utc), null, 2, 80000m, 92000m, 12000m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000003"), new Guid("99999999-9999-9999-9999-000000000012"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), new DateTime(2026, 2, 18, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0003", new DateTime(2026, 1, 19, 9, 0, 0, 0, DateTimeKind.Utc), null, 2, 73913.04m, 85000m, 11086.96m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000004"), new Guid("99999999-9999-9999-9999-000000000013"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), new DateTime(2026, 2, 19, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0004", new DateTime(2026, 1, 20, 9, 0, 0, 0, DateTimeKind.Utc), null, 2, 121739.13m, 140000m, 18260.87m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000005"), new Guid("99999999-9999-9999-9999-000000000014"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), new DateTime(2026, 2, 20, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0005", new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 29, 9, 0, 0, 0, DateTimeKind.Utc), 3, 27826.09m, 32000m, 4173.91m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000006"), new Guid("99999999-9999-9999-9999-000000000015"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), new DateTime(2026, 2, 21, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0006", new DateTime(2026, 1, 22, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 29, 9, 0, 0, 0, DateTimeKind.Utc), 3, 76521.74m, 88000m, 11478.26m },
                    { new Guid("cccccccc-cccc-cccc-cccc-000000000007"), new Guid("99999999-9999-9999-9999-000000000016"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), new DateTime(2026, 2, 22, 9, 0, 0, 0, DateTimeKind.Utc), "INV-2026-0007", new DateTime(2026, 1, 23, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 28, 9, 0, 0, 0, DateTimeKind.Utc), 3, 304347.83m, 350000m, 45652.17m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BillingRequestId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "BillingRequestId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequestLineItems_BillingRequestId",
                table: "BillingRequestLineItems",
                column: "BillingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_AssignedToUserId",
                table: "BillingRequests",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_CreatedAtUtc",
                table: "BillingRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_CreatedByUserId",
                table: "BillingRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_CustomerId",
                table: "BillingRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_RequestNumber",
                table: "BillingRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingRequests_Status",
                table: "BillingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorUserId",
                table: "Comments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BillingRequestId",
                table: "Comments",
                column: "BillingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillingRequestId",
                table: "Invoices",
                column: "BillingRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BillingRequestLineItems");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "BillingRequests");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
