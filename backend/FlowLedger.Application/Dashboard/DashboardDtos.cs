namespace FlowLedger.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int TotalRequests,
    int PendingAccountsReview,
    int PendingManagerApproval,
    int ApprovedThisMonth,
    decimal TotalInvoiceAmount,
    decimal PaidInvoiceAmount,
    int RejectedCount,
    decimal AverageApprovalHours,
    IReadOnlyList<StatusBreakdownDto> StatusBreakdown,
    IReadOnlyList<MonthlyInvoiceTrendDto> MonthlyInvoiceTrend,
    IReadOnlyList<AgingBucketDto> AgingBuckets,
    IReadOnlyList<RecentActivityDto> RecentActivity);

public sealed record StatusBreakdownDto(string Status, int Count);

public sealed record MonthlyInvoiceTrendDto(string Month, decimal Amount);

public sealed record AgingBucketDto(string Label, int Count);

public sealed record RecentActivityDto(
    Guid Id,
    Guid BillingRequestId,
    string RequestNumber,
    string ActorName,
    string ActionType,
    string Message,
    DateTime CreatedAtUtc);
