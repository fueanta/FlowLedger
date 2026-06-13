using FlowLedger.Application.Common;
using FlowLedger.Application.Dashboard;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly FlowLedgerDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardService(FlowLedgerDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DashboardQuery query,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var periodStart = now.AddMonths(-query.PeriodMonths);
        var requests = ApplyRequestVisibility(_dbContext.BillingRequests.AsNoTracking(), currentUser);
        var invoices = ApplyInvoiceVisibility(_dbContext.Invoices.AsNoTracking(), currentUser);

        var requestRows = await requests
            .Where(x =>
                x.CreatedAtUtc >= periodStart ||
                x.ApprovedAtUtc >= periodStart ||
                x.RejectedAtUtc >= periodStart)
            .Select(x => new
            {
                x.Status,
                x.CreatedAtUtc,
                x.SubmittedAtUtc,
                x.ApprovedAtUtc,
                x.RejectedAtUtc
            })
            .ToListAsync(cancellationToken);

        var invoiceRows = await invoices
            .Where(x => x.IssuedAtUtc >= periodStart || x.PaidAtUtc >= periodStart)
            .Select(x => new
            {
                x.Status,
                x.IssuedAtUtc,
                x.PaidAtUtc,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var pendingRows = await requests
            .Where(x => x.Status == BillingRequestStatus.AccountsReview || x.Status == BillingRequestStatus.ManagerApproval)
            .Select(x => new
            {
                x.Status,
                StartedAt = x.SubmittedAtUtc ?? x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var totalRequests = requestRows.Count(x => x.CreatedAtUtc >= periodStart);
        var pendingAccountsReview = pendingRows.Count(x => x.Status == BillingRequestStatus.AccountsReview);
        var pendingManagerApproval = pendingRows.Count(x => x.Status == BillingRequestStatus.ManagerApproval);
        var approvedThisMonth = requestRows.Count(x => x.ApprovedAtUtc >= periodStart);
        var totalInvoiceAmount = invoiceRows
            .Where(x => x.IssuedAtUtc >= periodStart)
            .Sum(x => x.TotalAmount);
        var paidInvoiceAmount = invoiceRows
            .Where(x => x.Status == InvoiceStatus.Paid && x.PaidAtUtc >= periodStart)
            .Sum(x => x.TotalAmount);
        var rejectedCount = requestRows.Count(x => x.RejectedAtUtc >= periodStart);
        var approvedDurations = requestRows
            .Where(x => x.SubmittedAtUtc != null && x.ApprovedAtUtc >= periodStart)
            .ToList();
        var averageApprovalHours = approvedDurations.Count == 0
            ? 0m
            : Math.Round((decimal)approvedDurations.Average(x => (x.ApprovedAtUtc!.Value - x.SubmittedAtUtc!.Value).TotalHours), 1);

        var statusBreakdown = requestRows
            .Where(x => x.CreatedAtUtc >= periodStart)
            .GroupBy(x => x.Status)
            .OrderBy(x => x.Key)
            .Select(x => new StatusBreakdownDto(x.Key.ToString(), x.Count()))
            .ToList();

        var monthlyInvoiceTrend = invoiceRows
            .Where(x => x.IssuedAtUtc >= periodStart)
            .GroupBy(x => new DateTime(x.IssuedAtUtc.Year, x.IssuedAtUtc.Month, 1))
            .OrderBy(x => x.Key)
            .Select(x => new MonthlyInvoiceTrendDto(x.Key.ToString("MMM"), x.Sum(row => row.TotalAmount)))
            .ToList();

        var agingBuckets = new List<AgingBucketDto>
        {
            new("0-1 days", pendingRows.Count(x => (now - x.StartedAt).TotalDays <= 1)),
            new("2-3 days", pendingRows.Count(x => (now - x.StartedAt).TotalDays > 1 && (now - x.StartedAt).TotalDays <= 3)),
            new("4+ days", pendingRows.Count(x => (now - x.StartedAt).TotalDays > 3))
        };

        var recentActivity = await ApplyAuditVisibility(_dbContext.AuditLogs.AsNoTracking(), currentUser)
            .Include(x => x.BillingRequest)
            .Include(x => x.ActorUser)
            .Where(x => x.BillingRequestId != null && x.BillingRequest != null && x.CreatedAtUtc >= periodStart)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .Select(x => new RecentActivityDto(
                x.Id,
                x.BillingRequestId!.Value,
                x.BillingRequest!.RequestNumber,
                x.ActorUser.FullName,
                x.ActionType.ToString(),
                x.Message,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            new DashboardPeriodDto(query.PeriodMonths, periodStart, now),
            new Dictionary<string, string>
            {
                [nameof(DashboardSummaryDto.TotalRequests)] = "Period",
                [nameof(DashboardSummaryDto.ApprovedThisMonth)] = "Period",
                [nameof(DashboardSummaryDto.TotalInvoiceAmount)] = "Period",
                [nameof(DashboardSummaryDto.PaidInvoiceAmount)] = "Period",
                [nameof(DashboardSummaryDto.RejectedCount)] = "Period",
                [nameof(DashboardSummaryDto.AverageApprovalHours)] = "Period",
                [nameof(DashboardSummaryDto.StatusBreakdown)] = "Period",
                [nameof(DashboardSummaryDto.MonthlyInvoiceTrend)] = "Period",
                [nameof(DashboardSummaryDto.RecentActivity)] = "Period",
                [nameof(DashboardSummaryDto.PendingAccountsReview)] = "Current",
                [nameof(DashboardSummaryDto.PendingManagerApproval)] = "Current",
                [nameof(DashboardSummaryDto.AgingBuckets)] = "Current"
            },
            totalRequests,
            pendingAccountsReview,
            pendingManagerApproval,
            approvedThisMonth,
            totalInvoiceAmount,
            paidInvoiceAmount,
            rejectedCount,
            averageApprovalHours,
            statusBreakdown,
            monthlyInvoiceTrend,
            agingBuckets,
            recentActivity);
    }

    private static IQueryable<BillingRequest> ApplyRequestVisibility(IQueryable<BillingRequest> query, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => query,
            RoleName.Sales => query.Where(x => x.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query,
            RoleName.Manager => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }

    private static IQueryable<Invoice> ApplyInvoiceVisibility(IQueryable<Invoice> query, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => query,
            RoleName.Sales => query.Where(x => x.BillingRequest != null && x.BillingRequest.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query,
            RoleName.Manager => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }

    private static IQueryable<AuditLog> ApplyAuditVisibility(IQueryable<AuditLog> query, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Admin => query,
            RoleName.Sales => query.Where(x => x.BillingRequest != null && x.BillingRequest.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query,
            RoleName.Manager => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }
}
