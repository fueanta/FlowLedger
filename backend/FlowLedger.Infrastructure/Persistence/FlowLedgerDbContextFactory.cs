using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowLedger.Infrastructure.Persistence;

public class FlowLedgerDbContextFactory : IDesignTimeDbContextFactory<FlowLedgerDbContext>
{
    public FlowLedgerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FLOWLEDGER_MIGRATION_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=FlowLedgerDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FlowLedgerDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FlowLedgerDbContext(options);
    }
}
