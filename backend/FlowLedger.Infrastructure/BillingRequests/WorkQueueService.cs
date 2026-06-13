using FlowLedger.Application.BillingRequests;
using FlowLedger.Application.Common;
using FlowLedger.Application.WorkQueue;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.BillingRequests;

public sealed class WorkQueueService : IWorkQueueService
{
    private readonly FlowLedgerDbContext _dbContext;

    public WorkQueueService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<BillingRequestListItemDto>> GetAsync(WorkQueueQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var page = PagingQueryGuard.Page(query.Page);
        var pageSize = PagingQueryGuard.PageSize(query.PageSize);
        var requests = _dbContext.BillingRequests.AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.AssignedQueue != WorkflowQueue.None)
            .AsQueryable();

        requests = ApplyRoleQueue(requests, query.Queue, currentUser);

        var search = PagingQueryGuard.Search(query.Search);
        if (search is not null)
        {
            requests = requests.Where(x =>
                x.RequestNumber.Contains(search) ||
                x.Title.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await ApplySort(requests, query.SortBy, query.SortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BillingRequestListItemDto(
                x.Id,
                x.RequestNumber,
                x.Title,
                x.Customer.Name,
                x.Status,
                x.AssignedQueue,
                x.AssignedAtUtc,
                x.LastWorkflowActionAtUtc,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<BillingRequestListItemDto>(items, page, pageSize, totalCount);
    }

    private static IQueryable<BillingRequest> ApplyRoleQueue(IQueryable<BillingRequest> query, WorkflowQueue? requestedQueue, CurrentUser currentUser)
    {
        return currentUser.Role switch
        {
            RoleName.Sales => query.Where(x => x.AssignedQueue == WorkflowQueue.Sales && x.CreatedByUserId == currentUser.Id),
            RoleName.Accounts => query.Where(x => x.AssignedQueue == WorkflowQueue.Accounts),
            RoleName.Manager => query.Where(x => x.AssignedQueue == WorkflowQueue.Manager),
            RoleName.Admin when requestedQueue is not null => query.Where(x => x.AssignedQueue == requestedQueue),
            RoleName.Admin => query,
            _ => query.Where(x => x.Id == Guid.Empty)
        };
    }

    private static IQueryable<BillingRequest> ApplySort(IQueryable<BillingRequest> query, string? sortBy, string? sortDirection)
    {
        var descending = PagingQueryGuard.Descending(sortDirection);
        var sort = PagingQueryGuard.SortBy(sortBy, "createdAtUtc", "createdAtUtc", "updatedAtUtc", "amount", "status", "clientName", "requestNumber");

        return sort.ToLowerInvariant() switch
        {
            "requestnumber" => descending ? query.OrderByDescending(x => x.RequestNumber) : query.OrderBy(x => x.RequestNumber),
            "updatedatutc" => descending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
            "amount" => descending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "clientname" => descending ? query.OrderByDescending(x => x.Customer.Name) : query.OrderBy(x => x.Customer.Name),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
        };
    }
}
