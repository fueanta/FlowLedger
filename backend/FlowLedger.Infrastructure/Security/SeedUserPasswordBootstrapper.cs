using FlowLedger.Application.Auth;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FlowLedger.Infrastructure.Security;

public sealed class SeedUserPasswordBootstrapper
{
    private readonly FlowLedgerDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public SeedUserPasswordBootstrapper(
        FlowLedgerDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        var credentials = new Dictionary<Guid, string?>
        {
            [FlowLedgerSeedData.SalesUserId] = _configuration["SeedUsers:SalesPassword"],
            [FlowLedgerSeedData.AccountsUserId] = _configuration["SeedUsers:AccountsPassword"],
            [FlowLedgerSeedData.ManagerUserId] = _configuration["SeedUsers:ManagerPassword"],
            [FlowLedgerSeedData.AdminUserId] = _configuration["SeedUsers:AdminPassword"]
        };

        foreach (var credential in credentials)
        {
            if (string.IsNullOrWhiteSpace(credential.Value))
            {
                continue;
            }

            var user = await _dbContext.Users.SingleAsync(x => x.Id == credential.Key, cancellationToken);
            var passwordHash = _passwordHasher.Hash(credential.Value);

            user.PasswordHash = passwordHash.Hash;
            user.PasswordSalt = passwordHash.Salt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
