using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task GetPdf_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/invoices/{FlowLedgerSeedData.Invoices[0].Id}/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPdf_AsAccounts_ReturnsPdfAttachment()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await client.GetAsync($"/api/invoices/{FlowLedgerSeedData.Invoices[0].Id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(InvoicePdfResult.ContentType);
        response.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("INV-2026-0001.pdf");
        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes.Take(5).ToArray()).Should().Be("%PDF-");
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

    [Fact]
    public async Task ListAndCsvExport_WithDateAndAmountFilters_ReturnMatchingInvoice()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var olderHighAmountRequestId = await CreateApprovedRequestAsync(salesClient, 70000m, "Invoice filter target");
        var recentHighAmountRequestId = await CreateApprovedRequestAsync(salesClient, 70000m, "Invoice filter target");
        var recentLowAmountRequestId = await CreateApprovedRequestAsync(salesClient, 40000m, "Invoice filter target");

        var olderHighAmountInvoice = (await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{olderHighAmountRequestId}", JsonOptions))!.Invoice!;
        var recentHighAmountInvoice = (await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{recentHighAmountRequestId}", JsonOptions))!.Invoice!;
        var recentLowAmountInvoice = (await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{recentLowAmountRequestId}", JsonOptions))!.Invoice!;

        var today = DateTime.UtcNow.Date;
        await SetInvoiceIssuedAtAsync(olderHighAmountInvoice.Id, today.AddDays(-10).AddHours(9));
        await SetInvoiceIssuedAtAsync(recentHighAmountInvoice.Id, today.AddHours(9));
        await SetInvoiceIssuedAtAsync(recentLowAmountInvoice.Id, today.AddHours(10));

        var listResponse = await accountsClient.GetAsync($"/api/invoices?fromDate={today:yyyy-MM-dd}&untilDate={today:yyyy-MM-dd}&minAmount=80000&maxAmount=81000");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<InvoiceListItemDto>>(JsonOptions);

        var csvResponse = await accountsClient.GetAsync($"/api/invoices/export?fromDate={today:yyyy-MM-dd}&untilDate={today:yyyy-MM-dd}&minAmount=80000&maxAmount=81000");
        var csv = await csvResponse.Content.ReadAsStringAsync();

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        list!.Items.Should().ContainSingle(x => x.Id == recentHighAmountInvoice.Id);
        csvResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        csv.Should().Contain(recentHighAmountInvoice.InvoiceNumber);
        csv.Should().NotContain(olderHighAmountInvoice.InvoiceNumber);
        csv.Should().NotContain(recentLowAmountInvoice.InvoiceNumber);
    }

    private async Task<Guid> CreateApprovedRequestAsync(HttpClient salesClient, decimal subtotal, string title = "Invoice API request")
    {
        var createResponse = await salesClient.PostAsJsonAsync(
            "/api/billing-requests",
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                title,
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

    private async Task SetInvoiceIssuedAtAsync(Guid invoiceId, DateTime issuedAtUtc)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        var invoice = await dbContext.Invoices.SingleAsync(x => x.Id == invoiceId);

        invoice.IssuedAtUtc = issuedAtUtc;
        invoice.DueAtUtc = issuedAtUtc.AddDays(30);

        await dbContext.SaveChangesAsync();
    }

    private sealed record CreateResponse(Guid Id);
}
