using FlowLedger.Application.Auth;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FlowLedger.Tests;

public sealed class AuthEndpointFixture : IAsyncLifetime
{
    private const string JwtKey = "phase-3-test-jwt-key-32-characters-minimum";

    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
                builder.UseSetting("Jwt:Issuer", "FlowLedger.Tests");
                builder.UseSetting("Jwt:Audience", "FlowLedger.Tests");
                builder.UseSetting("Jwt:Key", JwtKey);
            });

        Factory.CreateClient().Dispose();
        await SeedTestUserAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    private async Task SeedTestUserAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var appSetting = await dbContext.AppSettings.SingleAsync(x => x.Key == FlowLedgerSeedData.JwtAccessTokenMinutesKey);
        appSetting.Value = "5";

        var passwordHash = passwordHasher.Hash(TestAuthSeedData.TestSalesPassword);
        dbContext.Users.Add(TestAuthSeedData.TestSalesUser(passwordHash.Hash, passwordHash.Salt));

        await dbContext.SaveChangesAsync();
    }
}
