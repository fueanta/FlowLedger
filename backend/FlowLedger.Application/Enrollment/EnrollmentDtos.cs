using FlowLedger.Application.Common;
using FlowLedger.Domain.Enums;
using FluentValidation;

namespace FlowLedger.Application.Enrollment;

public sealed record EnrollmentRequestDto(
    Guid Id,
    string FullName,
    string Email,
    RoleName RequestedRole,
    EnrollmentRequestStatus Status,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    string? DecisionReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RegisterEnrollmentRequestDto(string FullName, string Email, string Password, RoleName RequestedRole);

public sealed record ApproveEnrollmentRequestDto(RoleName AssignedRole);

public sealed record RejectEnrollmentRequestDto(string Reason);

public sealed record EnrollmentRequestQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    EnrollmentRequestStatus? Status = null,
    RoleName? RequestedRole = null,
    string? SortBy = "createdAtUtc",
    string? SortDirection = "desc");

public interface IEnrollmentService
{
    Task<Guid> RegisterAsync(RegisterEnrollmentRequestDto request, CancellationToken cancellationToken);
    Task<PagedResult<EnrollmentRequestDto>> GetAsync(EnrollmentRequestQuery query, CurrentUser currentUser, CancellationToken cancellationToken);
    Task<EnrollmentRequestDto> GetByIdAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken);
    Task ApproveAsync(Guid id, ApproveEnrollmentRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
    Task RejectAsync(Guid id, RejectEnrollmentRequestDto request, CurrentUser currentUser, CancellationToken cancellationToken);
}

public sealed class RegisterEnrollmentRequestDtoValidator : AbstractValidator<RegisterEnrollmentRequestDto>
{
    public RegisterEnrollmentRequestDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.RequestedRole).IsInEnum();
    }
}

public sealed class ApproveEnrollmentRequestDtoValidator : AbstractValidator<ApproveEnrollmentRequestDto>
{
    public ApproveEnrollmentRequestDtoValidator()
    {
        RuleFor(x => x.AssignedRole).IsInEnum();
    }
}

public sealed class RejectEnrollmentRequestDtoValidator : AbstractValidator<RejectEnrollmentRequestDto>
{
    public RejectEnrollmentRequestDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
