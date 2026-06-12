using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace FlowLedger.Tests;

public sealed class DatabaseMigrationFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public DbContextOptions<FlowLedgerDbContext> DbContextOptions { get; private set; } = null!;
    public string ConnectionString => _database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        DbContextOptions = new DbContextOptionsBuilder<FlowLedgerDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var dbContext = new FlowLedgerDbContext(DbContextOptions);
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _database.DisposeAsync().AsTask();
    }
}
