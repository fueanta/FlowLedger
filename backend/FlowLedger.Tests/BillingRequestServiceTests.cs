using FluentAssertions;
using FlowLedger.Application.Audit;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.Configuration;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.BillingRequests;
using FlowLedger.Infrastructure.Audit;
using FlowLedger.Infrastructure.Configuration;
using FlowLedger.Infrastructure.Persistence;
using FlowLedger.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.Tests;

public class BillingRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_WithLineItems_CalculatesVatAndWritesAuditLog()
    {
        using var fixture = new BillingRequestServiceFixture();

        var id = await fixture.Service.CreateAsync(
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Unit service request",
                "Created from service unit test.",
                [
                    new CreateBillingRequestLineItemDto("Service A", 2, 100m),
                    new CreateBillingRequestLineItemDto("Service B", 1, 50m)
                ]),
            fixture.SalesUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.AuditLogs)
            .SingleAsync(x => x.Id == id);

        request.Status.Should().Be(BillingRequestStatus.Draft);
        request.AssignedQueue.Should().Be(WorkflowQueue.Sales);
        request.AssignedAtUtc.Should().NotBeNull();
        request.LastWorkflowActionAtUtc.Should().NotBeNull();
        request.SubtotalAmount.Should().Be(250m);
        request.VatAmount.Should().Be(37.50m);
        request.TotalAmount.Should().Be(287.50m);
        request.AuditLogs.Should().ContainSingle(x => x.ActionType == AuditActionType.Created);
    }

    [Fact]
    public async Task CreateAsync_WithConfiguredVat_CalculatesTotals()
    {
        using var fixture = new BillingRequestServiceFixture();
        await fixture.SetSettingsAsync(vatPercentage: 20m, managerApprovalThreshold: 100000m, invoiceDueDays: 30);

        var id = await fixture.Service.CreateAsync(
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Configured VAT request",
                "Created from service unit test.",
                [new CreateBillingRequestLineItemDto("Service A", 1, 100m)]),
            fixture.SalesUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests.SingleAsync(x => x.Id == id);
        request.SubtotalAmount.Should().Be(100m);
        request.VatAmount.Should().Be(20m);
        request.TotalAmount.Should().Be(120m);
    }

    [Fact]
    public async Task CreateAsync_WithArchivedClient_ThrowsInvalidOperationException()
    {
        using var fixture = new BillingRequestServiceFixture();
        var client = await fixture.DbContext.Customers.SingleAsync(x => x.Id == FlowLedgerSeedData.FiberRetailCustomerId);
        client.Status = ClientStatus.Archived;
        await fixture.DbContext.SaveChangesAsync();

        var act = () => fixture.Service.CreateAsync(
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Archived client request",
                "Created from service unit test.",
                [new CreateBillingRequestLineItemDto("Service A", 1, 100m)]),
            fixture.SalesUser,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Billing requests can only use active clients.");
    }

    [Fact]
    public async Task SubmitAsync_WithDraftRequest_MovesToAccountsReview()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateDraftRequestAsync(1000m);

        await fixture.Service.SubmitAsync(id, fixture.SalesUser, CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.AuditLogs)
            .SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.AccountsReview);
        request.AssignedToUserId.Should().Be(FlowLedgerSeedData.AccountsUserId);
        request.AssignedQueue.Should().Be(WorkflowQueue.Accounts);
        request.SubmittedByUserId.Should().Be(FlowLedgerSeedData.SalesUserId);
        request.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Submitted);
    }

    [Fact]
    public async Task ApproveAsync_UnderThreshold_AsAccounts_GeneratesInvoice()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateAccountsReviewRequestAsync(50000m);

        await fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Approved under threshold."),
            fixture.AccountsUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.Invoice)
            .Include(x => x.AuditLogs)
            .SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.InvoiceGenerated);
        request.AssignedQueue.Should().Be(WorkflowQueue.None);
        request.AssignedToUserId.Should().BeNull();
        request.Invoice.Should().NotBeNull();
        request.Invoice!.Status.Should().Be(InvoiceStatus.Issued);
        request.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.InvoiceGenerated);
    }

    [Fact]
    public async Task ApproveAsync_AboveThreshold_AsAccounts_MovesToManagerApproval()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateAccountsReviewRequestAsync(120000m);

        await fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Needs manager approval."),
            fixture.AccountsUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests.SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.ManagerApproval);
        request.AssignedToUserId.Should().Be(FlowLedgerSeedData.ManagerUserId);
        request.AssignedQueue.Should().Be(WorkflowQueue.Manager);
        request.AccountsReviewedByUserId.Should().Be(FlowLedgerSeedData.AccountsUserId);
        fixture.DbContext.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveAsync_WithConfiguredThreshold_GeneratesInvoiceUnderNewThreshold()
    {
        using var fixture = new BillingRequestServiceFixture();
        await fixture.SetSettingsAsync(vatPercentage: 15m, managerApprovalThreshold: 250000m, invoiceDueDays: 30);
        var id = await fixture.CreateAccountsReviewRequestAsync(120000m);

        await fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Configured threshold approves directly."),
            fixture.AccountsUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.Invoice)
            .SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.InvoiceGenerated);
        request.AssignedQueue.Should().Be(WorkflowQueue.None);
        request.AccountsReviewedByUserId.Should().Be(FlowLedgerSeedData.AccountsUserId);
        request.ManagerReviewedByUserId.Should().BeNull();
        request.Invoice.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveAsync_WithConfiguredInvoiceSettings_SnapshotsVatAndDueDays()
    {
        using var fixture = new BillingRequestServiceFixture();
        await fixture.SetSettingsAsync(vatPercentage: 20m, managerApprovalThreshold: 250000m, invoiceDueDays: 10);
        var id = await fixture.CreateAccountsReviewRequestAsync(1000m);

        await fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Configured invoice settings."),
            fixture.AccountsUser,
            CancellationToken.None);

        var invoice = await fixture.DbContext.Invoices.SingleAsync();
        invoice.VatPercentage.Should().Be(20m);
        invoice.VatAmount.Should().Be(200m);
        invoice.TotalAmount.Should().Be(1200m);
        invoice.DueDays.Should().Be(10);
        invoice.DueAtUtc.Should().Be(invoice.IssuedAtUtc.AddDays(10));
    }

    [Fact]
    public async Task ApproveAsync_ManagerApproval_AsManager_GeneratesInvoice()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateManagerApprovalRequestAsync();

        await fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Manager approved."),
            fixture.ManagerUser,
            CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.Invoice)
            .SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.InvoiceGenerated);
        request.AssignedQueue.Should().Be(WorkflowQueue.None);
        request.ManagerReviewedByUserId.Should().Be(FlowLedgerSeedData.ManagerUserId);
        request.Invoice.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitAsync_WithAlreadySubmittedRequest_ThrowsInvalidOperationException()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateAccountsReviewRequestAsync();

        var act = () => fixture.Service.SubmitAsync(id, fixture.SalesUser, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only draft or rejected requests can be submitted.");
    }

    [Fact]
    public async Task RejectAsync_WithAccountsReviewRequest_AllowsSalesToResubmit()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateAccountsReviewRequestAsync();

        await fixture.Service.RejectAsync(
            id,
            new RejectBillingRequestDto("Missing purchase order."),
            fixture.AccountsUser,
            CancellationToken.None);
        await fixture.Service.SubmitAsync(id, fixture.SalesUser, CancellationToken.None);

        var request = await fixture.DbContext.BillingRequests
            .Include(x => x.AuditLogs)
            .SingleAsync(x => x.Id == id);
        request.Status.Should().Be(BillingRequestStatus.AccountsReview);
        request.AssignedQueue.Should().Be(WorkflowQueue.Accounts);
        request.RejectedAtUtc.Should().BeNull();
        request.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Rejected);
        request.AuditLogs.Should().Contain(x => x.ActionType == AuditActionType.Submitted);
    }

    [Fact]
    public async Task ApproveAsync_WithSalesUser_ThrowsUnauthorizedAccessException()
    {
        using var fixture = new BillingRequestServiceFixture();
        var id = await fixture.CreateAccountsReviewRequestAsync();

        var act = () => fixture.Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Sales cannot approve."),
            fixture.SalesUser,
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        fixture.DbContext.Invoices.Should().BeEmpty();
    }
}

