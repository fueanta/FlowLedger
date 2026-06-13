using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Configuration;
using FlowLedger.Application.Invoices;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence.SeedData;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class SettingsEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public SettingsEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_AsInternalUser_ReturnsSettings()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Manager);

        var response = await client.GetAsync("/api/settings");
        var settings = await response.Content.ReadFromJsonAsync<SystemSettingsDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        settings!.VatPercentage.Should().BeGreaterThanOrEqualTo(0m);
        settings.ManagerApprovalThreshold.Should().BeGreaterThan(0m);
        settings.InvoiceDueDays.Should().BeInRange(1, 365);
    }

    [Fact]
    public async Task Update_AsAdmin_ChangesSettings()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await client.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(18m, 175000m, 45));
        var settings = await client.GetFromJsonAsync<SystemSettingsDto>("/api/settings", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        settings!.VatPercentage.Should().Be(18m);
        settings.ManagerApprovalThreshold.Should().Be(175000m);
        settings.InvoiceDueDays.Should().Be(45);

        await client.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(15m, 100000m, 30));
    }

    [Fact]
    public async Task Update_AsAccounts_ReturnsForbidden()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await client.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(16m, 90000m, 20));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExistingInvoice_DoesNotChangeAfterSettingsUpdate()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);
        await adminClient.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(15m, 100000m, 30));
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var billingRequestId = await CreateApprovedRequestAsync(salesClient, 40000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var request = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{billingRequestId}", JsonOptions);
        var before = await accountsClient.GetFromJsonAsync<InvoiceDetailDto>($"/api/invoices/{request!.Invoice!.Id}", JsonOptions);

        await adminClient.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(20m, 250000m, 10));
        var after = await accountsClient.GetFromJsonAsync<InvoiceDetailDto>($"/api/invoices/{request.Invoice.Id}", JsonOptions);

        after!.VatPercentage.Should().Be(before!.VatPercentage);
        after.VatAmount.Should().Be(before.VatAmount);
        after.TotalAmount.Should().Be(before.TotalAmount);
        after.DueDays.Should().Be(before.DueDays);
        after.DueAtUtc.Should().Be(before.DueAtUtc);

        await adminClient.PutAsJsonAsync("/api/settings", new UpdateSystemSettingsDto(15m, 100000m, 30));
    }

    private async Task<Guid> CreateApprovedRequestAsync(HttpClient salesClient, decimal subtotal)
    {
        var createResponse = await salesClient.PostAsJsonAsync(
            "/api/billing-requests",
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Settings invoice regression",
                "Created from settings integration test.",
                [new CreateBillingRequestLineItemDto("Settings service", 1, subtotal)]));
        createResponse.EnsureSuccessStatusCode();
        var body = await createResponse.Content.ReadFromJsonAsync<CreateResponse>();

        var submitResponse = await salesClient.PostAsync($"/api/billing-requests/{body!.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        var approveResponse = await accountsClient.PostAsJsonAsync(
            $"/api/billing-requests/{body.Id}/approve",
            new ApproveBillingRequestDto("Approved for settings regression."));
        approveResponse.EnsureSuccessStatusCode();

        return body.Id;
    }

    private sealed record CreateResponse(Guid Id);
}
