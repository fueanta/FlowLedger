using FlowLedger.Application.Common;
using FlowLedger.Application.Enrollment;
using FlowLedger.Application.Auth;
using FlowLedger.Domain.Entities;
using FlowLedger.Domain.Enums;
using FlowLedger.Infrastructure.Auth;
using FlowLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Infrastructure.Enrollment;

public sealed class EnrollmentService : IEnrollmentService
{
    private const int MaxPageSize = 100;

    private readonly FlowLedgerDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public EnrollmentService(FlowLedgerDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> RegisterAsync(RegisterEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailUnavailable = await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken) ||
            await _dbContext.EnrollmentRequests.AnyAsync(x => x.Email == email && x.Status == EnrollmentRequestStatus.Pending, cancellationToken);

        if (emailUnavailable)
        {
            throw new InvalidOperationException("Email is unavailable.");
        }

        var password = _passwordHasher.Hash(request.Password);
        var now = DateTime.UtcNow;
        var enrollment = new EnrollmentRequest
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            RequestedRole = request.RequestedRole,
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            Status = EnrollmentRequestStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.EnrollmentRequests.Add(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return enrollment.Id;
    }

    public async Task<PagedResult<EnrollmentRequestDto>> GetAsync(EnrollmentRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var requests = _dbContext.EnrollmentRequests.AsNoTracking().Include(x => x.ReviewedByUser).AsQueryable();

        if (query.Status is not null)
        {
            requests = requests.Where(x => x.Status == query.Status);
        }

        if (query.RequestedRole is not null)
        {
            requests = requests.Where(x => x.RequestedRole == query.RequestedRole);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            requests = requests.Where(x => x.FullName.Contains(search) || x.Email.Contains(search));
        }

        requests = ApplySort(requests, query.SortBy, query.SortDirection);

        var totalCount = await requests.CountAsync(cancellationToken);
        var items = await requests.Skip((page - 1) * pageSize).Take(pageSize).Select(x => ToDto(x)).ToListAsync(cancellationToken);

        return new PagedResult<EnrollmentRequestDto>(items, page, pageSize, totalCount);
    }

    public async Task<EnrollmentRequestDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var request = await _dbContext.EnrollmentRequests.AsNoTracking()
            .Include(x => x.ReviewedByUser)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return request is null ? throw new KeyNotFoundException("Enrollment request was not found.") : ToDto(request);
    }

    public async Task ApproveAsync(Guid id, ApproveEnrollmentRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var enrollment = await _dbContext.EnrollmentRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (enrollment is null)
        {
            throw new KeyNotFoundException("Enrollment request was not found.");
        }

        if (enrollment.Status != EnrollmentRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending enrollment requests can be approved.");
        }

        var existingUser = await _dbContext.Users.SingleOrDefaultAsync(x => x.Email == enrollment.Email, cancellationToken);
        var now = DateTime.UtcNow;
        if (existingUser is null)
        {
            _dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FullName = enrollment.FullName,
                Email = enrollment.Email,
                PasswordHash = enrollment.PasswordHash,
                PasswordSalt = enrollment.PasswordSalt,
                Role = request.AssignedRole,
                Status = UserStatus.Active,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                EnrollmentRequestId = enrollment.Id
            });
        }
        else
        {
            existingUser.FullName = enrollment.FullName;
            existingUser.PasswordHash = enrollment.PasswordHash;
            existingUser.PasswordSalt = enrollment.PasswordSalt;
            existingUser.Role = request.AssignedRole;
            existingUser.Status = UserStatus.Active;
            existingUser.IsActive = true;
            existingUser.DeactivatedAtUtc = null;
            existingUser.DeactivatedByUserId = null;
            existingUser.UpdatedAtUtc = now;
            existingUser.EnrollmentRequestId = enrollment.Id;
        }

        enrollment.Status = EnrollmentRequestStatus.Approved;
        enrollment.ReviewedByUserId = currentUser.Id;
        enrollment.ReviewedAtUtc = now;
        enrollment.DecisionReason = null;
        enrollment.UpdatedAtUtc = now;
        AddAudit(enrollment, currentUser, AuditActionType.EnrollmentApproved, "Enrollment request approved.", "Pending", "Approved", now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, RejectEnrollmentRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        EnsureAdmin(currentUser);

        var enrollment = await _dbContext.EnrollmentRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (enrollment is null)
        {
            throw new KeyNotFoundException("Enrollment request was not found.");
        }

        if (enrollment.Status != EnrollmentRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending enrollment requests can be rejected.");
        }

        var now = DateTime.UtcNow;
        enrollment.Status = EnrollmentRequestStatus.Rejected;
        enrollment.ReviewedByUserId = currentUser.Id;
        enrollment.ReviewedAtUtc = now;
        enrollment.DecisionReason = request.Reason.Trim();
        enrollment.UpdatedAtUtc = now;
        AddAudit(enrollment, currentUser, AuditActionType.EnrollmentRejected, "Enrollment request rejected.", "Pending", "Rejected", now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<EnrollmentRequest> ApplySort(IQueryable<EnrollmentRequest> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "requestedrole" => descending ? query.OrderByDescending(x => x.RequestedRole) : query.OrderBy(x => x.RequestedRole),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
        };
    }

    private static EnrollmentRequestDto ToDto(EnrollmentRequest request)
    {
        return new EnrollmentRequestDto(
            request.Id,
            request.FullName,
            request.Email,
            request.RequestedRole,
            request.Status,
            request.ReviewedByUser?.FullName,
            request.ReviewedAtUtc,
            request.DecisionReason,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);
    }

    private void AddAudit(
        EnrollmentRequest enrollment,
        CurrentUser currentUser,
        AuditActionType actionType,
        string message,
        string beforeStatus,
        string afterStatus,
        DateTime now)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "EnrollmentRequest",
            EntityId = enrollment.Id,
            EntityNumber = enrollment.Email,
            ActorUserId = currentUser.Id,
            ActorDisplayName = currentUser.FullName,
            ActionType = actionType,
            Message = message,
            BeforeStatus = beforeStatus,
            AfterStatus = afterStatus,
            CreatedAtUtc = now
        });
    }

    private static void EnsureAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only Admin users can manage enrollment requests.");
        }
    }
}
