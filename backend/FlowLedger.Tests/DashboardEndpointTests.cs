using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Dashboard;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class DashboardEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public DashboardEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Summary_WithDefaultPeriod_ReturnsRecentSeedDataAndScopeMetadata()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/dashboard/summary");
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        summary.Should().NotBeNull();
        summary!.Period.Months.Should().Be(1);
        summary.Period.StartUtc.Should().BeBefore(summary.Period.EndUtc);
        summary.MetricScopes.Should().ContainKey(nameof(DashboardSummaryDto.TotalRequests)).WhoseValue.Should().Be("Period");
        summary.MetricScopes.Should().ContainKey(nameof(DashboardSummaryDto.PendingAccountsReview)).WhoseValue.Should().Be("Current");
        summary!.TotalRequests.Should().BeGreaterThan(0);
        summary.PendingAccountsReview.Should().BeGreaterThan(0);
        summary.PendingManagerApproval.Should().BeGreaterThan(0);
        summary.TotalInvoiceAmount.Should().BeGreaterThan(0);
        summary.PaidInvoiceAmount.Should().BeGreaterThan(0);
        summary.StatusBreakdown.Should().Contain(x => x.Status == "AccountsReview");
        summary.MonthlyInvoiceTrend.Should().NotBeEmpty();
        summary.AgingBuckets.Select(x => x.Label).Should().BeEquivalentTo(["0-1 days", "2-3 days", "4+ days"]);
        summary.RecentActivity.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Summary_CurrentWorkloadMetrics_DoNotChangeWhenPeriodChanges()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var oneMonthResponse = await client.GetAsync("/api/dashboard/summary?periodMonths=1");
        var sixMonthResponse = await client.GetAsync("/api/dashboard/summary?periodMonths=6");
        var oneMonth = await oneMonthResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        var sixMonth = await sixMonthResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);

        oneMonthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        sixMonthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        oneMonth.Should().NotBeNull();
        sixMonth.Should().NotBeNull();
        sixMonth!.TotalRequests.Should().BeGreaterThanOrEqualTo(oneMonth!.TotalRequests);
        sixMonth.TotalInvoiceAmount.Should().BeGreaterThanOrEqualTo(oneMonth.TotalInvoiceAmount);
        sixMonth.PendingAccountsReview.Should().Be(oneMonth.PendingAccountsReview);
        sixMonth.PendingManagerApproval.Should().Be(oneMonth.PendingManagerApproval);
        sixMonth.AgingBuckets.Sum(x => x.Count).Should().Be(oneMonth.AgingBuckets.Sum(x => x.Count));
    }

    [Fact]
    public async Task Summary_PeriodMetrics_IncreaseAcrossOneThreeSixAndTwelveMonths()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var oneMonth = await GetSummaryAsync(client, 1);
        var threeMonths = await GetSummaryAsync(client, 3);
        var sixMonths = await GetSummaryAsync(client, 6);
        var twelveMonths = await GetSummaryAsync(client, 12);

        oneMonth.TotalRequests.Should().BeGreaterThan(0);
        threeMonths.TotalRequests.Should().BeGreaterThan(oneMonth.TotalRequests);
        sixMonths.TotalRequests.Should().BeGreaterThan(threeMonths.TotalRequests);
        twelveMonths.TotalRequests.Should().BeGreaterThan(sixMonths.TotalRequests);

        threeMonths.TotalInvoiceAmount.Should().BeGreaterThan(oneMonth.TotalInvoiceAmount);
        sixMonths.TotalInvoiceAmount.Should().BeGreaterThan(threeMonths.TotalInvoiceAmount);
        twelveMonths.TotalInvoiceAmount.Should().BeGreaterThan(sixMonths.TotalInvoiceAmount);
        twelveMonths.MonthlyInvoiceTrend.Count.Should().BeGreaterThan(oneMonth.MonthlyInvoiceTrend.Count);
    }

    [Fact]
    public async Task RefreshedDemoSeedData_FinalWorkflowStates_HaveMatchingAuditLogs()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();

        var requests = await dbContext.BillingRequests
            .Include(x => x.AuditLogs)
            .Where(x => x.RequestNumber.StartsWith("BR-2026-"))
            .ToListAsync();

        requests.Where(x => x.Status == BillingRequestStatus.AccountsReview)
            .Should().OnlyContain(x =>
                x.AssignedQueue == WorkflowQueue.Accounts &&
                x.AssignedAtUtc != null &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.Submitted) &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.Assigned));
        requests.Where(x => x.Status == BillingRequestStatus.ManagerApproval)
            .Should().OnlyContain(x =>
                x.AssignedQueue == WorkflowQueue.Manager &&
                x.AssignedAtUtc != null &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.Submitted) &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.Assigned));
        requests.Where(x => x.Status == BillingRequestStatus.Rejected)
            .Should().OnlyContain(x =>
                x.AssignedQueue == WorkflowQueue.Sales &&
                x.AssignedAtUtc != null &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.Rejected));
        requests.Where(x => x.Status == BillingRequestStatus.InvoiceGenerated)
            .Should().OnlyContain(x =>
                x.AssignedQueue == WorkflowQueue.None &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.InvoiceGenerated));
        requests.Where(x => x.Status == BillingRequestStatus.Paid)
            .Should().OnlyContain(x =>
                x.AssignedQueue == WorkflowQueue.None &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.InvoiceGenerated) &&
                x.AuditLogs.Any(log => log.ActionType == AuditActionType.PaymentMarked));
    }

    [Fact]
    public async Task Summary_WithInvalidPeriod_ReturnsValidationProblem()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/dashboard/summary?periodMonths=2");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("PeriodMonths");
    }

    private static async Task<DashboardSummaryDto> GetSummaryAsync(HttpClient client, int periodMonths)
    {
        var response = await client.GetAsync($"/api/dashboard/summary?periodMonths={periodMonths}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        summary.Should().NotBeNull();
        return summary!;
    }
}
