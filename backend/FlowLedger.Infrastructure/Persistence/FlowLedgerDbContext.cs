using FlowLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Persistence;

public class FlowLedgerDbContext(DbContextOptions<FlowLedgerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EnrollmentRequest> EnrollmentRequests => Set<EnrollmentRequest>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BillingRequest> BillingRequests => Set<BillingRequest>();
    public DbSet<BillingRequestLineItem> BillingRequestLineItems => Set<BillingRequestLineItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowLedgerDbContext).Assembly);
    }
}
