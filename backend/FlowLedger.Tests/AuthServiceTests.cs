using FluentAssertions;
using FlowLedger.Application.Auth;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Tests;

public class AuthServiceTests : IClassFixture<AuthServiceFixture>
{
    private readonly AuthServiceFixture _fixture;

    public AuthServiceTests(AuthServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoginAsync_WithStoredPasswordHash_ReturnsUserAndToken()
    {
        var response = await _fixture.AuthService.LoginAsync(
            new LoginRequestDto(TestAuthSeedData.TestSalesEmail, TestAuthSeedData.TestSalesPassword),
            CancellationToken.None);

        response.AccessToken.Should().Be("test-token");
        response.User.Email.Should().Be(TestAuthSeedData.TestSalesEmail);
        response.User.FullName.Should().Be("Test Sales User");
        response.User.Role.ToString().Should().Be("Sales");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var act = () => _fixture.AuthService.LoginAsync(
            new LoginRequestDto(TestAuthSeedData.TestSalesEmail, "wrong"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithSeededUser_ReturnsUser()
    {
        var user = await _fixture.AuthService.GetCurrentUserAsync(TestAuthSeedData.TestSalesUserId, CancellationToken.None);

        user.Email.Should().Be(TestAuthSeedData.TestSalesEmail);
        user.Role.ToString().Should().Be("Sales");
    }

    internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public Task<string> GenerateTokenAsync(FlowLedger.Domain.Entities.User user, CancellationToken cancellationToken)
        {
            return Task.FromResult("test-token");
        }
    }
}

public sealed class AuthServiceFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public AuthServiceFixture()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddDbContext<FlowLedgerDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IAuthService, FlowLedger.Infrastructure.Auth.AuthService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, AuthServiceTests.FakeJwtTokenGenerator>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var dbContext = _scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        var passwordHash = passwordHasher.Hash(TestAuthSeedData.TestSalesPassword);

        dbContext.Users.Add(TestAuthSeedData.TestSalesUser(passwordHash.Hash, passwordHash.Salt));
        dbContext.SaveChanges();
    }

    public IAuthService AuthService => _scope.ServiceProvider.GetRequiredService<IAuthService>();

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }
}
