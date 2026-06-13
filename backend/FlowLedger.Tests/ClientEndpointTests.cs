using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Common;
using FlowLedger.Application.Customers;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class ClientEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public ClientEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_AsManager_ReturnsPagedClients()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Manager);

        var response = await client.GetAsync("/api/clients?page=1&pageSize=10&sortBy=companyName&sortDirection=asc");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(6);
        result.Items.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.ContactPerson));
    }

    [Fact]
    public async Task Create_AsSales_CreatesActiveClient()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);

        var response = await client.PostAsJsonAsync("/api/clients", NewCreateClient("sales-create"));
        var body = await response.Content.ReadFromJsonAsync<CreateResponse>(JsonOptions);
        var detail = await client.GetFromJsonAsync<CustomerDto>($"/api/clients/{body!.Id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        detail!.Name.Should().Be("Phase One Client sales-create");
        detail.ContactPerson.Should().Be("Client Owner");
        detail.Status.Should().Be(ClientStatus.Active);
    }

    [Fact]
    public async Task Update_AsAccounts_UpdatesClientFields()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateClientAsync(salesClient, "accounts-update");
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await accountsClient.PutAsJsonAsync(
            $"/api/clients/{id}",
            new UpdateClientDto(
                "Updated Phase One Client",
                "Updated Owner",
                "updated-phase-one-client@flowledger.local",
                "+8801700002222",
                "Updated Address",
                "TIN-UPDATED",
                ClientStatus.Inactive));
        var detail = await accountsClient.GetFromJsonAsync<CustomerDto>($"/api/clients/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Name.Should().Be("Updated Phase One Client");
        detail.ContactPerson.Should().Be("Updated Owner");
        detail.Status.Should().Be(ClientStatus.Inactive);
    }

    [Fact]
    public async Task Update_AsManager_ReturnsForbidden()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateClientAsync(salesClient, "manager-denied");
        using var managerClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Manager);

        var response = await managerClient.PutAsJsonAsync(
            $"/api/clients/{id}",
            new UpdateClientDto(
                "Manager Update Denied",
                "Manager Owner",
                "manager-denied-update@flowledger.local",
                null,
                "Manager Address",
                null,
                ClientStatus.Active));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_AsAdmin_ArchivesClient()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateClientAsync(salesClient, "admin-archive");
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await adminClient.PostAsync($"/api/clients/{id}/archive", null);
        var detail = await adminClient.GetFromJsonAsync<CustomerDto>($"/api/clients/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Status.Should().Be(ClientStatus.Archived);
        detail.ArchivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Archive_AsAccounts_ReturnsForbidden()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateClientAsync(salesClient, "accounts-archive-denied");
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await accountsClient.PostAsync($"/api/clients/{id}/archive", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBillingRequest_WithArchivedClient_ReturnsBadRequest()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateClientAsync(salesClient, "archived-billing");
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);
        await adminClient.PostAsync($"/api/clients/{id}/archive", null);

        var response = await salesClient.PostAsJsonAsync(
            "/api/billing-requests",
            new
            {
                CustomerId = id,
                Title = "Archived client request",
                Description = "Should be blocked.",
                LineItems = new[] { new { Description = "Blocked service", Quantity = 1, UnitPrice = 1000m } }
            });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("active clients");
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string suffix)
    {
        var response = await client.PostAsJsonAsync("/api/clients", NewCreateClient(suffix));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateResponse>(JsonOptions);
        return body!.Id;
    }

    private static CreateClientDto NewCreateClient(string suffix)
    {
        return new CreateClientDto(
            $"Phase One Client {suffix}",
            "Client Owner",
            $"phase-one-client-{suffix}@flowledger.local",
            "+8801700001111",
            "Phase One Road, Dhaka",
            "TIN-PHASE-ONE");
    }

    private sealed record CreateResponse(Guid Id);
}
