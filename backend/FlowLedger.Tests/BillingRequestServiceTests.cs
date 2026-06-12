using FluentAssertions;
using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.BillingRequests;
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
        request.SubtotalAmount.Should().Be(250m);
        request.VatAmount.Should().Be(37.50m);
        request.TotalAmount.Should().Be(287.50m);
        request.AuditLogs.Should().ContainSingle(x => x.ActionType == AuditActionType.Created);
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

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<FlowLedgerDbContext>();
        DbContext.Users.AddRange(FlowLedgerSeedData.Users);
        DbContext.Customers.Add(FlowLedgerSeedData.Customers.Single(x => x.Id == FlowLedgerSeedData.FiberRetailCustomerId));
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

    public async Task<Guid> CreateAccountsReviewRequestAsync()
    {
        var id = await Service.CreateAsync(
            new CreateBillingRequestDto(
                FlowLedgerSeedData.FiberRetailCustomerId,
                "Approval unit request",
                "Created from service unit test.",
                [new CreateBillingRequestLineItemDto("Service", 1, 1000m)]),
            SalesUser,
            CancellationToken.None);

        await Service.SubmitAsync(id, SalesUser, CancellationToken.None);
        return id;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }
}
