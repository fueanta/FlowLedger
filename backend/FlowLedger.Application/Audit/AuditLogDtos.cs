using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;

namespace FlowLedger.Application.Audit;

public sealed record AuditLogListItemDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string? EntityNumber,
    string ActorDisplayName,
    AuditActionType ActionType,
    string Message,
    string? BeforeStatus,
    string? AfterStatus,
    DateTime CreatedAtUtc);

public sealed record AuditLogQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? EntityType = null,
    AuditActionType? ActionType = null,
    string? Actor = null,
    DateTime? FromDate = null,
    DateTime? UntilDate = null,
    string? SortBy = "createdAtUtc",
    string? SortDirection = "desc");

public interface IAuditLogService
{
    Task<PagedResult<AuditLogListItemDto>> GetAsync(AuditLogQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
}
