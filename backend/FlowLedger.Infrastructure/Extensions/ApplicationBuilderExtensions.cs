using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using FlowLedger.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static async Task BootstrapSeedUserPasswordsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var bootstrapper = scope.ServiceProvider.GetRequiredService<SeedUserPasswordBootstrapper>();
        await bootstrapper.BootstrapAsync();
    }

    public static async Task RefreshDemoSeedDatesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var refresher = scope.ServiceProvider.GetRequiredService<DemoSeedDataRefresher>();
        await refresher.RefreshAsync();
    }
}
