using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.BillingRequests;

public sealed record BillingRequestQuery(
    BillingRequestStatus? Status,
    Guid? CustomerId,
    WorkflowQueue? Queue,
    bool AssignedToMe = false,
    bool CreatedByMe = false,
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? UntilDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? SortBy = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 25);
