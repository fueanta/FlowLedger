using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence.SeedData;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class InvoiceEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public InvoiceEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetInvoicesAndDetail_WithGeneratedInvoice_ReturnsInvoiceData()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var billingRequestId = await CreateApprovedRequestAsync(salesClient, 45000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var request = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{billingRequestId}", JsonOptions);

        var listResponse = await accountsClient.GetAsync($"/api/invoices?search={request!.Invoice!.InvoiceNumber}");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);
        var detailResponse = await accountsClient.GetAsync($"/api/invoices/{request.Invoice.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<InvoiceDetailDto>(JsonOptions);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        list!.Items.Should().ContainSingle(x => x.Id == request.Invoice.Id);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        detail!.InvoiceNumber.Should().Be(request.Invoice.InvoiceNumber);
        detail.BillingRequest.Id.Should().Be(billingRequestId);
        detail.Customer.Id.Should().Be(FlowLedgerSeedData.FiberRetailCustomerId);
    }

    [Fact]
    public async Task MarkPaid_AsAccounts_UpdatesInvoiceAndBillingRequestAuditLog()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var billingRequestId = await CreateApprovedRequestAsync(salesClient, 48000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var request = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{billingRequestId}", JsonOptions);

        var paidResponse = await accountsClient.PostAsync($"/api/invoices/{request!.Invoice!.Id}/mark-paid", null);
        var invoice = await accountsClient.GetFromJsonAsync<InvoiceDetailDto>($"/api/invoices/{request.Invoice.Id}", JsonOptions);
        var billingRequest = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{billingRequestId}", JsonOptions);

        paidResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        invoice!.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
        billingRequest!.Status.Should().Be(BillingRequestStatus.Paid);
        billingRequest.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.PaymentMarked);
    }

    [Fact]
    public async Task MarkPaid_AsSales_ReturnsForbidden()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var billingRequestId = await CreateApprovedRequestAsync(salesClient, 49000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var request = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{billingRequestId}", JsonOptions);

        var response = await salesClient.PostAsync($"/api/invoices/{request!.Invoice!.Id}/mark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateApprovedRequestAsync(HttpClient salesClient, decimal subtotal)
    {
        var createResponse = await salesClient.PostAsJsonAsync(
            "/api/billing-requests",
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Invoice API request",
                "Created from invoice integration test.",
                [new CreateBillingRequestLineItemDto("Implementation services", 1, subtotal)]));
        createResponse.EnsureSuccessStatusCode();
        var body = await createResponse.Content.ReadFromJsonAsync<CreateResponse>();

        var submitResponse = await salesClient.PostAsync($"/api/billing-requests/{body!.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var approveResponse = await accountsClient.PostAsJsonAsync(
            $"/api/billing-requests/{body.Id}/approve",
            new ApproveBillingRequestDto("Approved for invoice test."));
        approveResponse.EnsureSuccessStatusCode();

        return body.Id;
    }

    private sealed record CreateResponse(Guid Id);
}
