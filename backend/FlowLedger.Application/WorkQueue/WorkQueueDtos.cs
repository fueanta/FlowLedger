using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.WorkQueue;

public sealed record WorkQueueQuery(
    WorkflowQueue? Queue,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 20);

public interface IWorkQueueService
{
    Task<PagedResult<BillingRequestListItemDto>> GetAsync(WorkQueueQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
}
