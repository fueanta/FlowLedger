using FlowLedger.Application.Auth;
using FlowLedger.Application.Configuration;
using FlowLedger.Infrastructure.Auth;
using FlowLedger.Infrastructure.Configuration;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FlowLedgerDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAppSettingReader, AppSettingReader>();
        services.AddScoped<SeedUserPasswordBootstrapper>();

        return services;
    }
}
