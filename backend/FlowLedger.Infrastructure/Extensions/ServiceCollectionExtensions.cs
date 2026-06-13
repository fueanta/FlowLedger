using FlowLedger.Application.Auth;
using FlowLedger.Application.Audit;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Common.Csv;
using FlowLedger.Application.Configuration;
using FlowLedger.Application.Customers;
using FlowLedger.Application.Dashboard;
using FlowLedger.Application.Enrollment;
using FlowLedger.Application.Invoices;
using FlowLedger.Application.Preferences;
using FlowLedger.Application.Users;
using FlowLedger.Application.WorkQueue;
using FlowLedger.Infrastructure.Audit;
using FlowLedger.Infrastructure.Auth;
using FlowLedger.Infrastructure.BillingRequests;
using FlowLedger.Infrastructure.Configuration;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Customers;
using FlowLedger.Infrastructure.Dashboard;
using FlowLedger.Infrastructure.Enrollment;
using FlowLedger.Infrastructure.Invoices;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using FlowLedger.Infrastructure.Preferences;
using FlowLedger.Infrastructure.Security;
using FlowLedger.Infrastructure.Time;
using FlowLedger.Infrastructure.Users;
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
        services.AddScoped<IWorkQueueService, WorkQueueService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAppSettingReader, AppSettingReader>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IWorkflowAuditWriter, WorkflowAuditWriter>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<SeedUserPasswordBootstrapper>();
        services.AddScoped<DemoSeedDataRefresher>();

        return services;
    }
}
