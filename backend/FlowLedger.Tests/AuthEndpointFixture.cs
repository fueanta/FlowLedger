using FlowLedger.Application.Auth;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.MsSql;

namespace FlowLedger.Tests;

public sealed class AuthEndpointFixture : IAsyncLifetime
{
    private const string JwtKey = "phase-3-test-jwt-key-32-characters-minimum";
    public const string SalesPassword = "Sales-test-password-1!";
    public const string AccountsPassword = "Accounts-test-password-1!";
    public const string ManagerPassword = "Manager-test-password-1!";
    public const string AdminPassword = "Admin-test-password-1!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
                builder.UseSetting("SeedUsers:SalesPassword", SalesPassword);
                builder.UseSetting("SeedUsers:AccountsPassword", AccountsPassword);
                builder.UseSetting("SeedUsers:ManagerPassword", ManagerPassword);
                builder.UseSetting("SeedUsers:AdminPassword", AdminPassword);
            });

        Factory.CreateClient().Dispose();
        await SeedTestUserAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _database.DisposeAsync();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(RoleName role)
    {
        var (email, password) = role switch
        {
            RoleName.Sales => ("sales@flowledger.local", SalesPassword),
            RoleName.Accounts => ("accounts@flowledger.local", AccountsPassword),
            RoleName.Manager => ("manager@flowledger.local", ManagerPassword),
            RoleName.Admin => ("admin@flowledger.local", AdminPassword),
            _ => throw new InvalidOperationException("Unsupported role.")
        };

        return await CreateAuthenticatedClientAsync(email, password);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        return client;
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

[CollectionDefinition("Api endpoints")]
public sealed class ApiEndpointCollection : ICollectionFixture<AuthEndpointFixture>;
