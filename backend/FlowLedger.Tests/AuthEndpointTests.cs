using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowLedger.Application.Auth;

namespace FlowLedger.Tests;

public class AuthEndpointTests : IClassFixture<AuthEndpointFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthEndpointFixture _fixture;

    public AuthEndpointTests(AuthEndpointFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithSeededUser_ReturnsJwtAndUser()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(TestAuthSeedData.TestSalesEmail, TestAuthSeedData.TestSalesPassword));
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.User.Id.Should().Be(TestAuthSeedData.TestSalesUserId);
        body.User.FullName.Should().Be("Test Sales User");
        body.User.Email.Should().Be(TestAuthSeedData.TestSalesEmail);
        body.User.Role.ToString().Should().Be("Sales");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(TestAuthSeedData.TestSalesUserId.ToString());
        token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(TestAuthSeedData.TestSalesEmail);
        token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Name).Value.Should().Be("Test Sales User");
        token.Claims.Single(x => x.Type == "role").Value.Should().Be("Sales");
        token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(4));
        token.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(6));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(TestAuthSeedData.TestSalesEmail, "wrong"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        using var client = _fixture.Factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(TestAuthSeedData.TestSalesEmail, TestAuthSeedData.TestSalesPassword));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var response = await client.GetAsync("/api/auth/me");
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        user.Should().NotBeNull();
        user!.Id.Should().Be(TestAuthSeedData.TestSalesUserId);
        user.Email.Should().Be(TestAuthSeedData.TestSalesEmail);
        user.Role.ToString().Should().Be("Sales");
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SwaggerDocument_IncludesBearerSecurityScheme()
    {
        using var client = _fixture.Factory.CreateClient();

        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");

        swagger.Should().Contain("\"Bearer\"");
        swagger.Should().Contain("\"scheme\": \"bearer\"");
    }

}
