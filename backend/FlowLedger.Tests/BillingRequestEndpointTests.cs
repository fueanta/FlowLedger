using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Audit;
using FlowLedger.Application.Auth;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class BillingRequestEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public BillingRequestEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/billing-requests");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithInvalidInput_ReturnsValidationProblem()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);

        var response = await client.PostAsJsonAsync(
            "/api/billing-requests",
            new CreateBillingRequestDto(
                Guid.Empty,
                "",
                "",
                [new CreateBillingRequestLineItemDto("", 0, 0m)]));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("CustomerId");
        body.Should().Contain("Title");
        body.Should().Contain("LineItems[0].Quantity");
    }

    [Fact]
    public async Task Create_AsAccounts_ReturnsForbidden()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await client.PostAsJsonAsync(
            "/api/billing-requests",
            NewRequest(25000m, "Accounts should not create"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateListDetailAndSubmit_WithSalesUser_CreatesDraftThenMovesToAccountsReview()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);

        var id = await CreateRequestAsync(client, 40000m);

        var listResponse = await client.GetAsync("/api/billing-requests?createdByMe=true&search=API%20workflow");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<BillingRequestListItemDto>>(JsonOptions);
        var detailBeforeSubmit = await client.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        var submitResponse = await client.PostAsync($"/api/billing-requests/{id}/submit", null);
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        var detailAfterSubmit = await client.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        list!.Items.Should().ContainSingle(x => x.Id == id);
        detailBeforeSubmit!.Status.Should().Be(BillingRequestStatus.Draft);
        detailBeforeSubmit.TotalAmount.Should().Be(46000m);
        detailBeforeSubmit.AvailableActions.Should().Contain(["Update", "Submit", "Comment"]);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, submitBody);
        detailAfterSubmit!.Status.Should().Be(BillingRequestStatus.AccountsReview);
        detailAfterSubmit.AssignedTo!.Role.Should().Be(RoleName.Accounts);
        detailAfterSubmit.AssignedQueue.Should().Be(WorkflowQueue.Accounts);
        detailAfterSubmit.AssignedAtUtc.Should().NotBeNull();
        detailAfterSubmit.LastWorkflowActionAtUtc.Should().NotBeNull();
        detailAfterSubmit.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Submitted);
    }

    [Fact]
    public async Task WorkQueue_AsAccounts_ReturnsAccountsQueueOnly()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 45000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await accountsClient.GetAsync("/api/work-queue?search=API%20workflow");
        var queue = await response.Content.ReadFromJsonAsync<PagedResult<BillingRequestListItemDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queue!.Items.Should().ContainSingle(x => x.Id == id);
        queue.Items.Should().OnlyContain(x => x.AssignedQueue == WorkflowQueue.Accounts);
    }

    [Fact]
    public async Task Approve_UnderThreshold_AsAccounts_GeneratesInvoiceAndAuditLogs()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 50000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await accountsClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/approve",
            new ApproveBillingRequestDto("Approved by accounts."));
        var detail = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Status.Should().Be(BillingRequestStatus.InvoiceGenerated);
        detail.Invoice.Should().NotBeNull();
        detail.Invoice!.Status.Should().Be(InvoiceStatus.Issued);
        detail.AssignedQueue.Should().Be(WorkflowQueue.None);
        detail.AssignedAtUtc.Should().BeNull();
        detail.AssignedTo.Should().BeNull();
        detail.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Approved);
        detail.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.InvoiceGenerated);
        detail.Comments.Should().Contain(x => x.Body == "Approved by accounts.");
    }

    [Fact]
    public async Task Approve_HighValue_AsAccounts_MovesToManagerApproval()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 120000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var response = await accountsClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/approve",
            new ApproveBillingRequestDto("Needs manager approval."));
        var detail = await accountsClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Status.Should().Be(BillingRequestStatus.ManagerApproval);
        detail.Invoice.Should().BeNull();
        detail.AssignedTo!.Role.Should().Be(RoleName.Manager);
        detail.AssignedQueue.Should().Be(WorkflowQueue.Manager);
    }

    [Fact]
    public async Task Approve_AccountsReview_AsManager_ReturnsForbidden()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 60000m);
        using var managerClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Manager);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/approve",
            new ApproveBillingRequestDto("Manager cannot approve AccountsReview."));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_ManagerApproval_AsManager_GeneratesInvoice()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 130000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);
        await accountsClient.PostAsJsonAsync($"/api/billing-requests/{id}/approve", new ApproveBillingRequestDto("Send to manager."));
        using var managerClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Manager);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/approve",
            new ApproveBillingRequestDto("Manager approved."));
        var detail = await managerClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Status.Should().Be(BillingRequestStatus.InvoiceGenerated);
        detail.Invoice.Should().NotBeNull();
        detail.Invoice!.TotalAmount.Should().Be(149500m);
        detail.AssignedQueue.Should().Be(WorkflowQueue.None);
        detail.AssignedTo.Should().BeNull();
    }

    [Fact]
    public async Task Reject_AsAccounts_SetsRejectedAndSalesCanUpdateAndResubmit()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 30000m);
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var rejectResponse = await accountsClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/reject",
            new RejectBillingRequestDto("Missing purchase order."));
        var updateResponse = await salesClient.PutAsJsonAsync(
            $"/api/billing-requests/{id}",
            NewRequest(35000m, "Updated after rejection"));
        var submitResponse = await salesClient.PostAsync($"/api/billing-requests/{id}/submit", null);
        var detail = await salesClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Status.Should().Be(BillingRequestStatus.AccountsReview);
        detail.Title.Should().Be("Updated after rejection");
        detail.AssignedQueue.Should().Be(WorkflowQueue.Accounts);
        detail.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Rejected);
        detail.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Updated);
    }

    [Fact]
    public async Task Submit_WhenAuditWriterFails_RollsBackWorkflowTransition()
    {
        using var normalClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateRequestAsync(normalClient, 25000m);
        using var failingFactory = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IWorkflowAuditWriter>();
                services.AddScoped<IWorkflowAuditWriter, FailingWorkflowAuditWriter>();
            });
        });
        using var failingClient = failingFactory.CreateClient();
        await AuthenticateAsync(failingClient, "sales@flowledger.local", AuthEndpointFixture.SalesPassword);

        var response = await failingClient.PostAsync($"/api/billing-requests/{id}/submit", null);
        var detail = await normalClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        detail!.Status.Should().Be(BillingRequestStatus.Draft);
        detail.AssignedQueue.Should().Be(WorkflowQueue.Sales);
        detail.AuditLogs.Should().NotContain(x => x.ActionType == AuditActionType.Submitted);
    }

    [Fact]
    public async Task AddComment_WithEmptyBody_ReturnsValidationProblem()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateRequestAsync(salesClient, 25000m);

        var response = await salesClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/comments",
            new AddCommentDto(""));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("Body");
    }

    [Fact]
    public async Task AddComment_WithVisibleRequest_AddsCommentAndAuditLog()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateRequestAsync(salesClient, 25000m);

        var response = await salesClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/comments",
            new AddCommentDto("Please review before submission."));
        var detail = await salesClient.GetFromJsonAsync<BillingRequestDetailDto>($"/api/billing-requests/{id}", JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        detail!.Comments.Should().Contain(x => x.Body == "Please review before submission.");
        detail.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Commented);
    }

    [Fact]
    public async Task Approve_AsSales_ReturnsForbidden()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        var id = await CreateSubmittedRequestAsync(salesClient, 30000m);

        var response = await salesClient.PostAsJsonAsync(
            $"/api/billing-requests/{id}/approve",
            new ApproveBillingRequestDto("Sales should not approve."));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> CreateSubmittedRequestAsync(HttpClient salesClient, decimal subtotal)
    {
        var id = await CreateRequestAsync(salesClient, subtotal);
        var submitResponse = await salesClient.PostAsync($"/api/billing-requests/{id}/submit", null);
        if (!submitResponse.IsSuccessStatusCode)
        {
            var body = await submitResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(body);
        }

        return id;
    }

    private static async Task<Guid> CreateRequestAsync(HttpClient client, decimal subtotal)
    {
        var response = await client.PostAsJsonAsync("/api/billing-requests", NewRequest(subtotal, "API workflow request"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return body!.Id;
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.AccessToken);
    }

    private static CreateBillingRequestDto NewRequest(decimal subtotal, string title)
    {
        return new CreateBillingRequestDto(
            FlowLedgerSeedData.FiberRetailCustomerId,
            title,
            "Created from integration test.",
            [new CreateBillingRequestLineItemDto("Consulting services", 1, subtotal)]);
    }

    private sealed record CreateResponse(Guid Id);

    private sealed class FailingWorkflowAuditWriter : IWorkflowAuditWriter
    {
        public void Add(WorkflowAuditEntry entry)
        {
            throw new InvalidOperationException("Simulated audit write failure.");
        }
    }
}
