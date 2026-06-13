using FlowLedger.Application.Auth;
using FlowLedger.Application.Common;
using FlowLedger.Application.Users;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Auth;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Users;

public sealed class UserAdminService : IUserAdminService
{
    private const int MaxPageSize = 100;

    private readonly FlowLedgerDbContext _dbContext;

    public UserAdminService(FlowLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<UserDto>> GetAsync(UserQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var users = _dbContext.Users.AsNoTracking().AsQueryable();

        if (query.Role is not null)
        {
            users = users.Where(x => x.Role == query.Role);
        }

        if (query.Status is not null)
        {
            users = users.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(x => x.FullName.Contains(search) || x.Email.Contains(search));
        }

        users = ApplySort(users, query.SortBy, query.SortDirection);

        var totalCount = await users.CountAsync(cancellationToken);
        var items = await users.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.ToDto()).ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, page, pageSize, totalCount);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? throw new KeyNotFoundException("User was not found.") : user.ToDto();
    }

    public async Task UpdateRoleAsync(Guid id, UpdateUserRoleDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var user = await LoadUserAsync(id, cancellationToken);
        var beforeRole = user.Role.ToString();
        user.Role = request.Role;
        var now = DateTime.UtcNow;
        user.UpdatedAtUtc = now;
        AddAudit(user, currentUser, AuditActionType.UserRoleChanged, "User role changed.", beforeRole, request.Role.ToString(), now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var user = await LoadUserAsync(id, cancellationToken);
        var beforeStatus = user.Status.ToString();
        user.Status = UserStatus.Active;
        user.IsActive = true;
        user.DeactivatedAtUtc = null;
        user.DeactivatedByUserId = null;
        var now = DateTime.UtcNow;
        user.UpdatedAtUtc = now;
        AddAudit(user, currentUser, AuditActionType.UserActivated, "User activated.", beforeStatus, UserStatus.Active.ToString(), now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        if (id == currentUser.Id)
        {
            throw new InvalidOperationException("Admin users cannot deactivate their own account.");
        }

        var user = await LoadUserAsync(id, cancellationToken);
        if (user.Role == RoleName.Admin)
        {
            var activeAdminCount = await _dbContext.Users.CountAsync(x =>
                x.Id != id &&
                x.Role == RoleName.Admin &&
                x.IsActive &&
                x.Status == UserStatus.Active,
                cancellationToken);

            if (activeAdminCount == 0)
            {
                throw new InvalidOperationException("At least one active Admin user is required.");
            }
        }

        var now = DateTime.UtcNow;
        user.Status = UserStatus.Inactive;
        user.IsActive = false;
        user.DeactivatedAtUtc = now;
        user.DeactivatedByUserId = currentUser.Id;
        user.UpdatedAtUtc = now;
        AddAudit(user, currentUser, AuditActionType.UserDeactivated, "User deactivated.", UserStatus.Active.ToString(), UserStatus.Inactive.ToString(), now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> LoadUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
    }

    private void AddAudit(User user, CurrentUser currentUser, AuditActionType actionType, string message, string? beforeStatus, string? afterStatus, DateTime now)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "User",
            EntityId = user.Id,
            EntityNumber = user.Email,
            ActorUserId = currentUser.Id,
            ActorDisplayName = currentUser.FullName,
            ActionType = actionType,
            Message = message,
            BeforeStatus = beforeStatus,
            AfterStatus = afterStatus,
            CreatedAtUtc = now
        });
    }

    private static IQueryable<User> ApplySort(IQueryable<User> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "role" => descending ? query.OrderByDescending(x => x.Role) : query.OrderBy(x => x.Role),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "updatedatutc" => descending ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.UpdatedAtUtc),
            _ => descending ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName)
        };
    }

    private static void EnsureAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only Admin users can manage users.");
        }
    }
}
