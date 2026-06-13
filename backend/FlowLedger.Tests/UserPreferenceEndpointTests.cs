using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Preferences;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Tests;

[Collection("Api endpoints")]
public class UserPreferenceEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public UserPreferenceEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMine_WithNoPreference_ReturnsRoleDefault()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var sales = await salesClient.GetFromJsonAsync<UserPreferenceDto>("/api/preferences/me", JsonOptions);
        var admin = await adminClient.GetFromJsonAsync<UserPreferenceDto>("/api/preferences/me", JsonOptions);

        sales!.DefaultLandingPage.Should().Be("/app/requests");
        sales.RowsPerPage.Should().Be(25);
        admin!.DefaultLandingPage.Should().Be("/app/dashboard");
        admin.RowsPerPage.Should().Be(50);
    }

    [Fact]
    public async Task UpdateMine_WithValidPreference_Persists()
    {
        using var accountsClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Accounts);

        var update = await accountsClient.PutAsJsonAsync(
            "/api/preferences/me",
            new UpdateUserPreferenceDto(6, "/app/work-queue", 100));
        var get = await accountsClient.GetFromJsonAsync<UserPreferenceDto>("/api/preferences/me", JsonOptions);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        get!.DefaultDashboardPeriodMonths.Should().Be(6);
        get.DefaultLandingPage.Should().Be("/app/work-queue");
        get.RowsPerPage.Should().Be(100);
    }

    [Fact]
    public async Task UpdateMine_WithAdminOnlyLandingPage_AsSales_ReturnsBadRequest()
    {
        using var salesClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Sales);

        var response = await salesClient.PutAsJsonAsync(
            "/api/preferences/me",
            new UpdateUserPreferenceDto(1, "/app/users", 25));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMine_WithInvalidPageSize_ReturnsBadRequest()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync(RoleName.Admin);

        var response = await adminClient.PutAsJsonAsync(
            "/api/preferences/me",
            new UpdateUserPreferenceDto(1, "/app/dashboard", 20));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