public sealed class BillingRequestServiceFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public BillingRequestServiceFixture()
    {
        var services = new ServiceCollection();
        services.AddDbContext<FlowLedgerDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IBillingRequestService, BillingRequestService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IWorkflowAuditWriter, WorkflowAuditWriter>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        DbContext.Users.AddRange(FlowLedgerSeedData.Users);
        DbContext.Customers.Add(CloneCustomer(FlowLedgerSeedData.Customers.Single(x => x.Id == FlowLedgerSeedData.FiberRetailCustomerId)));
        DbContext.SaveChanges();
        Service = _scope.ServiceProvider.GetRequiredService<IBillingRequestService>();
    }

    public FlowLedgerDbContext DbContext { get; }
    public IBillingRequestService Service { get; }
    public CurrentUser SalesUser => new(
        FlowLedgerSeedData.SalesUserId,
        "sales@flowledger.local",
        "Sarah Sales",
        RoleName.Sales);

    public CurrentUser AccountsUser => new(
        FlowLedgerSeedData.AccountsUserId,
        "accounts@flowledger.local",
        "Adam Accounts",
        RoleName.Accounts);

    public CurrentUser ManagerUser => new(
        FlowLedgerSeedData.ManagerUserId,
        "manager@flowledger.local",
        "Mina Manager",
        RoleName.Manager);

    public async Task<Guid> CreateDraftRequestAsync(decimal subtotal = 1000m)
    {
        return await Service.CreateAsync(
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Approval unit request",
                "Created from service unit test.",
                [new CreateBillingRequestLineItemDto("Service", 1, subtotal)]),
            SalesUser,
            CancellationToken.None);
    }

    public async Task<Guid> CreateAccountsReviewRequestAsync(decimal subtotal = 1000m)
    {
        var id = await CreateDraftRequestAsync(subtotal);

        await Service.SubmitAsync(id, SalesUser, CancellationToken.None);
        return id;
    }

    public async Task<Guid> CreateManagerApprovalRequestAsync()
    {
        var id = await CreateAccountsReviewRequestAsync(120000m);
        await Service.ApproveAsync(
            id,
            new ApproveBillingRequestDto("Move to manager."),
            AccountsUser,
            CancellationToken.None);
        return id;
    }

    public async Task SetSettingsAsync(decimal vatPercentage, decimal managerApprovalThreshold, int invoiceDueDays)
    {
        await UpsertSettingAsync(FlowLedgerSeedData.VatPercentageKey, vatPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await UpsertSettingAsync(FlowLedgerSeedData.ManagerApprovalThresholdKey, managerApprovalThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await UpsertSettingAsync(FlowLedgerSeedData.InvoiceDueDaysKey, invoiceDueDays.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await DbContext.SaveChangesAsync();
    }

    private async Task UpsertSettingAsync(string key, string value)
    {
        var setting = await DbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key);
        if (setting is null)
        {
            DbContext.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Description = key
            });
            return;
        }

        setting.Value = value;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private static Customer CloneCustomer(Customer customer)
    {
        return new Customer
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            ContactEmail = customer.ContactEmail,
            Phone = customer.Phone,
            BillingAddress = customer.BillingAddress,
            TaxIdentifier = customer.TaxIdentifier,
            Status = customer.Status,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc,
            ArchivedAtUtc = customer.ArchivedAtUtc,
            ArchivedByUserId = customer.ArchivedByUserId
        };
    }
}
