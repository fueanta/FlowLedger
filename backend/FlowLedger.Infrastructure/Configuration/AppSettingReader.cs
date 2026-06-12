using FlowLedger.Application.Configuration;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Configuration;

public sealed class AppSettingReader : IAppSettingReader
{
    private readonly FlowLedgerDbContext _dbContext;

    public AppSettingReader(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<string?> ReadValueAsync(string key, CancellationToken cancellationToken)
    {
        return _dbContext.AppSettings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
