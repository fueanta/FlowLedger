using FlowLedger.Application.Audit;
using FlowLedger.Application.Common;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Common;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Audit;

public sealed class AuditLogService : IAuditLogService
{
    private readonly FlowLedgerDbContext _dbContext;

    public AuditLogService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AuditLogListItemDto>> GetAsync(AuditLogQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureCanView(currentUser);

        var page = PagingQueryGuard.Page(query.Page);
        var pageSize = PagingQueryGuard.PageSize(query.PageSize);
        var logs = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!currentUser.IsAdmin)
        {
            logs = logs.Where(x => x.BillingRequest != null && x.BillingRequest.CreatedByUserId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            logs = logs.Where(x => x.EntityType == entityType);
        }

        if (query.ActionType is not null)
        {
            logs = logs.Where(x => x.ActionType == query.ActionType);
        }

        var actor = PagingQueryGuard.Search(query.Actor);
        if (actor is not null)
        {
            logs = logs.Where(x => x.ActorDisplayName.Contains(actor));
        }

        if (query.FromDate is not null)
        {
            logs = logs.Where(x => x.CreatedAtUtc >= query.FromDate);
        }

        if (query.UntilDate is not null)
        {
            logs = logs.Where(x => x.CreatedAtUtc <= query.UntilDate);
        }

        var search = PagingQueryGuard.Search(query.Search);
        if (search is not null)
        {
            logs = logs.Where(x =>
                x.EntityType.Contains(search) ||
                (x.EntityNumber != null && x.EntityNumber.Contains(search)) ||
                x.ActorDisplayName.Contains(search) ||
                x.Message.Contains(search));
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await ApplySort(logs, query.SortBy, query.SortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogListItemDto(
                x.Id,
                x.EntityType,
                x.EntityId,
                x.EntityNumber,
                x.ActorDisplayName,
                x.ActionType,
                x.Message,
                x.BeforeStatus,
                x.AfterStatus,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogListItemDto>(items, page, pageSize, totalCount);
    }

    private static IQueryable<AuditLog> ApplySort(IQueryable<AuditLog> query, string? sortBy, string? sortDirection)
    {
        var descending = PagingQueryGuard.Descending(sortDirection);
        var sort = PagingQueryGuard.SortBy(sortBy, "createdAtUtc", "createdAtUtc", "entityType", "actionType", "actorDisplayName");

        return sort.ToLowerInvariant() switch
        {
            "entitytype" => descending ? query.OrderByDescending(x => x.EntityType) : query.OrderBy(x => x.EntityType),
            "actiontype" => descending ? query.OrderByDescending(x => x.ActionType) : query.OrderBy(x => x.ActionType),
            "actordisplayname" => descending ? query.OrderByDescending(x => x.ActorDisplayName) : query.OrderBy(x => x.ActorDisplayName),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
        };
    }

    private static void EnsureCanView(CurrentUser currentUser)
    {
        if (currentUser.Role is not (RoleName.Sales or RoleName.Accounts or RoleName.Manager or RoleName.Admin))
        {
            throw new UnauthorizedAccessException("Only internal users can view audit logs.");
        }
    }
}
