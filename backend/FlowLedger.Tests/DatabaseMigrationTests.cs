using FluentAssertions;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Tests;

public class DatabaseMigrationTests : IClassFixture<DatabaseMigrationFixture>
{
    private readonly DatabaseMigrationFixture _fixture;

    public DatabaseMigrationTests(DatabaseMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrateAsync_CreatesExpectedTables()
    {
        var expectedTables = new[]
        {
            "Users",
            "Customers",
            "BillingRequests",
            "BillingRequestLineItems",
            "Comments",
            "AuditLogs",
            "Invoices",
            "Notifications",
            "AppSettings"
        };

        var existingTables = new List<string>();

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME IN (
                'Users',
                'Customers',
                'BillingRequests',
                'BillingRequestLineItems',
                'Comments',
                'AuditLogs',
                'Invoices',
                'Notifications',
                'AppSettings'
              )
            ORDER BY TABLE_NAME;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingTables.Add(reader.GetString(0));
        }

        existingTables.Should().BeEquivalentTo(expectedTables);
    }

    [Fact]
    public async Task MigrateAsync_SeedsExpectedBaselineData()
    {
        await using var dbContext = new FlowLedgerDbContext(_fixture.DbContextOptions);

        var statusCounts = await dbContext.BillingRequests
            .GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        (await dbContext.Users.CountAsync()).Should().Be(4);
        (await dbContext.Customers.CountAsync()).Should().Be(6);
        (await dbContext.BillingRequests.CountAsync()).Should().Be(17);
        (await dbContext.BillingRequestLineItems.CountAsync()).Should().Be(17);
        (await dbContext.Invoices.CountAsync()).Should().Be(7);
        (await dbContext.Comments.CountAsync()).Should().Be(3);
        (await dbContext.AuditLogs.CountAsync()).Should().Be(6);
        (await dbContext.Notifications.CountAsync()).Should().Be(3);
        (await dbContext.AppSettings.CountAsync()).Should().Be(1);
        (await dbContext.AppSettings.SingleAsync(x => x.Key == "Jwt.AccessTokenMinutes")).Value.Should().Be("30");

        statusCounts.Should().Contain(BillingRequestStatus.Draft, 2);
        statusCounts.Should().Contain(BillingRequestStatus.AccountsReview, 3);
        statusCounts.Should().Contain(BillingRequestStatus.ManagerApproval, 2);
        statusCounts.Should().Contain(BillingRequestStatus.Rejected, 2);
        statusCounts.Should().Contain(BillingRequestStatus.InvoiceGenerated, 4);
        statusCounts.Should().Contain(BillingRequestStatus.Paid, 3);
        statusCounts.Should().Contain(BillingRequestStatus.Cancelled, 1);
    }

    [Fact]
    public async Task MigrateAsync_SeedsPlannedWorkflowExamples()
    {
        await using var dbContext = new FlowLedgerDbContext(_fixture.DbContextOptions);

        var directApprovalRequest = await dbContext.BillingRequests
            .Include(x => x.Customer)
            .SingleAsync(x => x.RequestNumber == "BR-2026-0004");
        directApprovalRequest.Customer.Name.Should().Be("Fiber Retail Ltd.");
        directApprovalRequest.TotalAmount.Should().Be(45000m);
        directApprovalRequest.Status.Should().Be(BillingRequestStatus.AccountsReview);

        var highValueRequest = await dbContext.BillingRequests
            .Include(x => x.Customer)
            .SingleAsync(x => x.RequestNumber == "BR-2026-0006");
        highValueRequest.Customer.Name.Should().Be("Metro Logistics Bangladesh");
        highValueRequest.TotalAmount.Should().Be(180000m);
        highValueRequest.Status.Should().Be(BillingRequestStatus.AccountsReview);

        var rejectedRequest = await dbContext.BillingRequests
            .Include(x => x.Customer)
            .SingleAsync(x => x.RequestNumber == "BR-2026-0008");
        rejectedRequest.Customer.Name.Should().Be("Northstar Enterprise");
        rejectedRequest.Status.Should().Be(BillingRequestStatus.Rejected);

        var payableInvoice = await dbContext.Invoices
            .SingleAsync(x => x.InvoiceNumber == "INV-2026-0003");
        payableInvoice.Status.Should().Be(InvoiceStatus.Issued);
    }
}
