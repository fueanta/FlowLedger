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
        (await dbContext.AppSettings.CountAsync()).Should().Be(4);
        (await dbContext.AppSettings.SingleAsync(x => x.Key == "Jwt.AccessTokenMinutes")).Value.Should().Be("30");
        (await dbContext.AppSettings.SingleAsync(x => x.Key == "Billing.VatPercentage")).Value.Should().Be("15");
        (await dbContext.AppSettings.SingleAsync(x => x.Key == "Billing.ManagerApprovalThreshold")).Value.Should().Be("100000");
        (await dbContext.AppSettings.SingleAsync(x => x.Key == "Billing.InvoiceDueDays")).Value.Should().Be("30");

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

    [Fact]
    public async Task MigrateAsync_ConfiguresTemporalTables()
    {
        var expectedHistoryTables = new Dictionary<string, string>
        {
            ["Customers"] = "CustomersHistory",
            ["BillingRequests"] = "BillingRequestsHistory",
            ["Invoices"] = "InvoicesHistory",
            ["AppSettings"] = "AppSettingsHistory"
        };
        var rows = new Dictionary<string, (int TemporalType, string HistoryTable)>();

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.name, t.temporal_type, h.name AS history_table_name
            FROM sys.tables t
            LEFT JOIN sys.tables h ON t.history_table_id = h.object_id
            WHERE t.name IN ('Customers', 'BillingRequests', 'Invoices', 'AppSettings');
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows[reader.GetString(0)] = (reader.GetByte(1), reader.GetString(2));
        }

        rows.Keys.Should().BeEquivalentTo(expectedHistoryTables.Keys);
        foreach (var (tableName, historyTableName) in expectedHistoryTables)
        {
            rows[tableName].TemporalType.Should().Be(2);
            rows[tableName].HistoryTable.Should().Be(historyTableName);
        }
    }

    [Fact]
    public async Task MigrateAsync_WritesTemporalHistoryForTrackedTables()
    {
        await using var dbContext = new FlowLedgerDbContext(_fixture.DbContextOptions);

        var client = await dbContext.Customers.SingleAsync(x => x.ContactEmail == "billing@fiberretail.local");
        var originalClientName = client.Name;
        client.Name = "Fiber Retail Temporal";

        var request = await dbContext.BillingRequests.SingleAsync(x => x.RequestNumber == "BR-2026-0001");
        var originalRequestTitle = request.Title;
        request.Title = "Temporal request title";

        var invoice = await dbContext.Invoices.SingleAsync(x => x.InvoiceNumber == "INV-2026-0001");
        var originalDueDays = invoice.DueDays;
        invoice.DueDays += 1;

        var setting = await dbContext.AppSettings.SingleAsync(x => x.Key == "Billing.InvoiceDueDays");
        var originalSettingValue = setting.Value;
        setting.Value = "31";

        await dbContext.SaveChangesAsync();

        client.Name = originalClientName;
        request.Title = originalRequestTitle;
        invoice.DueDays = originalDueDays;
        setting.Value = originalSettingValue;
        await dbContext.SaveChangesAsync();

        (await CountHistoryAsync("CustomersHistory", "Id", client.Id)).Should().BeGreaterThan(0);
        (await CountHistoryAsync("BillingRequestsHistory", "Id", request.Id)).Should().BeGreaterThan(0);
        (await CountHistoryAsync("InvoicesHistory", "Id", invoice.Id)).Should().BeGreaterThan(0);
        (await CountHistoryAsync("AppSettingsHistory", "Key", setting.Key)).Should().BeGreaterThan(0);
    }

    private async Task<int> CountHistoryAsync(string historyTable, string keyColumn, object keyValue)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM dbo.{historyTable} WHERE [{keyColumn}] = @keyValue";
        command.Parameters.AddWithValue("@keyValue", keyValue);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
