using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Audit;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Customers;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class StandardDataTableEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public StandardDataTableEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/api/billing-requests?pageSize=20")]
    [InlineData("/api/invoices?pageSize=20")]
    [InlineData("/api/clients?pageSize=20")]
    [InlineData("/api/users?pageSize=20")]
    [InlineData("/api/enrollment-requests?pageSize=20")]
    [InlineData("/api/audit-logs?pageSize=20")]
    public async Task List_WithUnsupportedPageSize_ReturnsBadRequest(string path)
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/billing-requests?sortBy=passwordHash")]
    [InlineData("/api/invoices?sortBy=passwordHash")]
    [InlineData("/api/clients?sortBy=passwordHash")]
    [InlineData("/api/users?sortBy=passwordHash")]
    [InlineData("/api/enrollment-requests?sortBy=passwordHash")]
    [InlineData("/api/audit-logs?sortBy=passwordHash")]
    public async Task List_WithUnsupportedSort_ReturnsBadRequest(string path)
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BillingRequests_SearchSortAndPage_ReturnsExpectedRows()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/billing-requests?search=Fiber&page=1&pageSize=10&sortBy=amount&sortDirection=desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BillingRequestListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.PageSize.Should().Be(10);
        result.Items.Should().OnlyContain(x => x.CustomerName.Contains("Fiber") || x.Title.Contains("Fiber") || x.RequestNumber.Contains("Fiber"));
        result.Items.Select(x => x.TotalAmount).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Invoices_SearchAndSort_ReturnsExpectedRows()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await client.GetAsync("/api/invoices?search=INV-2026&pageSize=10&sortBy=amount&sortDirection=asc");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().NotBeEmpty();
        result.Items.Select(x => x.TotalAmount).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Clients_CsvExport_RespectsSearchAndUsesSafeHeaders()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/clients/export?search=Fiber");
        var csv = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        csv.Should().StartWith("Company Name,Contact Person,Email,Phone,Status,Tax Identifier,Created At UTC,Updated At UTC,Archived At UTC");
        csv.Should().Contain("Fiber Retail Ltd.");
        csv.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task BillingRequests_CsvExport_RespectsStatusFilter()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/billing-requests/export?status=Paid");
        var csv = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        csv.Should().Contain("Status");
        csv.Should().Contain("Paid");
        csv.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task Invoices_CsvExport_RespectsSearchFilter()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await client.GetAsync("/api/invoices/export?search=INV-2026-0001");
        var csv = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        csv.Should().Contain("INV-2026-0001");
        csv.Should().NotContain("INV-2026-0002");
    }

    [Fact]
    public async Task AuditLogs_List_ReturnsPagedAuditActivity()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/audit-logs?pageSize=10&search=BR-2026&sortBy=createdAtUtc&sortDirection=desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(x => x.EntityNumber == null || x.EntityNumber.Contains("BR-2026"));
    }

    [Fact]
    public async Task AuditLogs_FilterByEntityActionActorAndDate_ReturnsMatchingRows()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.GetAsync("/api/audit-logs?pageSize=10&entityType=BillingRequest&actionType=Created&actor=Sarah&fromDate=2000-01-01&untilDate=2100-01-01");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(x =>
            x.EntityType == "BillingRequest" &&
            x.ActionType == AuditActionType.Created &&
            x.ActorDisplayName.Contains("Sarah"));
    }

    [Theory]
    [InlineData(RoleName.Manager)]
    [InlineData(RoleName.Accounts)]
    [InlineData(RoleName.Admin)]
    [InlineData(RoleName.Sales)]
    public async Task AuditLogs_AllInternalRoles_SeeAllLogs(RoleName role)
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(role);

        var response = await client.GetAsync("/api/audit-logs?pageSize=10&sortBy=createdAtUtc&sortDirection=desc");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().NotBeEmpty("all internal roles should see the full audit log.");
    }
}
