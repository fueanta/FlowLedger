using FlowLedger.Application.Auth;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Configuration;
using FlowLedger.Application.Customers;
using FlowLedger.Application.Dashboard;
using FlowLedger.Application.Invoices;
using FlowLedger.Infrastructure.Auth;
using FlowLedger.Infrastructure.BillingRequests;
using FlowLedger.Infrastructure.Configuration;
using FlowLedger.Infrastructure.Customers;
using FlowLedger.Infrastructure.Dashboard;
using FlowLedger.Infrastructure.Invoices;
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
        services.AddScoped<IBillingRequestService, BillingRequestService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAppSettingReader, AppSettingReader>();
        services.AddScoped<SeedUserPasswordBootstrapper>();

        return services;
    }
}
