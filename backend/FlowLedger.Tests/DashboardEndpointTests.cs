using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Dashboard;
using FlowLedger.Domain.Enums;

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
    public async Task Summary_AsAdmin_ReturnsCardsBreakdownsTrendAgingAndActivity()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/dashboard/summary?periodMonths=6");
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        summary.Should().NotBeNull();
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
    public async Task Summary_WithInvalidPeriod_ReturnsValidationProblem()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/dashboard/summary?periodMonths=2");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("PeriodMonths");
    }
}
