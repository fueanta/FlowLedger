using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Auth;
using FlowLedger.Application.Common;
using FlowLedger.Application.Enrollment;
using FlowLedger.Application.Users;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class EnrollmentAndUserAdminEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public EnrollmentAndUserAdminEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_PendingUserCannotLoginUntilAdminApproval()
    {
        using var publicClient = _fixture.Factory.CreateClient();
        var email = UniqueEmail("pending-approval");
        var password = "Pending-user-password-1!";

        var register = await publicClient.PostAsJsonAsync(
            "/api/enrollment-requests",
            new RegisterEnrollmentRequestDto("Pending User", email, password, RoleName.Sales));
        var loginBeforeApproval = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        loginBeforeApproval.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_AsAdmin_CreatesActiveUserWhoCanLogin()
    {
        using var publicClient = _fixture.Factory.CreateClient();
        var email = UniqueEmail("approved");
        var password = "Approved-user-password-1!";
        var id = await RegisterAsync(publicClient, "Approved User", email, password, RoleName.Sales);
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var approve = await adminClient.PostAsJsonAsync($"/api/enrollment-requests/{id}/approve", new ApproveEnrollmentRequestDto(RoleName.Accounts));
        var login = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        loginBody!.User.Role.Should().Be(RoleName.Accounts);
        loginBody.User.Status.Should().Be(UserStatus.Active);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        var auditLog = await dbContext.AuditLogs.AsNoTracking().SingleAsync(x =>
            x.EntityType == "EnrollmentRequest" &&
            x.EntityId == id &&
            x.ActionType == AuditActionType.EnrollmentApproved);
        auditLog.ActorUserId.Should().Be(FlowLedgerSeedData.AdminUserId);
        auditLog.BeforeStatus.Should().Be("Pending");
        auditLog.AfterStatus.Should().Be("Approved");
    }

    [Fact]
    public async Task Reject_AsAdmin_KeepsUserUnableToLogin()
    {
        using var publicClient = _fixture.Factory.CreateClient();
        var email = UniqueEmail("rejected");
        var password = "Rejected-user-password-1!";
        var id = await RegisterAsync(publicClient, "Rejected User", email, password, RoleName.Sales);
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var reject = await adminClient.PostAsJsonAsync($"/api/enrollment-requests/{id}/reject", new RejectEnrollmentRequestDto("Not approved."));
        var login = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));

        reject.StatusCode.Should().Be(HttpStatusCode.NoContent);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_User_PreventsLogin()
    {
        using var publicClient = _fixture.Factory.CreateClient();
        var email = UniqueEmail("deactivated");
        var password = "Deactivated-user-password-1!";
        var id = await RegisterAsync(publicClient, "Deactivated User", email, password, RoleName.Sales);
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);
        await adminClient.PostAsJsonAsync($"/api/enrollment-requests/{id}/approve", new ApproveEnrollmentRequestDto(RoleName.Sales));
        var users = await adminClient.GetFromJsonAsync<PagedResult<UserDto>>($"/api/users?search={Uri.EscapeDataString(email)}", JsonOptions);
        var userId = users!.Items.Single().Id;

        var deactivate = await adminClient.PostAsync($"/api/users/{userId}/deactivate", null);
        var login = await publicClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));

        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        var auditLog = await dbContext.AuditLogs.AsNoTracking().SingleAsync(x =>
            x.EntityType == "User" &&
            x.EntityId == userId &&
            x.ActionType == AuditActionType.UserDeactivated);
        auditLog.ActorUserId.Should().Be(FlowLedgerSeedData.AdminUserId);
        auditLog.BeforeStatus.Should().Be("Active");
        auditLog.AfterStatus.Should().Be("Inactive");
    }

    [Fact]
    public async Task Deactivate_Self_ReturnsBadRequest()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await adminClient.PostAsync($"/api/users/{FlowLedgerSeedData.AdminUserId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactivate_LastActiveAdmin_ReturnsBadRequest()
    {
        using var publicClient = _fixture.Factory.CreateClient();
        var email = UniqueEmail("second-admin");
        var password = "Second-admin-password-1!";
        var id = await RegisterAsync(publicClient, "Second Admin", email, password, RoleName.Admin);
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);
        await adminClient.PostAsJsonAsync($"/api/enrollment-requests/{id}/approve", new ApproveEnrollmentRequestDto(RoleName.Admin));
        var users = await adminClient.GetFromJsonAsync<PagedResult<UserDto>>($"/api/users?search={Uri.EscapeDataString(email)}", JsonOptions);
        var secondAdminId = users!.Items.Single().Id;
        await adminClient.PostAsync($"/api/users/{secondAdminId}/deactivate", null);

        var response = await adminClient.PostAsync($"/api/users/{FlowLedgerSeedData.AdminUserId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Guid> RegisterAsync(HttpClient client, string fullName, string email, string password, RoleName role)
    {
        var response = await client.PostAsJsonAsync(
            "/api/enrollment-requests",
            new RegisterEnrollmentRequestDto(fullName, email, password, role));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return body!.Id;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@flowledger.local";
    }

    private sealed record CreateResponse(Guid Id);
}
